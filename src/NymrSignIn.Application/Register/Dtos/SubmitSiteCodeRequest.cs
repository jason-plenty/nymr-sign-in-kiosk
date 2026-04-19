namespace NymrSignIn.Application.Register.Dtos;

public sealed record SubmitSiteCodeRequest(
    string SiteCode,
    string? AdditionalInfo);
