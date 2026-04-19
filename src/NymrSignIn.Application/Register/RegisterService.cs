using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NymrSignIn.Application.Register.Dtos;
using NymrSignIn.Domain.Register;

namespace NymrSignIn.Application.Register;

public sealed class RegisterService
{
    private readonly IRegisterRepository _repository;
    private readonly ISignatureStorage _signatureStorage;
    private readonly IRegisterEmailService _emailService;
    private readonly ISiteCodeGenerator _siteCodeGenerator;
    private readonly TimeProvider _timeProvider;
    private readonly SiteOptions _siteOptions;
    private readonly ILogger<RegisterService> _logger;

    public RegisterService(
        IRegisterRepository repository,
        ISignatureStorage signatureStorage,
        IRegisterEmailService emailService,
        ISiteCodeGenerator siteCodeGenerator,
        TimeProvider timeProvider,
        IOptions<SiteOptions> siteOptions,
        ILogger<RegisterService> logger)
    {
        _repository = repository;
        _signatureStorage = signatureStorage;
        _emailService = emailService;
        _siteCodeGenerator = siteCodeGenerator;
        _timeProvider = timeProvider;
        _siteOptions = siteOptions.Value;
        _logger = logger;
    }

    public async Task<RegisterResponse> GetTodaysRegisterAsync(CancellationToken cancellationToken)
    {
        var today = GetUkDateNow();
        var entries = await _repository.GetEntriesByDateAsync(today, cancellationToken);

        var signedIn = entries
            .Where(e => !e.IsSignedOut
                && e.Status is SiteStatus.OnSite or SiteStatus.OnSiteConditional)
            .OrderBy(e => e.TimeIn)
            .Select(e => new SignedInEntryDto(
                e.Id,
                e.Name,
                e.Organisation,
                FormatDate(e.DateIn),
                FormatTime(e.TimeIn),
                DescribeMedical(e.MedicalStatus),
                DescribeStatus(e.Status)))
            .ToList();

        var signedOut = entries
            .Where(e => e.IsSignedOut)
            .OrderBy(e => e.TimeOut)
            .Select(e => new SignedOutEntryDto(
                e.Id,
                e.Name,
                e.Organisation,
                FormatDate(e.DateIn),
                FormatTime(e.TimeIn),
                FormatTime(e.TimeOut!.Value)))
            .ToList();

        var denied = entries
            .Where(e => e.Status == SiteStatus.Denied)
            .OrderBy(e => e.TimeIn)
            .Select(e => new DeniedEntryDto(
                e.Id,
                e.Name,
                e.Organisation,
                FormatDate(e.DateIn),
                FormatTime(e.TimeIn)))
            .ToList();

        return new RegisterResponse(signedIn, signedOut, denied);
    }

    public async Task<SignInResponse> SignInAsync(SignInRequest request, CancellationToken cancellationToken)
    {
        var ukNow = GetUkDateTimeNow();
        var dateIn = DateOnly.FromDateTime(ukNow);
        var timeIn = TimeOnly.FromDateTime(ukNow);
        var utcNow = _timeProvider.GetUtcNow();

        var entry = SiteRegisterEntry.Create(request.Name, request.Organisation, dateIn, timeIn, utcNow);

        if (!string.IsNullOrWhiteSpace(request.SignatureBase64))
        {
            try
            {
                var signatureUrl = await UploadSignatureAsync(entry.Id, dateIn, request.SignatureBase64, cancellationToken);
                entry.SetSignatureUrl(signatureUrl);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to upload signature for entry {EntryId}; continuing without signature", entry.Id);
            }
        }

        await _repository.AddAsync(entry, cancellationToken);

        _logger.LogInformation("Person {Name} signed in at {TimeIn} — status {Status}", entry.Name, timeIn, entry.Status);

        return new SignInResponse(
            entry.Id,
            entry.Name,
            entry.Organisation,
            FormatDate(dateIn),
            FormatTime(timeIn));
    }

    public async Task<ConfirmFitResponse> ConfirmFitAsync(
        Guid id,
        ConfirmFitRequest request,
        CancellationToken cancellationToken)
    {
        var entry = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Register entry {id} not found.");

        entry.DeclareFit(request.AdditionalInfo, request.SiteCode);
        await _repository.UpdateAsync(entry, cancellationToken);

        _logger.LogInformation("Person {Name} declared FIT", entry.Name);

        return new ConfirmFitResponse(
            entry.Id,
            entry.Name,
            entry.Organisation,
            FormatDate(entry.DateIn),
            FormatTime(entry.TimeIn),
            DescribeStatus(entry.Status));
    }

    public async Task<DeclareNotFitResponse> DeclareNotFitAsync(
        Guid id,
        DeclareNotFitRequest request,
        CancellationToken cancellationToken)
    {
        var entry = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Register entry {id} not found.");

        var generatedCode = _siteCodeGenerator.Generate();
        entry.DeclareNotFit(request.AdditionalInfo, generatedCode);
        await _repository.UpdateAsync(entry, cancellationToken);

        _logger.LogInformation("Person {Name} declared NOT FIT — site code issued", entry.Name);

        return new DeclareNotFitResponse(
            entry.Id,
            _siteOptions.SiteController.Name,
            _siteOptions.SiteController.Email,
            _siteOptions.SiteController.Phone);
    }

    public async Task SendNotFitAlertAsync(Guid id, CancellationToken cancellationToken)
    {
        var entry = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Register entry {id} not found.");

        if (entry.Status is not SiteStatus.Denied)
        {
            throw new InvalidOperationException(
                $"Entry {id} is in status {entry.Status}; not-fit alert can only be sent for denied entries.");
        }

        await _emailService.SendNotFitAlertAsync(entry, cancellationToken);

        _logger.LogInformation("Not-fit alert email sent on request for entry {EntryId}", entry.Id);
    }

    public async Task<ConfirmFitResponse> SubmitSiteCodeAsync(
        Guid id,
        SubmitSiteCodeRequest request,
        CancellationToken cancellationToken)
    {
        var entry = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Register entry {id} not found.");

        var accepted = entry.TrySubmitSiteCode(request.SiteCode, request.AdditionalInfo);
        await _repository.UpdateAsync(entry, cancellationToken);

        if (!accepted)
        {
            _logger.LogWarning("Invalid site code submitted for entry {EntryId}", entry.Id);
            throw new InvalidSiteCodeException("The site code you entered is not recognised. Check with the Site Controller and try again.");
        }

        _logger.LogInformation("Entry {EntryId} authorised on conditional entry", entry.Id);

        return new ConfirmFitResponse(
            entry.Id,
            entry.Name,
            entry.Organisation,
            FormatDate(entry.DateIn),
            FormatTime(entry.TimeIn),
            DescribeStatus(entry.Status));
    }

    public async Task<SignOutResponse> SignOutAsync(Guid id, CancellationToken cancellationToken)
    {
        var entry = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Register entry {id} not found.");

        var ukNow = GetUkDateTimeNow();
        var timeOut = TimeOnly.FromDateTime(ukNow);

        entry.SignOut(timeOut);
        await _repository.UpdateAsync(entry, cancellationToken);

        _logger.LogInformation("Person {Name} signed out at {TimeOut}", entry.Name, timeOut);

        return new SignOutResponse(FormatTime(timeOut));
    }

    public async Task<byte[]> ExportCsvAsync(CancellationToken cancellationToken)
    {
        var today = GetUkDateNow();
        var entries = await _repository.GetEntriesByDateAsync(today, cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine("Name,Organisation,Date,Time In,Time Out,Medical Status,Site Code,Additional Information,Status");

        foreach (var entry in entries.OrderBy(e => e.TimeIn))
        {
            var timeOut = entry.Status == SiteStatus.Denied
                ? "ENTRY DENIED"
                : entry.IsSignedOut
                    ? FormatTime(entry.TimeOut!.Value)
                    : "STILL ON SITE";

            sb.AppendLine(string.Join(",",
                CsvEscape(entry.Name),
                CsvEscape(entry.Organisation),
                FormatDate(entry.DateIn),
                FormatTime(entry.TimeIn),
                timeOut,
                CsvEscape(DescribeMedical(entry.MedicalStatus)),
                CsvEscape(entry.SiteCode ?? string.Empty),
                CsvEscape(entry.AdditionalInfo ?? string.Empty),
                CsvEscape(DescribeStatus(entry.Status))));
        }

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    public async Task SendDailyEmailAsync(CancellationToken cancellationToken)
    {
        var today = GetUkDateNow();
        var entries = await _repository.GetEntriesByDateAsync(today, cancellationToken);

        await _emailService.SendDailyRegisterEmailAsync(entries, today, cancellationToken);

        _logger.LogInformation("Daily register email sent for {Date} with {Count} entries", today, entries.Count);
    }

    private async Task<string> UploadSignatureAsync(
        Guid entryId,
        DateOnly date,
        string base64Data,
        CancellationToken cancellationToken)
    {
        var base64 = base64Data.Contains(',')
            ? base64Data[(base64Data.IndexOf(',') + 1)..]
            : base64Data;

        var bytes = Convert.FromBase64String(base64);
        using var stream = new MemoryStream(bytes);
        return await _signatureStorage.UploadSignatureAsync(entryId, date, stream, cancellationToken);
    }

    private DateTime GetUkDateTimeNow()
    {
        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        var ukZone = TimeZoneInfo.FindSystemTimeZoneById(_siteOptions.TimeZone);
        return TimeZoneInfo.ConvertTimeFromUtc(utcNow, ukZone);
    }

    private DateOnly GetUkDateNow() => DateOnly.FromDateTime(GetUkDateTimeNow());

    private static string FormatDate(DateOnly date) =>
        date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

    private static string FormatTime(TimeOnly time) =>
        time.ToString("HH:mm", CultureInfo.InvariantCulture);

    private static string DescribeStatus(SiteStatus status) => status switch
    {
        SiteStatus.OnSite => "on-site",
        SiteStatus.OnSiteConditional => "on-site-conditional",
        SiteStatus.Denied => "denied",
        _ => "pending"
    };

    private static string DescribeMedical(MedicalStatus status) => status switch
    {
        MedicalStatus.Fit => "Medically Fit - No Conditions",
        MedicalStatus.NotFit => "Not Fit - Entry Denied",
        MedicalStatus.Conditional => "Conditional Entry - Authorised by Site Controller",
        _ => string.Empty
    };

    private static string CsvEscape(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";

        return value;
    }
}
