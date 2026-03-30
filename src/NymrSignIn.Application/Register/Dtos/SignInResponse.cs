namespace NymrSignIn.Application.Register.Dtos;

public sealed record SignInResponse(
    Guid Id,
    string DateIn,
    string TimeIn);
