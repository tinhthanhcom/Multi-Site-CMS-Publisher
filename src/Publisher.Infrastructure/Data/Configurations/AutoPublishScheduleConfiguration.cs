using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Publisher.Core.Entities;

namespace Publisher.Infrastructure.Data.Configurations;

public sealed class AutoPublishScheduleConfiguration : IEntityTypeConfiguration<AutoPublishSchedule>
{
    public void Configure(EntityTypeBuilder<AutoPublishSchedule> b)
    {
        b.ToTable("AutoPublishSchedules", t => t.HasCheckConstraint(
            "CK_AutoSchedules_Type", "[ScheduleType] IN ('daily', 'weekly', 'cron')"));

        b.HasKey(x => x.Id).HasName("PK_AutoPublishSchedules");
        b.Property(x => x.Id).ValueGeneratedOnAdd();

        b.Property(x => x.SiteId).HasColumnType("int").IsRequired();
        b.Property(x => x.Name).HasColumnType("nvarchar(100)").IsRequired();
        b.Property(x => x.Description).HasColumnType("nvarchar(300)");
        b.Property(x => x.PromptTemplateId).HasColumnType("int");
        b.Property(x => x.TopicsJson).HasColumnType("nvarchar(max)").IsRequired();
        b.Property(x => x.KeywordsJson).HasColumnType("nvarchar(max)");
        b.Property(x => x.ScheduleType).HasColumnType("nvarchar(20)").IsRequired().HasDefaultValue("daily");
        b.Property(x => x.CronExpression).HasColumnType("nvarchar(100)");
        b.Property(x => x.TimeOfDay).HasColumnType("time");
        b.Property(x => x.DayOfWeek).HasColumnType("tinyint");
        b.Property(x => x.PostsPerRun).HasColumnType("int").IsRequired().HasDefaultValue(1);
        b.Property(x => x.IsActive).HasColumnType("bit").IsRequired().HasDefaultValue(true);
        b.Property(x => x.LastRunAt).HasColumnType("datetime2");
        b.Property(x => x.LastRunStatus).HasColumnType("nvarchar(20)");
        b.Property(x => x.NextRunAt).HasColumnType("datetime2");
        b.Property(x => x.TotalPostsPublished).HasColumnType("int").IsRequired().HasDefaultValue(0);
        b.Property(x => x.CreatedBy).HasColumnType("int").IsRequired();
        b.Property(x => x.CreatedAt).HasColumnType("datetime2").IsRequired().HasDefaultValueSql("GETUTCDATE()");
        b.Property(x => x.UpdatedAt).HasColumnType("datetime2").IsRequired().HasDefaultValueSql("GETUTCDATE()");

        b.HasOne(x => x.Site)
            .WithMany(s => s.Schedules)
            .HasForeignKey(x => x.SiteId)
            .HasConstraintName("FK_AutoPublishSchedules_Site")
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.PromptTemplate)
            .WithMany()
            .HasForeignKey(x => x.PromptTemplateId)
            .HasConstraintName("FK_AutoPublishSchedules_Template")
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.CreatedByUser)
            .WithMany()
            .HasForeignKey(x => x.CreatedBy)
            .HasConstraintName("FK_AutoPublishSchedules_User")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
