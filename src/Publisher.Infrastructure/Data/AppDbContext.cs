using Microsoft.EntityFrameworkCore;
using Publisher.Core.Entities;

namespace Publisher.Infrastructure.Data;

/// <summary>EF Core context for the application database (AppDB / PublisherApp).</summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Site> Sites => Set<Site>();
    public DbSet<SiteFieldMapping> SiteFieldMappings => Set<SiteFieldMapping>();
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<AIPromptTemplate> AIPromptTemplates => Set<AIPromptTemplate>();
    public DbSet<AutoPublishSchedule> AutoPublishSchedules => Set<AutoPublishSchedule>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
