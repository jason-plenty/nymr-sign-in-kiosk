namespace NymrSignIn.Application.Register.Dtos;

public sealed record DeniedEntryDto(
    Guid Id,
    string Name,
    string Organisation,
    string DateIn,
    string TimeIn);
