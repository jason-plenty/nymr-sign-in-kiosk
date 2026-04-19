using NymrSignIn.Client.Admin.Models;

namespace NymrSignIn.Client.Admin.Services;

public interface IAdminApiService
{
    Task<PagedResult<RegisterEntryDto>> SearchAsync(
        RegisterSearchCriteria criteria,
        CancellationToken cancellationToken = default);

    Task<byte[]> ExportCsvAsync(
        RegisterSearchCriteria criteria,
        CancellationToken cancellationToken = default);
}
