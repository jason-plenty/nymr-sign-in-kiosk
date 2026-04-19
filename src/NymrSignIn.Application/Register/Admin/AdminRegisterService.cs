using Microsoft.Extensions.Logging;
using NymrSignIn.Application.Register.Admin.Dtos;

namespace NymrSignIn.Application.Register.Admin;

public sealed class AdminRegisterService
{
    private readonly IRegisterRepository _repository;
    private readonly ILogger<AdminRegisterService> _logger;

    public AdminRegisterService(
        IRegisterRepository repository,
        ILogger<AdminRegisterService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<PagedResult<RegisterEntryDto>> SearchAsync(
        RegisterSearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Admin register search: From={FromDate}, To={ToDate}, Status={Status}, Search={Search}, Page={Page}, PageSize={PageSize}",
            criteria.FromDate,
            criteria.ToDate,
            criteria.Status,
            criteria.Search,
            criteria.Page,
            criteria.PageSize);

        return await _repository.SearchAsync(criteria, cancellationToken);
    }
}
