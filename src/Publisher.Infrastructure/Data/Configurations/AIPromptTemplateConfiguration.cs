using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Publisher.Core.Entities;

namespace Publisher.Infrastructure.Data.Configurations;

public sealed class AIPromptTemplateConfiguration : IEntityTypeConfiguration<AIPromptTemplate>
{
    public void Configure(EntityTypeBuilder<AIPromptTemplate> b)
    {
        b.ToTable("AIPromptTemplates");

        b.HasKey(x => x.Id).HasName("PK_AIPromptTemplates");
        b.Property(x => x.Id).ValueGeneratedOnAdd();

        b.Property(x => x.SiteId).HasColumnType("int");
        b.Property(x => x.Name).HasColumnType("nvarchar(100)").IsRequired();
        b.Property(x => x.Description).HasColumnType("nvarchar(300)");
        b.Property(x => x.ContentType).HasColumnType("nvarchar(50)").IsRequired().HasDefaultValue("article");
        b.Property(x => x.SystemPrompt).HasColumnType("nvarchar(max)");
        b.Property(x => x.UserPromptTpl).HasColumnType("nvarchar(max)").IsRequired();
        b.Property(x => x.DefaultLength).HasColumnType("int").IsRequired().HasDefaultValue(800);
        b.Property(x => x.DefaultTone).HasColumnType("nvarchar(50)").IsRequired().HasDefaultValue("seo-friendly");
        b.Property(x => x.IsActive).HasColumnType("bit").IsRequired().HasDefaultValue(true);
        b.Property(x => x.CreatedBy).HasColumnType("int").IsRequired();
        b.Property(x => x.CreatedAt).HasColumnType("datetime2").IsRequired().HasDefaultValueSql("GETUTCDATE()");
        b.Property(x => x.UpdatedAt).HasColumnType("datetime2").IsRequired().HasDefaultValueSql("GETUTCDATE()");

        b.HasOne(x => x.Site)
            .WithMany(s => s.PromptTemplates)
            .HasForeignKey(x => x.SiteId)
            .HasConstraintName("FK_AIPromptTemplates_Site")
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.CreatedByUser)
            .WithMany()
            .HasForeignKey(x => x.CreatedBy)
            .HasConstraintName("FK_AIPromptTemplates_User")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
