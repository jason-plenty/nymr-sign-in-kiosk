namespace NymrSignIn.Client.Register.Models;

public sealed record RegisterViewModel(
    IReadOnlyList<SignedInEntry> SignedIn,
    IReadOnlyList<SignedOutEntry> SignedOut);

public sealed record SignedInEntry(
    Guid Id,
    string Name,
    string Organisation,
    string DateIn,
    string TimeIn);

public sealed record SignedOutEntry(
    Guid Id,
    string Name,
    string Organisation,
    string DateIn,
    string TimeIn,
    string TimeOut);

public sealed record SignInResponseModel(
    Guid Id,
    string DateIn,
    string TimeIn);

public sealed record SignOutResponseModel(
    string TimeOut);
