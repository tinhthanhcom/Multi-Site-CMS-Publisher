namespace Publisher.Core.Models;

/// <summary>Describes a column discovered in a remote site database table.</summary>
public sealed class ColumnInfo
{
    public string Name { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public bool IsNullable { get; set; }
    public bool IsIdentity { get; set; }
    public int? MaxLength { get; set; }
}
