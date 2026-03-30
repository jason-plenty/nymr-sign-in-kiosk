using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NymrSignIn.Infrastructure.Persistence;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=(localdb)\\mssqllocaldb;Database=NymrSignIn;Trusted_Connection=True;MultipleActiveResultSets=true",
            sqlOptions => sqlOptions.EnableRetryOnFailure());

        return new AppDbContext(optionsBuilder.Options);
    }
}
