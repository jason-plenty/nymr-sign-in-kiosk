using NymrSignIn.Domain.Register;

namespace NymrSignIn.Application.Register;

public interface IRegisterEmailService
{
    Task SendDailyRegisterEmailAsync(
        IReadOnlyList<SiteRegisterEntry> entries,
        DateOnly date,
        CancellationToken cancellationToken);
}
