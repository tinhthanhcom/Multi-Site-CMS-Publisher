using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Publisher.Core.Entities;

namespace Publisher.Infrastructure.Data.Configurations;

public sealed class PostConfiguration : IEntityTypeConfiguration<Post>
{
    public void Configure(EntityTypeBuilder<Post> b)
    {
        b.ToTable("Posts", t => t.HasCheckConstraint(
            "CK_Posts_Status", "[Status] IN ('draft', 'scheduled', 'publishing', 'published', 'failed')"));

        b.HasKey(x => x.Id).HasName("PK_Posts");
        b.Property(x => x.Id).ValueGeneratedOnAdd();

        b.Property(x => x.SiteId).HasColumnType("int").IsRequired();
        b.Property(x => x.Title).HasColumnType("nvarchar(500)").IsRequired();
        b.Property(x => x.Slug).HasColumnType("nvarchar(500)");
        b.Property(x => x.Content).HasColumnType("nvarchar(max)").IsRequired();
        b.Property(x => x.Excerpt).HasColumnType("nvarchar(1000)");
        b.Property(x => x.Thumbnail).HasColumnType("nvarchar(500)");
        b.Property(x => x.CategoryId).HasColumnType("nvarchar(50)");
        b.Property(x => x.AuthorId).HasColumnType("nvarchar(50)");
        b.Property(x => x.SeoTitle).HasColumnType("nvarchar(300)");
        b.Property(x => x.SeoDescription).HasColumnType("nvarchar(500)");
        b.Property(x => x.CustomDataJson).HasColumnType("nvarchar(max)");
        b.Property(x => x.Language).HasColumnType("nvarchar(10)").IsRequired().HasDefaultValue("vi");
        b.Property(x => x.TranslationGroupId).HasColumnType("uniqueidentifier");
        b.Property(x => x.Status).HasColumnType("nvarchar(20)").IsRequired().HasDefaultValue("draft");
        b.Property(x => x.ScheduledAt).HasColumnType("datetime2");
        b.Property(x => x.PublishedAt).HasColumnType("datetime2");
        b.Property(x => x.RemotePostId).HasColumnType("nvarchar(50)");
        b.Property(x => x.PublishError).HasColumnType("nvarchar(1000)");
        b.Property(x => x.RetryCount).HasColumnType("int").IsRequired().HasDefaultValue(0);
        b.Property(x => x.IsAIGenerated).HasColumnType("bit").IsRequired().HasDefaultValue(false);
        b.Property(x => x.AIPromptUsed).HasColumnType("nvarchar(max)");
        b.Property(x => x.AITokensUsed).HasColumnType("int");
        b.Property(x => x.CreatedBy).HasColumnType("int").IsRequired();
        b.Property(x => x.CreatedAt).HasColumnType("datetime2").IsRequired().HasDefaultValueSql("GETUTCDATE()");
        b.Property(x => x.UpdatedAt).HasColumnType("datetime2").IsRequired().HasDefaultValueSql("GETUTCDATE()");

        b.HasIndex(x => x.SiteId).HasDatabaseName("IX_Posts_SiteId");
        b.HasIndex(x => x.Status).HasDatabaseName("IX_Posts_Status");
        b.HasIndex(x => x.ScheduledAt)
            .HasDatabaseName("IX_Posts_ScheduledAt")
            .HasFilter("[Status] = 'scheduled'");
        b.HasIndex(x => x.TranslationGroupId)
            .HasDatabaseName("IX_Posts_TranslationGroupId")
            .HasFilter("[TranslationGroupId] IS NOT NULL");

        b.HasOne(x => x.Site)
            .WithMany(s => s.Posts)
            .HasForeignKey(x => x.SiteId)
            .HasConstraintName("FK_Posts_Site")
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.CreatedByUser)
            .WithMany(u => u.CreatedPosts)
            .HasForeignKey(x => x.CreatedBy)
            .HasConstraintName("FK_Posts_CreatedBy")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
