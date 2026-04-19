using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;
using NymrSignIn.Application.Register.Admin.Dtos;
using NymrSignIn.Domain.Register;

namespace NymrSignIn.Application.Register.Admin;

public sealed class AdminRegisterService
{
    private readonly IRegisterRepository _repository;
    private readonly ILogger<AdminRegisterService> _logger;

    public AdminRegisterService(
        IRegisterRepository repository,
        ILogger<AdminRegisterService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<PagedResult<RegisterEntryDto>> SearchAsync(
        RegisterSearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Admin register search: From={FromDate}, To={ToDate}, Status={Status}, Search={Search}, Page={Page}, PageSize={PageSize}",
            criteria.FromDate,
            criteria.ToDate,
            criteria.Status,
            criteria.Search,
            criteria.Page,
            criteria.PageSize);

        return await _repository.SearchAsync(criteria, cancellationToken);
    }

    public async Task<byte[]> ExportCsvAsync(
        RegisterSearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Admin register export: From={FromDate}, To={ToDate}, Status={Status}, Search={Search}",
            criteria.FromDate,
            criteria.ToDate,
            criteria.Status,
            criteria.Search);

        var entries = await _repository.ListFilteredAsync(criteria, cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine("Name,Organisation,Date,Time In,Time Out,Medical Status,Site Code,Additional Information,Status");

        foreach (var entry in entries)
        {
            var timeOut = entry.Status == SiteStatus.Denied
                ? "ENTRY DENIED"
                : entry.IsSignedOut
                    ? entry.TimeOut!.Value.ToString("HH:mm", CultureInfo.InvariantCulture)
                    : "STILL ON SITE";

            sb.AppendLine(string.Join(",",
                CsvEscape(entry.Name),
                CsvEscape(entry.Organisation),
                entry.DateIn.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
                entry.TimeIn.ToString("HH:mm", CultureInfo.InvariantCulture),
                timeOut,
                CsvEscape(DescribeMedical(entry.MedicalStatus)),
                CsvEscape(entry.SiteCode ?? string.Empty),
                CsvEscape(entry.AdditionalInfo ?? string.Empty),
                CsvEscape(DescribeStatus(entry.Status))));
        }

        return Encoding.UTF8.GetPreamble()
            .Concat(Encoding.UTF8.GetBytes(sb.ToString()))
            .ToArray();
    }

    private static string DescribeMedical(MedicalStatus status) => status switch
    {
        MedicalStatus.Fit => "Medically Fit - No Conditions",
        MedicalStatus.NotFit => "Not Fit - Entry Denied",
        MedicalStatus.Conditional => "Conditional Entry - Authorised by Site Controller",
        _ => string.Empty
    };

    private static string DescribeStatus(SiteStatus status) => status switch
    {
        SiteStatus.OnSite => "on-site",
        SiteStatus.OnSiteConditional => "on-site-conditional",
        SiteStatus.Denied => "denied",
        _ => "pending"
    };

    private static string CsvEscape(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";

        return value;
    }
}
