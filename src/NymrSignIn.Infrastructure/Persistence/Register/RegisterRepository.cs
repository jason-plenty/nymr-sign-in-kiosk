using Microsoft.EntityFrameworkCore;
using NymrSignIn.Application.Register;
using NymrSignIn.Application.Register.Admin;
using NymrSignIn.Application.Register.Admin.Dtos;
using NymrSignIn.Domain.Register;

namespace NymrSignIn.Infrastructure.Persistence.Register;

public sealed class RegisterRepository : IRegisterRepository
{
    private readonly AppDbContext _context;

    public RegisterRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<SiteRegisterEntry>> GetEntriesByDateAsync(
        DateOnly date,
        CancellationToken cancellationToken)
    {
        return await _context.SiteRegisterEntries
            .AsNoTracking()
            .Where(e => e.DateIn == date)
            .OrderBy(e => e.TimeIn)
            .ToListAsync(cancellationToken);
    }

    public async Task<SiteRegisterEntry?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return await _context.SiteRegisterEntries
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task AddAsync(
        SiteRegisterEntry entry,
        CancellationToken cancellationToken)
    {
        _context.SiteRegisterEntries.Add(entry);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        SiteRegisterEntry entry,
        CancellationToken cancellationToken)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SiteRegisterEntry>> ListFilteredAsync(
        RegisterSearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        var query = _context.SiteRegisterEntries.AsNoTracking().AsQueryable();
        query = ApplyFilters(query, criteria);
        query = ApplySort(query, criteria.SortBy, criteria.SortDescending);

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<RegisterEntryDto>> SearchAsync(
        RegisterSearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        var query = _context.SiteRegisterEntries.AsNoTracking().AsQueryable();

        query = ApplyFilters(query, criteria);

        var totalCount = await query.CountAsync(cancellationToken);

        query = ApplySort(query, criteria.SortBy, criteria.SortDescending);

        var skip = (criteria.Page - 1) * criteria.PageSize;

        var items = await query
            .Skip(skip)
            .Take(criteria.PageSize)
            .Select(e => new RegisterEntryDto(
                e.Id,
                e.Name,
                e.Organisation,
                e.DateIn,
                e.TimeIn,
                e.TimeOut,
                e.TimeOut != null,
                e.SignatureUrl,
                e.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedResult<RegisterEntryDto>(items, totalCount, criteria.Page, criteria.PageSize);
    }

    private static IQueryable<SiteRegisterEntry> ApplyFilters(
        IQueryable<SiteRegisterEntry> query,
        RegisterSearchCriteria criteria)
    {
        if (criteria.FromDate.HasValue)
        {
            var from = criteria.FromDate.Value;
            query = query.Where(e => e.DateIn >= from);
        }

        if (criteria.ToDate.HasValue)
        {
            var to = criteria.ToDate.Value;
            query = query.Where(e => e.DateIn <= to);
        }

        query = criteria.Status switch
        {
            RegisterEntryStatus.StillOnSite => query.Where(e => e.TimeOut == null),
            RegisterEntryStatus.SignedOut => query.Where(e => e.TimeOut != null),
            _ => query
        };

        if (!string.IsNullOrWhiteSpace(criteria.Search))
        {
            var pattern = $"%{EscapeLikePattern(criteria.Search.Trim())}%";
            query = query.Where(e =>
                EF.Functions.Like(e.Name, pattern, "\\") ||
                EF.Functions.Like(e.Organisation, pattern, "\\"));
        }

        return query;
    }

    private static IOrderedQueryable<SiteRegisterEntry> ApplySort(
        IQueryable<SiteRegisterEntry> query,
        RegisterSortField sortBy,
        bool descending)
    {
        IOrderedQueryable<SiteRegisterEntry> ordered = sortBy switch
        {
            RegisterSortField.DateIn => descending
                ? query.OrderByDescending(e => e.DateIn)
                : query.OrderBy(e => e.DateIn),
            RegisterSortField.TimeIn => descending
                ? query.OrderByDescending(e => e.DateIn).ThenByDescending(e => e.TimeIn)
                : query.OrderBy(e => e.DateIn).ThenBy(e => e.TimeIn),
            RegisterSortField.TimeOut => descending
                ? query.OrderByDescending(e => e.TimeOut)
                : query.OrderBy(e => e.TimeOut),
            RegisterSortField.Name => descending
                ? query.OrderByDescending(e => e.Name)
                : query.OrderBy(e => e.Name),
            RegisterSortField.Organisation => descending
                ? query.OrderByDescending(e => e.Organisation)
                : query.OrderBy(e => e.Organisation),
            RegisterSortField.IsSignedOut => descending
                ? query.OrderByDescending(e => e.TimeOut != null)
                : query.OrderBy(e => e.TimeOut != null),
            _ => query.OrderByDescending(e => e.DateIn).ThenByDescending(e => e.TimeIn)
        };

        return ordered.ThenBy(e => e.Id);
    }

    private static string EscapeLikePattern(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("%", "\\%")
            .Replace("_", "\\_");
    }
}
