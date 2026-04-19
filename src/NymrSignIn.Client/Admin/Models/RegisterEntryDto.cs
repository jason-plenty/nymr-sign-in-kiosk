namespace NymrSignIn.Client.Admin.Models;

public sealed record RegisterEntryDto(
    Guid Id,
    string Name,
    string Organisation,
    DateOnly DateIn,
    TimeOnly TimeIn,
    TimeOnly? TimeOut,
    bool IsSignedOut,
    string? SignatureUrl,
    DateTimeOffset CreatedAtUtc);
