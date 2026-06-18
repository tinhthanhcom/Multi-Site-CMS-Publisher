namespace Publisher.Core.Enums;

/// <summary>
/// Logical user roles. NOTE: the database stores these as exact-case strings
/// ('Admin' | 'Editor' | 'Viewer'). Entity properties keep <see cref="string"/>
/// to match the schema; use <see cref="UserRoles"/> for the canonical constants.
/// </summary>
public enum UserRole
{
    Admin,
    Editor,
    Viewer
}

/// <summary>
/// Canonical string constants for the Users.Role column (CK_Users_Role).
/// </summary>
public static class UserRoles
{
    public const string Admin = "Admin";
    public const string Editor = "Editor";
    public const string Viewer = "Viewer";

    public static readonly IReadOnlyList<string> All = new[] { Admin, Editor, Viewer };
}
