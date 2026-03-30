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
    private readonly TimeProvider _timeProvider;
    private readonly SiteOptions _siteOptions;
    private readonly ILogger<RegisterService> _logger;

    public RegisterService(
        IRegisterRepository repository,
        ISignatureStorage signatureStorage,
        IRegisterEmailService emailService,
        TimeProvider timeProvider,
        IOptions<SiteOptions> siteOptions,
        ILogger<RegisterService> logger)
    {
        _repository = repository;
        _signatureStorage = signatureStorage;
        _emailService = emailService;
        _timeProvider = timeProvider;
        _siteOptions = siteOptions.Value;
        _logger = logger;
    }

    public async Task<RegisterResponse> GetTodaysRegisterAsync(CancellationToken cancellationToken)
    {
        var today = GetUkDateNow();
        var entries = await _repository.GetEntriesByDateAsync(today, cancellationToken);

        var signedIn = entries
            .Where(e => !e.IsSignedOut)
            .OrderBy(e => e.TimeIn)
            .Select(e => new SignedInEntryDto(
                e.Id,
                e.Name,
                e.Organisation,
                e.DateIn.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
                e.TimeIn.ToString("HH:mm", CultureInfo.InvariantCulture)))
            .ToList();

        var signedOut = entries
            .Where(e => e.IsSignedOut)
            .OrderBy(e => e.TimeOut)
            .Select(e => new SignedOutEntryDto(
                e.Id,
                e.Name,
                e.Organisation,
                e.DateIn.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
                e.TimeIn.ToString("HH:mm", CultureInfo.InvariantCulture),
                e.TimeOut!.Value.ToString("HH:mm", CultureInfo.InvariantCulture)))
            .ToList();

        return new RegisterResponse(signedIn, signedOut);
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
            var signatureUrl = await UploadSignatureAsync(entry.Id, dateIn, request.SignatureBase64, cancellationToken);
            entry.SetSignatureUrl(signatureUrl);
        }

        await _repository.AddAsync(entry, cancellationToken);

        _logger.LogInformation("Person {Name} signed in at {TimeIn}", entry.Name, timeIn);

        return new SignInResponse(
            entry.Id,
            dateIn.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
            timeIn.ToString("HH:mm", CultureInfo.InvariantCulture));
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

        return new SignOutResponse(timeOut.ToString("HH:mm", CultureInfo.InvariantCulture));
    }

    public async Task<byte[]> ExportCsvAsync(CancellationToken cancellationToken)
    {
        var today = GetUkDateNow();
        var entries = await _repository.GetEntriesByDateAsync(today, cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine("Name,Organisation,Date,Time In,Time Out");

        foreach (var entry in entries.OrderBy(e => e.TimeIn))
        {
            var timeOut = entry.IsSignedOut
                ? entry.TimeOut!.Value.ToString("HH:mm", CultureInfo.InvariantCulture)
                : "STILL ON SITE";

            sb.AppendLine(string.Join(",",
                CsvEscape(entry.Name),
                CsvEscape(entry.Organisation),
                entry.DateIn.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
                entry.TimeIn.ToString("HH:mm", CultureInfo.InvariantCulture),
                timeOut));
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

    private static string CsvEscape(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";

        return value;
    }
}
