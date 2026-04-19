namespace NymrSignIn.Application.Register.Dtos;

public sealed record ConfirmFitResponse(
    Guid Id,
    string Name,
    string Organisation,
    string DateIn,
    string TimeIn,
    string Status);
