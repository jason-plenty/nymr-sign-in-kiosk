namespace NymrSignIn.Application.Register;

public sealed class SiteOptions
{
    public const string SectionName = "Site";

    public string Name { get; init; } = "Construction Site";
    public string TimeZone { get; init; } = "GMT Standard Time";
}
