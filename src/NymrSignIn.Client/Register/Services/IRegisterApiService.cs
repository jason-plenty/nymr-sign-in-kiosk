using NymrSignIn.Client.Register.Models;

namespace NymrSignIn.Client.Register.Services;

public interface IRegisterApiService
{
    Task<RegisterViewModel> GetTodaysRegisterAsync(CancellationToken cancellationToken = default);
    Task<SignInResponseModel> SignInAsync(SignInFormModel model, CancellationToken cancellationToken = default);
    Task<ConfirmFitResponseModel> ConfirmFitAsync(Guid id, string? additionalInfo, string? siteCode, CancellationToken cancellationToken = default);
    Task<DeclareNotFitResponseModel> DeclareNotFitAsync(Guid id, string? additionalInfo, CancellationToken cancellationToken = default);
    Task<SiteCodeSubmissionResult> SubmitSiteCodeAsync(Guid id, string siteCode, string? additionalInfo, CancellationToken cancellationToken = default);
    Task<SignOutResponseModel> SignOutAsync(Guid id, CancellationToken cancellationToken = default);
    Task<byte[]> ExportCsvAsync(CancellationToken cancellationToken = default);
    Task SendDailyEmailAsync(CancellationToken cancellationToken = default);
}

public sealed record SiteCodeSubmissionResult(bool IsValid, ConfirmFitResponseModel? Confirmation, string? ErrorMessage);
