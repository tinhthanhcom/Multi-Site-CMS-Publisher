namespace Publisher.Core.Models;

/// <summary>Result of testing a connection to a remote site database.</summary>
public sealed class ConnectionTestResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    /// <summary>True if the minimum INSERT permission on the target table was verified.</summary>
    public bool CanInsert { get; set; }
    public long DurationMs { get; set; }
}
