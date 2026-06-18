using System.Diagnostics;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Publisher.Core.Interfaces;
using Publisher.Core.Models;
using Publisher.Infrastructure.Data;
using Publisher.Infrastructure.Publishing;

namespace Publisher.Web.Services;

/// <summary>
/// Outcome of a "test INSERT + rollback" mapping validation against a real site DB.
/// </summary>
public sealed record MappingValidationResult(bool Success, string? Error, long DurationMs)
{
    public static MappingValidationResult Ok(long ms) => new(true, null, ms);
    public static MappingValidationResult Fail(string error, long ms) => new(false, error, ms);
}

/// <summary>
/// Web-side helper that proves a <see cref="FieldMappingInput"/> works against a site's real
/// schema by executing the builder-produced INSERT inside a transaction and ALWAYS rolling back.
/// <para>
/// Nothing is ever committed. The site connection string is decrypted only in-memory to open the
/// connection and is never logged, audited, or returned to the UI.
/// </para>
/// <para>
/// This class is intentionally NOT registered in DI (to avoid touching Program.cs / Infrastructure
/// DI during concurrent work) — the page constructs it from already-injected dependencies.
/// </para>
/// </summary>
public sealed class MappingValidator
{
    private const int CommandTimeoutSeconds = 15;

    private readonly AppDbContext _db;
    private readonly IConnectionStringEncryptor _encryptor;
    private readonly InsertCommandBuilder _builder;

    public MappingValidator(AppDbContext db, IConnectionStringEncryptor encryptor, InsertCommandBuilder builder)
    {
        _db = db;
        _encryptor = encryptor;
        _builder = builder;
    }

    /// <summary>
    /// Builds the INSERT for <paramref name="mapping"/> using a sample post (draft), executes it
    /// against the site DB inside a transaction, then ROLLS BACK. Returns success/failure.
    /// </summary>
    public async Task<MappingValidationResult> ValidateAsync(int siteId, FieldMappingInput mapping, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();

        string connectionString;
        try
        {
            var site = await _db.Sites.AsNoTracking().FirstOrDefaultAsync(s => s.Id == siteId, ct).ConfigureAwait(false);
            if (site is null)
            {
                sw.Stop();
                return MappingValidationResult.Fail($"Site {siteId} was not found.", sw.ElapsedMilliseconds);
            }

            if (string.IsNullOrWhiteSpace(site.ConnectionStringEnc))
            {
                sw.Stop();
                return MappingValidationResult.Fail("Site has no connection string configured.", sw.ElapsedMilliseconds);
            }

            connectionString = _encryptor.Decrypt(site.ConnectionStringEnc);
        }
        catch (Exception)
        {
            sw.Stop();
            // Decryption/load failures: do not echo any secret material.
            return MappingValidationResult.Fail("Failed to load/decrypt the site connection string.", sw.ElapsedMilliseconds);
        }

        // Build the INSERT with a representative sample post (draft, no publish stamp).
        InsertBuildResult built;
        try
        {
            built = _builder.Build(mapping, SamplePost(), published: false, publishTimeUtc: DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            sw.Stop();
            // Builder validation errors (bad identifiers, malformed custom JSON) are safe to surface.
            return MappingValidationResult.Fail(ex.Message, sw.ElapsedMilliseconds);
        }

        try
        {
            await using var conn = new SqlConnection(WithConnectTimeout(connectionString));
            await conn.OpenAsync(ct).ConfigureAwait(false);
            await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(ct).ConfigureAwait(false);

            try
            {
                var dyn = new DynamicParameters();
                foreach (var kvp in built.Parameters)
                    dyn.Add(kvp.Key, kvp.Value);

                // Executes INSERT ...; SELECT SCOPE_IDENTITY(); inside the transaction.
                _ = await conn.ExecuteScalarAsync<string?>(
                    new CommandDefinition(built.Sql, dyn, transaction: tx, commandTimeout: CommandTimeoutSeconds, cancellationToken: ct))
                    .ConfigureAwait(false);
            }
            finally
            {
                // ALWAYS roll back — we never persist the probe row.
                await tx.RollbackAsync(ct).ConfigureAwait(false);
            }

            sw.Stop();
            return MappingValidationResult.Ok(sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            // SqlException.Message does not contain the connection string; safe to surface.
            return MappingValidationResult.Fail(ex.Message, sw.ElapsedMilliseconds);
        }
    }

    /// <summary>A representative non-empty post so NOT NULL / type constraints are exercised.</summary>
    private static PostPublishData SamplePost() => new()
    {
        Title = "Mapping validation sample title",
        Slug = "mapping-validation-sample",
        Content = "Mapping validation sample content.",
        Excerpt = "Sample excerpt.",
        Thumbnail = "https://example.invalid/sample.png",
        CategoryId = null,
        AuthorId = null,
        SeoTitle = "Sample SEO title",
        SeoDescription = "Sample SEO description.",
    };

    private static string WithConnectTimeout(string connectionString)
    {
        var builder = new SqlConnectionStringBuilder(connectionString);
        if (!builder.ContainsKey("Connect Timeout") && !builder.ContainsKey("Connection Timeout"))
            builder.ConnectTimeout = 10;
        return builder.ConnectionString;
    }
}
