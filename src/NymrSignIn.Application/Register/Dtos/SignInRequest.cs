namespace NymrSignIn.Application.Register.Dtos;

public sealed record SignInRequest(
    string Name,
    string Organisation,
    string? SignatureBase64);
