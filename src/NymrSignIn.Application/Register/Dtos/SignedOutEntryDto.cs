namespace NymrSignIn.Application.Register.Dtos;

public sealed record SignedOutEntryDto(
    Guid Id,
    string Name,
    string Organisation,
    string DateIn,
    string TimeIn,
    string TimeOut);
