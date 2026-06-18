using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Publisher.Core.Entities;
using Publisher.Core.Enums;
using Publisher.Core.Options;
using Publisher.Infrastructure.Auditing;
using Publisher.Infrastructure.Data;
using Publisher.Infrastructure.Publishing;
using Publisher.Infrastructure.Security;
using Publisher.Infrastructure.Sites;

namespace Publisher.Tests;

/// <summary>
/// Wave 4 verification: exercises the full manual-publishing path against a LIVE LocalDB.
/// REQUIRES (localdb)\MSSQLLocalDB with the seeded PublisherApp AppDB (migrations applied,
/// admin/Admin@123). Creates a throwaway target DB "TargetSiteDemo" with a dbo.Articles table.
/// These tests are tagged Category=Integration so they are distinguishable from the unit tests.
/// </summary>
[Trait("Category", "Integration")]
public sealed class PublishEndToEndTests
{
    // Dev encryption key the Web app uses in Development (32-char raw -> AES-256-GCM key).
    private const string DevKey = "dev0123456789abcdef0123456789abc";

    private const string AppDbConnString =
        "Server=(localdb)\\MSSQLLocalDB;Database=PublisherApp;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True";

    private const string MasterConnString =
        "Server=(localdb)\\MSSQLLocalDB;Database=master;Trusted_Connection=True;TrustServerCertificate=True";

    private const string TargetDbName = "TargetSiteDemo";

    private const string TargetConnString =
        "Server=(localdb)\\MSSQLLocalDB;Database=TargetSiteDemo;Trusted_Connection=True;TrustServerCertificate=True";

    private static ConnectionStringEncryptor BuildEncryptor() =>
        new(Options.Create(new EncryptionOptions { Key = DevKey }));

    private static AppDbContext NewAppDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(AppDbConnString)
            .Options;
        return new AppDbContext(options);
    }

    /// <summary>Idempotently ensures TargetSiteDemo exists with a clean-schema dbo.Articles table.</summary>
    private static async Task EnsureTargetDbAsync()
    {
        await using (var master = new SqlConnection(MasterConnString))
        {
            await master.OpenAsync();
            await master.ExecuteAsync(
                "IF DB_ID(N'" + TargetDbName + "') IS NULL CREATE DATABASE [" + TargetDbName + "];");
        }

        await using var target = new SqlConnection(TargetConnString);
        await target.OpenAsync();
        await target.ExecuteAsync(@"
IF OBJECT_ID(N'dbo.Articles', N'U') IS NULL
CREATE TABLE dbo.Articles (
  Id INT IDENTITY(1,1) PRIMARY KEY,
  Title NVARCHAR(500) NOT NULL,
  Body NVARCHAR(MAX) NOT NULL,
  UrlSlug NVARCHAR(500) NULL,
  Summary NVARCHAR(1000) NULL,
  IsPublished BIT NOT NULL DEFAULT 0,
  PublishedDate DATETIME2 NULL,
  CreatedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);");
    }

    private static async Task<int> GetAdminIdAsync(AppDbContext db) =>
        await db.Users.Where(u => u.Username == "admin").Select(u => u.Id).FirstAsync();

    /// <summary>Upserts an active Site + its SiteFieldMapping for the target DB. Returns the site id.</summary>
    private static async Task<int> EnsureSiteAsync(AppDbContext db, ConnectionStringEncryptor enc, int adminId, string siteName)
    {
        var site = await db.Sites.Include(s => s.FieldMapping).FirstOrDefaultAsync(s => s.Name == siteName);
        var encrypted = enc.Encrypt(TargetConnString);

        if (site is null)
        {
            site = new Site
            {
                Name = siteName,
                DbType = "SqlServer",
                IsActive = true,
                ConnectionStringEnc = encrypted,
                CreatedBy = adminId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            db.Sites.Add(site);
            await db.SaveChangesAsync();
        }
        else
        {
            site.IsActive = true;
            site.ConnectionStringEnc = encrypted;
            site.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }

        var mapping = await db.SiteFieldMappings.FirstOrDefaultAsync(m => m.SiteId == site.Id);
        if (mapping is null)
        {
            mapping = new SiteFieldMapping { SiteId = site.Id, CreatedBy = adminId, CreatedAt = DateTime.UtcNow };
            db.SiteFieldMappings.Add(mapping);
        }

        mapping.SchemaName = "dbo";
        mapping.TableName = "Articles";
        mapping.FieldTitle = "Title";
        mapping.FieldContent = "Body";
        mapping.FieldStatus = "IsPublished";
        mapping.StatusValueDraft = "0";
        mapping.StatusValuePublished = "1";
        mapping.FieldSlug = "UrlSlug";
        mapping.FieldExcerpt = "Summary";
        mapping.FieldPublishedAt = "PublishedDate";
        mapping.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return site.Id;
    }

    private static PostPublisher BuildPublisher(AppDbContext db, ConnectionStringEncryptor enc)
    {
        var audit = new AuditLogService(db, NullLogger<AuditLogService>.Instance);
        return new PostPublisher(
            db,
            enc,
            new InsertCommandBuilder(),
            audit,
            NullLogger<PostPublisher>.Instance);
    }

    [Fact]
    public async Task FullPublishPath_InsertsRemoteRow_AndTransitionsPostToPublished()
    {
        var enc = BuildEncryptor();
        await EnsureTargetDbAsync();

        await using var db = NewAppDb();
        var adminId = await GetAdminIdAsync(db);
        var siteId = await EnsureSiteAsync(db, enc, adminId, "E2E Verify Site");

        // Unique title so reruns never collide on the remote table.
        var uniqueTitle = "E2E Article " + Guid.NewGuid().ToString("N");
        var uniqueSlug = "e2e-" + Guid.NewGuid().ToString("N");
        const string body = "<p>End-to-end body content.</p>";

        var post = new Post
        {
            SiteId = siteId,
            Title = uniqueTitle,
            Slug = uniqueSlug,
            Content = body,
            Excerpt = "E2E excerpt",
            Status = PostStatuses.Draft,
            CreatedBy = adminId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        db.Posts.Add(post);
        await db.SaveChangesAsync();

        try
        {
            // Act
            var result = await BuildPublisher(db, enc).PublishAsync(post.Id, adminId);

            // Assert: publish result
            Assert.True(result.Success, "Publish should succeed. Error: " + (result.Error ?? "<none>"));
            Assert.False(string.IsNullOrWhiteSpace(result.RemotePostId), "RemotePostId should be captured.");

            // Assert: AppDB post state transitions
            var reloaded = await db.Posts.AsNoTracking().FirstAsync(p => p.Id == post.Id);
            Assert.Equal(PostStatuses.Published, reloaded.Status);
            Assert.NotNull(reloaded.PublishedAt);
            Assert.Equal(result.RemotePostId, reloaded.RemotePostId);
            Assert.Null(reloaded.PublishError);

            // Assert: exactly one matching remote row with the mapped values
            await using var target = new SqlConnection(TargetConnString);
            await target.OpenAsync();
            var rows = (await target.QueryAsync(
                "SELECT Title, Body, UrlSlug, Summary, IsPublished, PublishedDate FROM dbo.Articles WHERE Title = @t;",
                new { t = uniqueTitle })).ToList();

            Assert.Single(rows);
            var row = rows[0];
            Assert.Equal(uniqueTitle, (string)row.Title);
            Assert.Equal(body, (string)row.Body);
            Assert.Equal(uniqueSlug, (string)row.UrlSlug);
            Assert.Equal("E2E excerpt", (string)row.Summary);
            Assert.True((bool)row.IsPublished);
            Assert.NotNull(row.PublishedDate);

            // RemotePostId should match the inserted identity value (strongly-typed to avoid
            // dynamic-binder ambiguity on the boxed identity column).
            var remoteRowId = await target.ExecuteScalarAsync<int>(
                "SELECT Id FROM dbo.Articles WHERE Title = @t;", new { t = uniqueTitle });
            Assert.Equal(result.RemotePostId, remoteRowId.ToString());
        }
        finally
        {
            // Clean up the remote row (idempotent best-effort).
            await using var target = new SqlConnection(TargetConnString);
            await target.OpenAsync();
            await target.ExecuteAsync("DELETE FROM dbo.Articles WHERE Title = @t;", new { t = uniqueTitle });
        }
    }

    [Fact]
    public async Task UnsafeTableName_FailsPublish_AndMarksPostFailed()
    {
        var enc = BuildEncryptor();
        await EnsureTargetDbAsync();

        await using var db = NewAppDb();
        var adminId = await GetAdminIdAsync(db);
        var siteId = await EnsureSiteAsync(db, enc, adminId, "E2E Unsafe Site");

        // Poison the mapping with an unsafe table identifier (SQL-injection attempt).
        var mapping = await db.SiteFieldMappings.FirstAsync(m => m.SiteId == siteId);
        mapping.TableName = "Articles; DROP TABLE dbo.Articles--";
        await db.SaveChangesAsync();

        var post = new Post
        {
            SiteId = siteId,
            Title = "Unsafe " + Guid.NewGuid().ToString("N"),
            Content = "body",
            Status = PostStatuses.Draft,
            CreatedBy = adminId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        db.Posts.Add(post);
        await db.SaveChangesAsync();

        try
        {
            var result = await BuildPublisher(db, enc).PublishAsync(post.Id, adminId);

            Assert.False(result.Success);
            var reloaded = await db.Posts.AsNoTracking().FirstAsync(p => p.Id == post.Id);
            Assert.Equal(PostStatuses.Failed, reloaded.Status);
            Assert.False(string.IsNullOrWhiteSpace(reloaded.PublishError));

            // Sanity: the unsafe statement never executed -> the real table still exists.
            await using var target = new SqlConnection(TargetConnString);
            await target.OpenAsync();
            var exists = await target.ExecuteScalarAsync<int>(
                "SELECT CASE WHEN OBJECT_ID(N'dbo.Articles', N'U') IS NULL THEN 0 ELSE 1 END;");
            Assert.Equal(1, exists);
        }
        finally
        {
            // Restore the mapping to a safe value so other tests / reruns are unaffected.
            await using var cleanup = NewAppDb();
            var m = await cleanup.SiteFieldMappings.FirstAsync(x => x.SiteId == siteId);
            m.TableName = "Articles";
            await cleanup.SaveChangesAsync();
            cleanup.Posts.Remove(await cleanup.Posts.FirstAsync(p => p.Id == post.Id));
            await cleanup.SaveChangesAsync();
        }
    }
}
