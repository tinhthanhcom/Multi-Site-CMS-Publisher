using System.Diagnostics;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Publisher.Core.Entities;
using Publisher.Core.Enums;
using Publisher.Core.Interfaces;
using Publisher.Core.Models;
using Publisher.Infrastructure.Data;
using Publisher.Infrastructure.Security;

namespace Publisher.Infrastructure.Publishing;

/// <summary>
/// Publishes a <see cref="Post"/> into its target site's database via the shared, parameterized
/// <see cref="InsertCommandBuilder"/>. Status transitions:
/// <para>(draft|scheduled|failed) -> publishing -> published, or -> failed on error.</para>
/// Connection strings are decrypted in-memory only and are NEVER logged, audited or persisted.
/// </summary>
public sealed class PostPublisher : IPostPublisher
{
    private const int DefaultConnectTimeoutSeconds = 15;

    private readonly AppDbContext _db;
    private readonly IConnectionStringEncryptor _encryptor;
    private readonly InsertCommandBuilder _builder;
    private readonly IAuditLogService _audit;
    private readonly ILogger<PostPublisher> _logger;

    public PostPublisher(
        AppDbContext db,
        IConnectionStringEncryptor encryptor,
        InsertCommandBuilder builder,
        IAuditLogService audit,
        ILogger<PostPublisher> logger)
    {
        _db = db;
        _encryptor = encryptor;
        _builder = builder;
        _audit = audit;
        _logger = logger;
    }

    public async Task<PublishResult> PublishAsync(int postId, int userId, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();

        // 1. Load post + its site (+ the site's field mapping). Tracked so we can persist status.
        var post = await _db.Posts
            .Include(p => p.Site!)
                .ThenInclude(s => s.FieldMapping)
            .FirstOrDefaultAsync(p => p.Id == postId, ct)
            .ConfigureAwait(false);

        if (post is null)
        {
            await _audit.WriteAsync(
                action: "POST_PUBLISH_FAILED",
                userId: userId,
                entityType: nameof(Post),
                entityId: postId.ToString(),
                details: "Post not found.",
                isSuccess: false,
                errorMessage: "Post not found.",
                ct: ct).ConfigureAwait(false);
            return new PublishResult { Success = false, Error = "Post not found." };
        }

        var site = post.Site;
        var mapping = site?.FieldMapping;

        // Guards: site must exist + be active; mapping must exist.
        string? guardError = null;
        if (site is null)
            guardError = "Site not found for this post.";
        else if (!site.IsActive)
            guardError = "Site is not active.";
        else if (mapping is null)
            guardError = "Site has no field mapping configured.";

        if (guardError is not null)
        {
            post.Status = PostStatuses.Failed;
            post.PublishError = guardError;
            post.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);

            await _audit.WriteAsync(
                action: "POST_PUBLISH_FAILED",
                userId: userId,
                siteId: post.SiteId,
                entityType: nameof(Post),
                entityId: postId.ToString(),
                details: guardError,
                isSuccess: false,
                errorMessage: guardError,
                durationMs: (int)Math.Min(sw.ElapsedMilliseconds, int.MaxValue),
                ct: ct).ConfigureAwait(false);

            return new PublishResult { Success = false, Error = guardError };
        }

        // 1b. Localized (per-language-column) sites publish the WHOLE translation group as one
        //     remote row; single-language sites publish just this post.
        var isLocalized = !string.IsNullOrWhiteSpace(mapping!.LocalizedColumnsJson);
        List<Post> targetPosts;
        if (isLocalized && post.TranslationGroupId is Guid groupId)
        {
            targetPosts = await _db.Posts
                .Where(p => p.TranslationGroupId == groupId && p.SiteId == post.SiteId)
                .ToListAsync(ct)
                .ConfigureAwait(false);
            if (targetPosts.Count == 0)
                targetPosts = new List<Post> { post };
        }
        else
        {
            targetPosts = new List<Post> { post };
        }

        // 2. Mark in-flight so the UI can reflect publishing state.
        var now = DateTime.UtcNow;
        foreach (var tp in targetPosts)
        {
            tp.Status = PostStatuses.Publishing;
            tp.UpdatedAt = now;
        }
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        try
        {
            // 3. Build the parameterized INSERT from the mapping + post data.
            var mappingInput = ToMappingInput(mapping!);
            var publishTimeUtc = DateTime.UtcNow;

            var build = isLocalized
                ? _builder.BuildLocalized(
                    mappingInput,
                    targetPosts.Select(ToPublishData).ToList(),
                    site!.DefaultLanguage,
                    published: true,
                    publishTimeUtc: publishTimeUtc)
                : _builder.Build(mappingInput, ToPublishData(post), published: true, publishTimeUtc: publishTimeUtc);

            // 4. Decrypt connection string (never logged), open connection, execute in a transaction.
            var connectionString = _encryptor.Decrypt(site!.ConnectionStringEnc);

            string? remoteId;
            await using (var conn = new SqlConnection(WithConnectTimeout(connectionString)))
            {
                await conn.OpenAsync(ct).ConfigureAwait(false);
                await using var tx = await conn.BeginTransactionAsync(ct).ConfigureAwait(false);

                var dp = new DynamicParameters();
                foreach (var kvp in build.Parameters)
                    dp.Add(kvp.Key, kvp.Value);

                remoteId = await conn.ExecuteScalarAsync<string>(
                    new CommandDefinition(build.Sql, dp, transaction: tx, cancellationToken: ct))
                    .ConfigureAwait(false);

                await tx.CommitAsync(ct).ConfigureAwait(false);
            }

            // 5. Success: persist published state for the whole group (one shared remote row).
            var publishedNow = DateTime.UtcNow;
            foreach (var tp in targetPosts)
            {
                tp.Status = PostStatuses.Published;
                tp.PublishedAt = publishTimeUtc;
                tp.RemotePostId = remoteId;
                tp.PublishError = null;
                tp.UpdatedAt = publishedNow;
            }
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);

            var langSuffix = isLocalized ? $" ({targetPosts.Count} languages)" : string.Empty;
            await _audit.WriteAsync(
                action: "POST_PUBLISHED",
                userId: userId,
                siteId: post.SiteId,
                entityType: nameof(Post),
                entityId: postId.ToString(),
                details: $"Published into [{mapping!.SchemaName}].[{mapping.TableName}]; RemotePostId={remoteId}{langSuffix}.",
                isSuccess: true,
                durationMs: (int)Math.Min(sw.ElapsedMilliseconds, int.MaxValue),
                ct: ct).ConfigureAwait(false);

            return new PublishResult { Success = true, RemotePostId = remoteId };
        }
        catch (Exception ex)
        {
            // 6. Failure (builder UnsafeIdentifierException, SqlException, decryption, etc.).
            // Sanitize: ex.Message from SqlException/SafeIdentifier does not echo the connection string.
            var error = ex.Message;
            _logger.LogError(ex, "Failed to publish post {PostId} to site {SiteId}", postId, post.SiteId);

            var failedNow = DateTime.UtcNow;
            foreach (var tp in targetPosts)
            {
                tp.Status = PostStatuses.Failed;
                tp.PublishError = error;
                tp.RetryCount += 1;
                tp.UpdatedAt = failedNow;
            }
            try
            {
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            }
            catch (Exception saveEx)
            {
                _logger.LogError(saveEx, "Failed to persist failed-publish state for post {PostId}", postId);
            }

            await _audit.WriteAsync(
                action: "POST_PUBLISH_FAILED",
                userId: userId,
                siteId: post.SiteId,
                entityType: nameof(Post),
                entityId: postId.ToString(),
                details: "Publish failed.",
                isSuccess: false,
                errorMessage: error,
                durationMs: (int)Math.Min(sw.ElapsedMilliseconds, int.MaxValue),
                ct: ct).ConfigureAwait(false);

            return new PublishResult { Success = false, Error = error };
        }
    }

    private static FieldMappingInput ToMappingInput(SiteFieldMapping m) => new()
    {
        TableName = m.TableName,
        SchemaName = m.SchemaName,
        FieldTitle = m.FieldTitle,
        FieldContent = m.FieldContent,
        FieldStatus = m.FieldStatus,
        StatusValueDraft = m.StatusValueDraft,
        StatusValuePublished = m.StatusValuePublished,
        FieldSlug = m.FieldSlug,
        FieldExcerpt = m.FieldExcerpt,
        FieldThumbnail = m.FieldThumbnail,
        FieldPublishedAt = m.FieldPublishedAt,
        FieldCategoryId = m.FieldCategoryId,
        FieldAuthorId = m.FieldAuthorId,
        FieldSortOrder = m.FieldSortOrder,
        FieldSeoTitle = m.FieldSeoTitle,
        FieldSeoDescription = m.FieldSeoDescription,
        DefaultAuthorId = m.DefaultAuthorId,
        DefaultCategoryId = m.DefaultCategoryId,
        CustomFieldsJson = m.CustomFieldsJson,
        LocalizedColumnsJson = m.LocalizedColumnsJson,
    };

    private static PostPublishData ToPublishData(Post p) => new()
    {
        Title = p.Title,
        Slug = p.Slug,
        Content = p.Content,
        Excerpt = p.Excerpt,
        Thumbnail = p.Thumbnail,
        CategoryId = p.CategoryId,
        AuthorId = p.AuthorId,
        SeoTitle = p.SeoTitle,
        SeoDescription = p.SeoDescription,
        CustomDataJson = p.CustomDataJson,
        Language = p.Language,
    };

    /// <summary>Ensures a sensible connect timeout without mutating other settings.</summary>
    private static string WithConnectTimeout(string connectionString)
    {
        var builder = new SqlConnectionStringBuilder(connectionString);
        if (!builder.ContainsKey("Connect Timeout") && !builder.ContainsKey("Connection Timeout"))
            builder.ConnectTimeout = DefaultConnectTimeoutSeconds;
        return builder.ConnectionString;
    }
}
