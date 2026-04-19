using System.Globalization;
using System.Text;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Users.Item.SendMail;
using NymrSignIn.Application.Register;
using NymrSignIn.Domain.Register;

namespace NymrSignIn.Infrastructure.Email;

public sealed class GraphEmailService : IRegisterEmailService
{
    private static readonly string[] GraphScopes = ["https://graph.microsoft.com/.default"];

    private readonly EmailOptions _options;
    private readonly SiteOptions _siteOptions;
    private readonly ILogger<GraphEmailService> _logger;
    private readonly Lazy<GraphServiceClient?> _graphClient;

    public GraphEmailService(
        IOptions<EmailOptions> options,
        IOptions<SiteOptions> siteOptions,
        ILogger<GraphEmailService> logger)
    {
        _options = options.Value;
        _siteOptions = siteOptions.Value;
        _logger = logger;
        _graphClient = new Lazy<GraphServiceClient?>(CreateGraphClient);
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

        var body = BuildDailyBody(entries.Count, signedOut.Count, signedIn, conditional, denied, dateStr);
        var csvBytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(BuildCsv(entries))).ToArray();

        var message = BuildMessage(
            subject: $"Site Register — {_options.SiteName} — {dateStr}",
            body: body,
            recipients: _options.ToAddresses);

        message.Attachments =
        [
            new FileAttachment
            {
                OdataType = "#microsoft.graph.fileAttachment",
                Name = $"site-register-{date:yyyy-MM-dd}.csv",
                ContentType = "text/csv",
                ContentBytes = csvBytes,
            }
        ];

        await SendAsync(message, "daily register", date.ToString("yyyy-MM-dd"), cancellationToken);
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

        var message = BuildMessage(
            subject: $"NOT-FIT ALERT — {entry.Name} — {_options.SiteName}",
            body: body.ToString(),
            recipients: recipients);

        await SendAsync(message, "not-fit alert", entry.Id.ToString(), cancellationToken);
    }

    private static Message BuildMessage(string subject, string body, IEnumerable<string> recipients)
    {
        return new Message
        {
            Subject = subject,
            Body = new ItemBody
            {
                ContentType = BodyType.Text,
                Content = body,
            },
            ToRecipients = recipients
                .Where(a => !string.IsNullOrWhiteSpace(a))
                .Select(a => new Recipient
                {
                    EmailAddress = new EmailAddress { Address = a.Trim() },
                })
                .ToList(),
        };
    }

    private async Task SendAsync(Message message, string kind, string key, CancellationToken cancellationToken)
    {
        var client = _graphClient.Value;
        if (client is null)
        {
            _logger.LogWarning(
                "Graph email not configured; skipping {Kind} email for {Key}. Set Email:Graph:TenantId / ClientId / ClientSecret.",
                kind, key);
            return;
        }

        if (string.IsNullOrWhiteSpace(_options.FromAddress))
        {
            _logger.LogError("Cannot send {Kind} for {Key}: Email:FromAddress is not configured.", kind, key);
            return;
        }

        var request = new SendMailPostRequestBody
        {
            Message = message,
            SaveToSentItems = false,
        };

        try
        {
            await client.Users[_options.FromAddress]
                .SendMail
                .PostAsync(request, cancellationToken: cancellationToken);

            _logger.LogInformation("Graph {Kind} sent for {Key} from {From}", kind, key, _options.FromAddress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Graph {Kind} failed for {Key}", kind, key);
            throw;
        }
    }

    private GraphServiceClient? CreateGraphClient()
    {
        var graph = _options.Graph;
        if (string.IsNullOrWhiteSpace(graph.TenantId)
            || string.IsNullOrWhiteSpace(graph.ClientId)
            || string.IsNullOrWhiteSpace(graph.ClientSecret))
        {
            return null;
        }

        var credential = new ClientSecretCredential(graph.TenantId, graph.ClientId, graph.ClientSecret);
        return new GraphServiceClient(credential, GraphScopes);
    }

    private static string BuildDailyBody(
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

    private static string BuildCsv(IReadOnlyList<SiteRegisterEntry> entries)
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
