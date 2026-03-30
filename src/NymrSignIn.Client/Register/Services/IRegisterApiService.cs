using NymrSignIn.Client.Register.Models;

namespace NymrSignIn.Client.Register.Services;

public interface IRegisterApiService
{
    Task<RegisterViewModel> GetTodaysRegisterAsync(CancellationToken cancellationToken = default);
    Task<SignInResponseModel> SignInAsync(SignInFormModel model, CancellationToken cancellationToken = default);
    Task<SignOutResponseModel> SignOutAsync(Guid id, CancellationToken cancellationToken = default);
    Task<byte[]> ExportCsvAsync(CancellationToken cancellationToken = default);
    Task SendDailyEmailAsync(CancellationToken cancellationToken = default);
}
