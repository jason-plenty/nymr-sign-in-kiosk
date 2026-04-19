using NymrSignIn.Application.Register.Admin.Dtos;
using NymrSignIn.Domain.Register;

namespace NymrSignIn.Application.Register;

public interface IRegisterRepository
{
    Task<IReadOnlyList<SiteRegisterEntry>> GetEntriesByDateAsync(DateOnly date, CancellationToken cancellationToken);
    Task<SiteRegisterEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(SiteRegisterEntry entry, CancellationToken cancellationToken);
    Task UpdateAsync(SiteRegisterEntry entry, CancellationToken cancellationToken);
    Task<PagedResult<RegisterEntryDto>> SearchAsync(RegisterSearchCriteria criteria, CancellationToken cancellationToken);
}
