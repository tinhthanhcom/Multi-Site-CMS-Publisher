using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Publisher.Core.Entities;

namespace Publisher.Infrastructure.Data.Configurations;

public sealed class SiteFieldMappingConfiguration : IEntityTypeConfiguration<SiteFieldMapping>
{
    public void Configure(EntityTypeBuilder<SiteFieldMapping> b)
    {
        b.ToTable("SiteFieldMappings");

        b.HasKey(x => x.Id).HasName("PK_SiteFieldMappings");
        b.Property(x => x.Id).ValueGeneratedOnAdd();

        b.Property(x => x.SiteId).HasColumnType("int").IsRequired();
        b.Property(x => x.TableName).HasColumnType("nvarchar(128)").IsRequired();
        b.Property(x => x.SchemaName).HasColumnType("nvarchar(128)").IsRequired().HasDefaultValue("dbo");
        b.Property(x => x.FieldTitle).HasColumnType("nvarchar(128)").IsRequired();
        b.Property(x => x.FieldContent).HasColumnType("nvarchar(128)").IsRequired();
        b.Property(x => x.FieldStatus).HasColumnType("nvarchar(128)").IsRequired();
        b.Property(x => x.StatusValueDraft).HasColumnType("nvarchar(50)").IsRequired().HasDefaultValue("0");
        b.Property(x => x.StatusValuePublished).HasColumnType("nvarchar(50)").IsRequired().HasDefaultValue("1");
        b.Property(x => x.FieldSlug).HasColumnType("nvarchar(128)");
        b.Property(x => x.FieldExcerpt).HasColumnType("nvarchar(128)");
        b.Property(x => x.FieldThumbnail).HasColumnType("nvarchar(128)");
        b.Property(x => x.FieldPublishedAt).HasColumnType("nvarchar(128)");
        b.Property(x => x.FieldCategoryId).HasColumnType("nvarchar(128)");
        b.Property(x => x.FieldAuthorId).HasColumnType("nvarchar(128)");
        b.Property(x => x.FieldSortOrder).HasColumnType("nvarchar(128)");
        b.Property(x => x.FieldSeoTitle).HasColumnType("nvarchar(128)");
        b.Property(x => x.FieldSeoDescription).HasColumnType("nvarchar(128)");
        b.Property(x => x.DefaultAuthorId).HasColumnType("nvarchar(50)");
        b.Property(x => x.DefaultCategoryId).HasColumnType("nvarchar(50)");
        b.Property(x => x.CustomFieldsJson).HasColumnType("nvarchar(max)");
        b.Property(x => x.CreatedBy).HasColumnType("int").IsRequired();
        b.Property(x => x.CreatedAt).HasColumnType("datetime2").IsRequired().HasDefaultValueSql("GETUTCDATE()");
        b.Property(x => x.UpdatedAt).HasColumnType("datetime2").IsRequired().HasDefaultValueSql("GETUTCDATE()");

        // One mapping per site
        b.HasIndex(x => x.SiteId).IsUnique().HasDatabaseName("UQ_SiteFieldMappings_Site");

        b.HasOne(x => x.Site)
            .WithOne(s => s.FieldMapping)
            .HasForeignKey<SiteFieldMapping>(x => x.SiteId)
            .HasConstraintName("FK_SiteFieldMappings_Site")
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.CreatedByUser)
            .WithMany()
            .HasForeignKey(x => x.CreatedBy)
            .HasConstraintName("FK_SiteFieldMappings_User")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
