namespace NymrSignIn.Application.Register.Dtos;

public sealed record ConfirmFitRequest(
    string? AdditionalInfo,
    string? SiteCode);
