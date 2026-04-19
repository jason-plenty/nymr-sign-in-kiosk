using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NymrSignIn.Application.Register;
using NymrSignIn.Domain.Register;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace NymrSignIn.Infrastructure.Email;

public sealed class SendGridEmailService : IRegisterEmailService
{
    private readonly ISendGridClient _sendGridClient;
    private readonly EmailOptions _options;
    private readonly SiteOptions _siteOptions;
    private readonly ILogger<SendGridEmailService> _logger;

    public SendGridEmailService(
        ISendGridClient sendGridClient,
        IOptions<EmailOptions> options,
        IOptions<SiteOptions> siteOptions,
        ILogger<SendGridEmailService> logger)
    {
        _sendGridClient = sendGridClient;
        _options = options.Value;
        _siteOptions = siteOptions.Value;
        _logger = logger;
    }

    public async Task SendDailyRegisterEmailAsync(
        IReadOnlyList<SiteRegisterEntry> entries,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        var signedIn = entries.Where(e => !e.IsSignedOut && e.Status != SiteStatus.Denied).ToList();
        var signedOut = entries.Where(e => e.IsSignedOut).ToList();
        var denied = entries.Where(e => e.Status == SiteStatus.Denied).ToList();
        var conditional = entries.Where(e => e.Status == SiteStatus.OnSiteConditional).ToList();
        var dateStr = date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

        var body = BuildEmailBody(entries.Count, signedOut.Count, signedIn, conditional, denied, dateStr);
        var csvData = BuildCsvAttachment(entries);

        var message = new SendGridMessage
        {
            From = new EmailAddress(_options.FromAddress),
            Subject = $"Site Register — {_options.SiteName} — {dateStr}",
            PlainTextContent = body
        };

        foreach (var address in _options.ToAddresses)
        {
            message.AddTo(address.Trim());
        }

        var csvBytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csvData)).ToArray();
        var base64Csv = Convert.ToBase64String(csvBytes);
        message.AddAttachment(
            $"site-register-{date:yyyy-MM-dd}.csv",
            base64Csv,
            "text/csv");

        var response = await _sendGridClient.SendEmailAsync(message, cancellationToken);

        _logger.LogInformation(
            "SendGrid daily register response for {Date}: {StatusCode}",
            date,
            response.StatusCode);
    }

    public async Task SendNotFitAlertAsync(
        SiteRegisterEntry entry,
        CancellationToken cancellationToken)
    {
        var recipients = _options.NotFitAlertToAddresses
            .Select(a => a.Trim())
            .Where(a => !string.IsNullOrWhiteSpace(a))
            .ToList();

        if (!string.IsNullOrWhiteSpace(_siteOptions.SiteController.Email))
        {
            recipients.Add(_siteOptions.SiteController.Email.Trim());
        }

        recipients = recipients.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        if (recipients.Count == 0)
        {
            _logger.LogWarning(
                "Not-fit alert for entry {EntryId} not sent: no recipients configured.", entry.Id);
            return;
        }

        var dateStr = entry.DateIn.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        var timeStr = entry.TimeIn.ToString("HH:mm", CultureInfo.InvariantCulture);

        var body = new StringBuilder();
        body.AppendLine($"A person has declared NOT FIT at {_options.SiteName}.");
        body.AppendLine();
        body.AppendLine($"Name:          {entry.Name}");
        body.AppendLine($"Organisation:  {entry.Organisation}");
        body.AppendLine($"Date / Time:   {dateStr} {timeStr}");
        if (!string.IsNullOrWhiteSpace(entry.AdditionalInfo))
        {
            body.AppendLine($"Details:       {entry.AdditionalInfo}");
        }
        body.AppendLine();
        body.AppendLine($"ONE-TIME SITE CODE: {entry.SiteCodeGenerated}");
        body.AppendLine();
        body.AppendLine("If you authorise this person to enter the site, call them and read out the code.");
        body.AppendLine("They will enter it at the kiosk. The code is valid today only.");

        var message = new SendGridMessage
        {
            From = new EmailAddress(_options.FromAddress),
            Subject = $"NOT-FIT ALERT — {entry.Name} — {_options.SiteName}",
            PlainTextContent = body.ToString()
        };

        foreach (var recipient in recipients)
        {
            message.AddTo(recipient);
        }

        var response = await _sendGridClient.SendEmailAsync(message, cancellationToken);

        _logger.LogInformation(
            "SendGrid not-fit alert for entry {EntryId}: {StatusCode}",
            entry.Id,
            response.StatusCode);
    }

    private static string BuildEmailBody(
        int totalCount,
        int signedOutCount,
        List<SiteRegisterEntry> stillOnSite,
        List<SiteRegisterEntry> conditional,
        List<SiteRegisterEntry> denied,
        string dateStr)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Daily site register — {dateStr}");
        sb.AppendLine();
        sb.AppendLine($"Total sign-ins today: {totalCount}");
        sb.AppendLine($"Signed out: {signedOutCount}");
        sb.AppendLine($"Still on site: {stillOnSite.Count}");
        sb.AppendLine($"Conditional entries: {conditional.Count}");
        sb.AppendLine($"Denied: {denied.Count}");

        if (stillOnSite.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("WARNING — THE FOLLOWING PEOPLE HAVE NOT SIGNED OUT:");
            foreach (var entry in stillOnSite)
            {
                sb.AppendLine($"  - {entry.Name} ({entry.Organisation}) — signed in at {entry.TimeIn:HH:mm}");
            }
        }

        if (conditional.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("CONDITIONAL ENTRIES (authorised by Site Controller):");
            foreach (var entry in conditional)
            {
                sb.AppendLine($"  - {entry.Name} ({entry.Organisation}) — code {entry.SiteCode} — {entry.AdditionalInfo}");
            }
        }

        if (denied.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("DENIED ENTRIES:");
            foreach (var entry in denied)
            {
                sb.AppendLine($"  - {entry.Name} ({entry.Organisation}) — {entry.TimeIn:HH:mm} — {entry.AdditionalInfo}");
            }
        }

        return sb.ToString();
    }

    private static string BuildCsvAttachment(IReadOnlyList<SiteRegisterEntry> entries)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Name,Organisation,Date,Time In,Time Out,Medical Status,Site Code,Additional Information,Status");

        foreach (var entry in entries.OrderBy(e => e.TimeIn))
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

        return sb.ToString();
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
