using Microsoft.EntityFrameworkCore;
using NymrSignIn.Application.Register;
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
}
