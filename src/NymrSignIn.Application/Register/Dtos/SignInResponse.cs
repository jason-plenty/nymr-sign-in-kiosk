namespace NymrSignIn.Application.Register.Dtos;

public sealed record SignInResponse(
    Guid Id,
    string Name,
    string Organisation,
    string DateIn,
    string TimeIn);
