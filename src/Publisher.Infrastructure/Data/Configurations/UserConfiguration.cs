using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Publisher.Core.Entities;

namespace Publisher.Infrastructure.Data.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("Users", t => t.HasCheckConstraint(
            "CK_Users_Role", "[Role] IN ('Admin', 'Editor', 'Viewer')"));

        b.HasKey(x => x.Id).HasName("PK_Users");
        b.Property(x => x.Id).ValueGeneratedOnAdd();

        b.Property(x => x.Username).HasColumnType("nvarchar(50)").IsRequired();
        b.Property(x => x.PasswordHash).HasColumnType("nvarchar(256)").IsRequired();
        b.Property(x => x.FullName).HasColumnType("nvarchar(100)").IsRequired();
        b.Property(x => x.Email).HasColumnType("nvarchar(150)");
        b.Property(x => x.Role).HasColumnType("nvarchar(20)").IsRequired().HasDefaultValue("Editor");
        b.Property(x => x.IsActive).HasColumnType("bit").IsRequired().HasDefaultValue(true);
        b.Property(x => x.LastLoginAt).HasColumnType("datetime2");
        b.Property(x => x.CreatedAt).HasColumnType("datetime2").IsRequired().HasDefaultValueSql("GETUTCDATE()");
        b.Property(x => x.UpdatedAt).HasColumnType("datetime2").IsRequired().HasDefaultValueSql("GETUTCDATE()");

        b.HasIndex(x => x.Username).IsUnique().HasDatabaseName("UQ_Users_Username");
    }
}
