namespace NymrSignIn.Application.Register.Dtos;

public sealed record DeclareNotFitResponse(
    Guid Id,
    string SiteControllerName,
    string SiteControllerEmail,
    string SiteControllerPhone);
