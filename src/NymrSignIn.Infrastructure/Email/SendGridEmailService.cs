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
    private readonly ILogger<SendGridEmailService> _logger;

    public SendGridEmailService(
        ISendGridClient sendGridClient,
        IOptions<EmailOptions> options,
        ILogger<SendGridEmailService> logger)
    {
        _sendGridClient = sendGridClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendDailyRegisterEmailAsync(
        IReadOnlyList<SiteRegisterEntry> entries,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        var signedIn = entries.Where(e => !e.IsSignedOut).ToList();
        var signedOut = entries.Where(e => e.IsSignedOut).ToList();
        var dateStr = date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

        var body = BuildEmailBody(entries.Count, signedOut.Count, signedIn, dateStr);
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
            "SendGrid response for {Date}: {StatusCode}",
            date,
            response.StatusCode);
    }

    private static string BuildEmailBody(
        int totalCount,
        int signedOutCount,
        List<SiteRegisterEntry> stillOnSite,
        string dateStr)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Daily site register — {dateStr}");
        sb.AppendLine();
        sb.AppendLine($"Total sign-ins today: {totalCount}");
        sb.AppendLine($"Signed out: {signedOutCount}");
        sb.AppendLine($"Still on site: {stillOnSite.Count}");

        if (stillOnSite.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("WARNING — THE FOLLOWING PEOPLE HAVE NOT SIGNED OUT:");
            foreach (var entry in stillOnSite)
            {
                sb.AppendLine($"  - {entry.Name} ({entry.Organisation}) — signed in at {entry.TimeIn:HH:mm}");
            }
        }

        return sb.ToString();
    }

    private static string BuildCsvAttachment(IReadOnlyList<SiteRegisterEntry> entries)
    {
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

        return sb.ToString();
    }

    private static string CsvEscape(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";

        return value;
    }
}
