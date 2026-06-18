namespace Publisher.Core.Models;

/// <summary>Preview of the parameterized INSERT statement that will run against a site DB.</summary>
public sealed class InsertPreview
{
    public string Sql { get; set; } = string.Empty;
    public IReadOnlyList<string> ParameterNames { get; set; } = Array.Empty<string>();
}
