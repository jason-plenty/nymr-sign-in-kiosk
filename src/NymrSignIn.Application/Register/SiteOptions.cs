namespace NymrSignIn.Application.Register;

public sealed class SiteOptions
{
    public const string SectionName = "Site";

    public string Name { get; init; } = "Construction Site";
    public string TimeZone { get; init; } = "GMT Standard Time";
    public string SiteCodePrefix { get; init; } = "SITE";

    public SiteControllerOptions SiteController { get; init; } = new();
}

public sealed class SiteControllerOptions
{
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
}
