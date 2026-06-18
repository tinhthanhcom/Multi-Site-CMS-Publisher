using Publisher.Core.Models;

namespace Publisher.Core.Interfaces;

/// <summary>
/// Connects to a remote site's database (Dapper-based). Implemented later by Agent C.
/// </summary>
public interface ISiteDbConnector
{
    /// <summary>Tests connectivity (and minimum INSERT permission) for a persisted site.</summary>
    Task<ConnectionTestResult> TestConnectionAsync(int siteId, CancellationToken ct = default);

    /// <summary>
    /// Tests connectivity using a raw, already-decrypted connection string — for the
    /// "test before save" case where the site is not yet persisted.
    /// </summary>
    Task<ConnectionTestResult> TestConnectionAsync(string connectionString, CancellationToken ct = default);

    /// <summary>Lists tables available in the site's database.</summary>
    Task<IReadOnlyList<TableInfo>> GetTablesAsync(int siteId, CancellationToken ct = default);

    /// <summary>Lists columns of a given table in the site's database.</summary>
    Task<IReadOnlyList<ColumnInfo>> GetColumnsAsync(int siteId, string schema, string table, CancellationToken ct = default);
}
