using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Publisher.Core.Entities;

namespace Publisher.Infrastructure.Data.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.ToTable("AuditLogs");

        b.HasKey(x => x.Id).HasName("PK_AuditLogs");
        b.Property(x => x.Id).HasColumnType("bigint").ValueGeneratedOnAdd();

        b.Property(x => x.UserId).HasColumnType("int");
        b.Property(x => x.SiteId).HasColumnType("int");
        b.Property(x => x.Action).HasColumnType("nvarchar(50)").IsRequired();
        b.Property(x => x.EntityType).HasColumnType("nvarchar(50)");
        b.Property(x => x.EntityId).HasColumnType("nvarchar(50)");
        b.Property(x => x.Details).HasColumnType("nvarchar(max)");
        b.Property(x => x.IpAddress).HasColumnType("nvarchar(45)");
        b.Property(x => x.IsSuccess).HasColumnType("bit").IsRequired().HasDefaultValue(true);
        b.Property(x => x.ErrorMessage).HasColumnType("nvarchar(1000)");
        b.Property(x => x.DurationMs).HasColumnType("int");
        b.Property(x => x.CreatedAt).HasColumnType("datetime2").IsRequired().HasDefaultValueSql("GETUTCDATE()");

        // NOTE: No FK constraints on AuditLogs in the SQL schema (UserId/SiteId are loose
        // references so logs survive deletes). Indexes mirror database-design.sql.
        b.HasIndex(x => x.UserId).HasDatabaseName("IX_AuditLogs_UserId");
        b.HasIndex(x => x.SiteId).HasDatabaseName("IX_AuditLogs_SiteId");
        b.HasIndex(x => x.Action).HasDatabaseName("IX_AuditLogs_Action");
        b.HasIndex(x => x.CreatedAt).HasDatabaseName("IX_AuditLogs_CreatedAt");
    }
}
