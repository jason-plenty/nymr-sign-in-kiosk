namespace NymrSignIn.Application.Register.Dtos;

public sealed record SignedInEntryDto(
    Guid Id,
    string Name,
    string Organisation,
    string DateIn,
    string TimeIn,
    string MedicalStatus,
    string Status);
