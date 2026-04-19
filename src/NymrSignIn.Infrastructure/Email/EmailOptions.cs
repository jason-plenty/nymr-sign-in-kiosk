namespace NymrSignIn.Infrastructure.Email;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public string FromAddress { get; init; } = "noreply@nymr.co.uk";
    public string FromDisplayName { get; init; } = "NYMR Sign-In Kiosk";
    public List<string> ToAddresses { get; init; } = [];
    public string SiteName { get; init; } = "Construction Site";

    public List<string> NotFitAlertToAddresses { get; init; } = [];

    public string SmtpHost { get; init; } = "smtp.office365.com";
    public int SmtpPort { get; init; } = 587;
    public string SmtpUsername { get; init; } = string.Empty;
    public string SmtpPassword { get; init; } = string.Empty;
}
