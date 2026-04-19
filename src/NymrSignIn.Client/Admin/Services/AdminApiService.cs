using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using NymrSignIn.Client.Admin.Models;

namespace NymrSignIn.Client.Admin.Services;

public sealed class AdminApiService : IAdminApiService
{
    private const string ClientName = "AdminApi";

    private readonly IHttpClientFactory _httpClientFactory;

    public AdminApiService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<PagedResult<RegisterEntryDto>> SearchAsync(
        RegisterSearchCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient(ClientName);
        var query = BuildQueryString(criteria);
        var url = $"api/v1/admin/register/search{query}";

        var result = await client.GetFromJsonAsync<PagedResult<RegisterEntryDto>>(url, cancellationToken);
        return result ?? new PagedResult<RegisterEntryDto>([], 0, criteria.Page, criteria.PageSize);
    }

    public async Task<byte[]> ExportCsvAsync(
        RegisterSearchCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient(ClientName);
        var query = BuildQueryString(criteria);
        var url = $"api/v1/admin/register/export{query}";
        return await client.GetByteArrayAsync(url, cancellationToken);
    }

    private static string BuildQueryString(RegisterSearchCriteria criteria)
    {
        var parts = new List<string>
        {
            $"Page={criteria.Page}",
            $"PageSize={criteria.PageSize}",
            $"Status={(int)criteria.Status}",
            $"SortBy={(int)criteria.SortBy}",
            $"SortDescending={criteria.SortDescending.ToString().ToLowerInvariant()}"
        };

        if (criteria.FromDate.HasValue)
        {
            parts.Add($"FromDate={criteria.FromDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}");
        }

        if (criteria.ToDate.HasValue)
        {
            parts.Add($"ToDate={criteria.ToDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}");
        }

        if (!string.IsNullOrWhiteSpace(criteria.Search))
        {
            parts.Add($"Search={Uri.EscapeDataString(criteria.Search.Trim())}");
        }

        return "?" + string.Join("&", parts);
    }
}
