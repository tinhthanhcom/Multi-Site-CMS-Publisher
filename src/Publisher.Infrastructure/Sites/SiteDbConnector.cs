using System.Data;
using System.Diagnostics;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Publisher.Core.Entities;
using Publisher.Core.Interfaces;
using Publisher.Core.Models;
using Publisher.Infrastructure.Data;
using Publisher.Infrastructure.Security;

namespace Publisher.Infrastructure.Sites;

/// <summary>
/// Dapper-based connector to a remote site's SQL Server database. Loads sites from the app DB,
/// decrypts their connection string via <see cref="IConnectionStringEncryptor"/>, and inspects
/// schema. Connection strings/secrets are NEVER logged or audited.
/// </summary>
public sealed class SiteDbConnector : ISiteDbConnector
{
    private const int DefaultConnectTimeoutSeconds = 10;

    private readonly AppDbContext _db;
    private readonly IConnectionStringEncryptor _encryptor;
    private readonly ILogger<SiteDbConnector>? _logger;
    private readonly IAuditLogService? _audit;

    public SiteDbConnector(
        AppDbContext db,
        IConnectionStringEncryptor encryptor,
        ILogger<SiteDbConnector>? logger = null,
        IAuditLogService? audit = null)
    {
        _db = db;
        _encryptor = encryptor;
        _logger = logger;
        _audit = audit;
    }

    public async Task<ConnectionTestResult> TestConnectionAsync(int siteId, CancellationToken ct = default)
    {
        var site = await _db.Sites.FirstOrDefaultAsync(s => s.Id == siteId, ct).ConfigureAwait(false);
        if (site is null)
        {
            return new ConnectionTestResult
            {
                Success = false,
                Error = $"Site {siteId} was not found.",
            };
        }

        string connectionString;
        try
        {
            connectionString = _encryptor.Decrypt(site.ConnectionStringEnc);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to decrypt connection string for site {SiteId}", siteId);
            var failResult = new ConnectionTestResult
            {
                Success = false,
                Error = "Failed to decrypt the stored connection string.",
            };
            await PersistAndAuditAsync(site, failResult, ct).ConfigureAwait(false);
            return failResult;
        }

        var result = await TestConnectionAsync(connectionString, ct).ConfigureAwait(false);
        await PersistAndAuditAsync(site, result, ct).ConfigureAwait(false);
        return result;
    }

    public async Task<ConnectionTestResult> TestConnectionAsync(string connectionString, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var connStr = WithConnectTimeout(connectionString);
            await using var conn = new SqlConnection(connStr);
            await conn.OpenAsync(ct).ConfigureAwait(false);

            var one = await conn.ExecuteScalarAsync<int>(
                new CommandDefinition("SELECT 1;", cancellationToken: ct)).ConfigureAwait(false);

            // Best-effort INSERT permission probe at the database level. If the probe fails or is
            // inconclusive, fall back to the SELECT-1 success — a deeper per-table INSERT permission
            // check happens later at mapping-validation time.
            bool canInsert = one == 1;
            try
            {
                var perm = await conn.ExecuteScalarAsync<int?>(
                    new CommandDefinition(
                        "SELECT CONVERT(int, HAS_PERMS_BY_NAME(DB_NAME(), 'DATABASE', 'INSERT'));",
                        cancellationToken: ct)).ConfigureAwait(false);
                if (perm.HasValue)
                    canInsert = perm.Value == 1;
            }
            catch (Exception ex)
            {
                _logger?.LogDebug(ex, "INSERT permission probe failed; falling back to SELECT-1 success.");
            }

            sw.Stop();
            return new ConnectionTestResult
            {
                Success = one == 1,
                CanInsert = canInsert,
                DurationMs = sw.ElapsedMilliseconds,
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            // Never echo the connection string. ex.Message from SqlException does not contain it.
            return new ConnectionTestResult
            {
                Success = false,
                Error = ex.Message,
                CanInsert = false,
                DurationMs = sw.ElapsedMilliseconds,
            };
        }
    }

    public async Task<IReadOnlyList<TableInfo>> GetTablesAsync(int siteId, CancellationToken ct = default)
    {
        var connectionString = await ResolveConnectionStringAsync(siteId, ct).ConfigureAwait(false);
        await using var conn = new SqlConnection(WithConnectTimeout(connectionString));
        await conn.OpenAsync(ct).ConfigureAwait(false);

        const string sql = @"
SELECT TABLE_SCHEMA AS [Schema], TABLE_NAME AS [Name]
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_SCHEMA, TABLE_NAME;";

        var rows = await conn.QueryAsync<TableInfo>(
            new CommandDefinition(sql, cancellationToken: ct)).ConfigureAwait(false);
        return rows.ToList();
    }

    public async Task<IReadOnlyList<ColumnInfo>> GetColumnsAsync(int siteId, string schema, string table, CancellationToken ct = default)
    {
        // Validate identifiers before they are placed anywhere near the query / COLUMNPROPERTY.
        SafeIdentifier.Validate(schema, nameof(schema));
        SafeIdentifier.Validate(table, nameof(table));

        var connectionString = await ResolveConnectionStringAsync(siteId, ct).ConfigureAwait(false);
        await using var conn = new SqlConnection(WithConnectTimeout(connectionString));
        await conn.OpenAsync(ct).ConfigureAwait(false);

        // IsIdentity via COLUMNPROPERTY(OBJECT_ID([schema].[table]), col, 'IsIdentity').
        // The schema.table is parameterized into OBJECT_ID; column name comes from the catalog row.
        const string sql = @"
SELECT
    c.COLUMN_NAME AS [Name],
    c.DATA_TYPE AS [DataType],
    CASE WHEN c.IS_NULLABLE = 'YES' THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS [IsNullable],
    c.CHARACTER_MAXIMUM_LENGTH AS [MaxLength],
    CASE WHEN COLUMNPROPERTY(OBJECT_ID(QUOTENAME(c.TABLE_SCHEMA) + '.' + QUOTENAME(c.TABLE_NAME)), c.COLUMN_NAME, 'IsIdentity') = 1
         THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS [IsIdentity]
FROM INFORMATION_SCHEMA.COLUMNS c
WHERE c.TABLE_SCHEMA = @schema AND c.TABLE_NAME = @table
ORDER BY c.ORDINAL_POSITION;";

        var rows = await conn.QueryAsync<ColumnInfo>(
            new CommandDefinition(sql, new { schema, table }, cancellationToken: ct)).ConfigureAwait(false);
        return rows.ToList();
    }

    private async Task<string> ResolveConnectionStringAsync(int siteId, CancellationToken ct)
    {
        var site = await _db.Sites.FirstOrDefaultAsync(s => s.Id == siteId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Site {siteId} was not found.");
        return _encryptor.Decrypt(site.ConnectionStringEnc);
    }

    private async Task PersistAndAuditAsync(Site site, ConnectionTestResult result, CancellationToken ct)
    {
        site.LastConnectionTest = DateTime.UtcNow;
        site.LastConnectionStatus = result.Success;
        site.LastConnectionError = result.Success ? null : result.Error;

        try
        {
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to persist connection-test status for site {SiteId}", site.Id);
        }

        if (_audit is not null)
        {
            // NEVER include the connection string in the audit details.
            var details = result.Success
                ? $"Connection OK (CanInsert={result.CanInsert})."
                : "Connection failed.";
            await _audit.WriteAsync(
                action: "CONNECTION_TEST",
                siteId: site.Id,
                entityType: nameof(Site),
                entityId: site.Id.ToString(),
                details: details,
                isSuccess: result.Success,
                errorMessage: result.Success ? null : result.Error,
                durationMs: (int)Math.Min(result.DurationMs, int.MaxValue),
                ct: ct).ConfigureAwait(false);
        }
    }

    /// <summary>Ensures a sensible connect timeout without mutating other settings.</summary>
    private static string WithConnectTimeout(string connectionString)
    {
        var builder = new SqlConnectionStringBuilder(connectionString);
        // Only impose our default when the caller did not specify a connect timeout.
        if (!builder.ContainsKey("Connect Timeout") && !builder.ContainsKey("Connection Timeout"))
            builder.ConnectTimeout = DefaultConnectTimeoutSeconds;
        return builder.ConnectionString;
    }
}
