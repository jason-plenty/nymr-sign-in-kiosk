namespace NymrSignIn.Client.Register.Models;

public sealed record RegisterViewModel(
    IReadOnlyList<SignedInEntry> SignedIn,
    IReadOnlyList<SignedOutEntry> SignedOut,
    IReadOnlyList<DeniedEntry> Denied);

public sealed record SignedInEntry(
    Guid Id,
    string Name,
    string Organisation,
    string DateIn,
    string TimeIn,
    string MedicalStatus,
    string Status);

public sealed record SignedOutEntry(
    Guid Id,
    string Name,
    string Organisation,
    string DateIn,
    string TimeIn,
    string TimeOut);

public sealed record DeniedEntry(
    Guid Id,
    string Name,
    string Organisation,
    string DateIn,
    string TimeIn);

public sealed record SignInResponseModel(
    Guid Id,
    string Name,
    string Organisation,
    string DateIn,
    string TimeIn);

public sealed record SignOutResponseModel(
    string TimeOut);

public sealed record ConfirmFitResponseModel(
    Guid Id,
    string Name,
    string Organisation,
    string DateIn,
    string TimeIn,
    string Status);

public sealed record DeclareNotFitResponseModel(
    Guid Id,
    string SiteControllerName,
    string SiteControllerEmail,
    string SiteControllerPhone);
