namespace NymrSignIn.Infrastructure.Email;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public string FromAddress { get; init; } = "noreply@nymr.co.uk";
    public string FromDisplayName { get; init; } = "NYMR Sign-In Kiosk";
    public List<string> ToAddresses { get; init; } = [];
    public string SiteName { get; init; } = "Construction Site";

    public List<string> NotFitAlertToAddresses { get; init; } = [];

    public GraphEmailCredentials Graph { get; init; } = new();
}

public sealed class GraphEmailCredentials
{
    public string TenantId { get; init; } = string.Empty;
    public string ClientId { get; init; } = string.Empty;
    public string ClientSecret { get; init; } = string.Empty;
}
