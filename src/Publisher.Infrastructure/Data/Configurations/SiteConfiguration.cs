using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Publisher.Core.Entities;

namespace Publisher.Infrastructure.Data.Configurations;

public sealed class SiteConfiguration : IEntityTypeConfiguration<Site>
{
    public void Configure(EntityTypeBuilder<Site> b)
    {
        b.ToTable("Sites", t => t.HasCheckConstraint(
            "CK_Sites_DbType", "[DbType] IN ('SqlServer', 'MySQL')"));

        b.HasKey(x => x.Id).HasName("PK_Sites");
        b.Property(x => x.Id).ValueGeneratedOnAdd();

        b.Property(x => x.Name).HasColumnType("nvarchar(100)").IsRequired();
        b.Property(x => x.BaseUrl).HasColumnType("nvarchar(500)");
        b.Property(x => x.Description).HasColumnType("nvarchar(500)");
        b.Property(x => x.ConnectionStringEnc).HasColumnType("nvarchar(2000)").IsRequired();
        b.Property(x => x.DbType).HasColumnType("nvarchar(20)").IsRequired().HasDefaultValue("SqlServer");
        b.Property(x => x.SystemPrompt).HasColumnType("nvarchar(max)");
        b.Property(x => x.DefaultTone).HasColumnType("nvarchar(50)");
        b.Property(x => x.DefaultLanguage).HasColumnType("nvarchar(10)").IsRequired().HasDefaultValue("vi");
        b.Property(x => x.SupportedLanguagesJson).HasColumnType("nvarchar(max)");
        b.Property(x => x.IsActive).HasColumnType("bit").IsRequired().HasDefaultValue(true);
        b.Property(x => x.LastConnectionTest).HasColumnType("datetime2");
        b.Property(x => x.LastConnectionStatus).HasColumnType("bit");
        b.Property(x => x.LastConnectionError).HasColumnType("nvarchar(500)");
        b.Property(x => x.CreatedBy).HasColumnType("int").IsRequired();
        b.Property(x => x.CreatedAt).HasColumnType("datetime2").IsRequired().HasDefaultValueSql("GETUTCDATE()");
        b.Property(x => x.UpdatedAt).HasColumnType("datetime2").IsRequired().HasDefaultValueSql("GETUTCDATE()");

        b.HasIndex(x => x.Name).IsUnique().HasDatabaseName("UQ_Sites_Name");

        b.HasOne(x => x.CreatedByUser)
            .WithMany(u => u.CreatedSites)
            .HasForeignKey(x => x.CreatedBy)
            .HasConstraintName("FK_Sites_CreatedBy")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
