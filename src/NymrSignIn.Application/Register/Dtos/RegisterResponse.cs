namespace NymrSignIn.Application.Register.Dtos;

public sealed record RegisterResponse(
    IReadOnlyList<SignedInEntryDto> SignedIn,
    IReadOnlyList<SignedOutEntryDto> SignedOut);
