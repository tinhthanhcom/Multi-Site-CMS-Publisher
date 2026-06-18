using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Publisher.Infrastructure.Data;

/// <summary>
/// Design-time factory so `dotnet ef` can build the context without a host project.
/// The connection string is taken from the env var <c>ConnectionStrings__AppDb</c>
/// (the standard .NET config convention) when set, otherwise it falls back to the
/// local default below. Override the env var to run migrations against another server.
/// </summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    private const string DefaultConnectionString =
        "Server=localhost;Database=01MultiSiteCMS;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True";

    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__AppDb")
            ?? DefaultConnectionString;

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString, sql =>
                sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName))
            .Options;

        return new AppDbContext(options);
    }
}
