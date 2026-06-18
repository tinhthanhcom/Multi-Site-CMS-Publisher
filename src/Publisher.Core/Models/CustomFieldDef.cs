namespace Publisher.Core.Models;

/// <summary>
/// Shape of each item in SiteFieldMapping.CustomFieldsJson, e.g.
/// {"fieldName": "ViewCount", "defaultValue": "0", "dataType": "int"}.
/// </summary>
public sealed class CustomFieldDef
{
    public string FieldName { get; set; } = string.Empty;
    public string DefaultValue { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
}
