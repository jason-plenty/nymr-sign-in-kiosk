namespace NymrSignIn.Infrastructure.Email;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public string SendGridApiKey { get; init; } = string.Empty;
    public string FromAddress { get; init; } = "noreply@nymr.co.uk";
    public List<string> ToAddresses { get; init; } = [];
    public string SiteName { get; init; } = "Construction Site";

    public List<string> NotFitAlertToAddresses { get; init; } = [];
}
