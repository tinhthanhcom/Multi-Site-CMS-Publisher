namespace Publisher.Core.Models;

/// <summary>Describes a table discovered in a remote site database.</summary>
public sealed class TableInfo
{
    public string Schema { get; set; } = "dbo";
    public string Name { get; set; } = string.Empty;
}
