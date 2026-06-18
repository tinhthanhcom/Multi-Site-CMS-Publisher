using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Publisher.Infrastructure.Data;

/// <summary>
/// Design-time factory so `dotnet ef` can build the context without a host project.
/// Uses the LocalDB instance for migrations / database update.
/// </summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    private const string DesignTimeConnectionString =
        "Server=(localdb)\\MSSQLLocalDB;Database=PublisherApp;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True";

    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(DesignTimeConnectionString, sql =>
                sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName))
            .Options;

        return new AppDbContext(options);
    }
}
