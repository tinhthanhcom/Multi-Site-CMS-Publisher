using System.Text.RegularExpressions;

namespace Publisher.Infrastructure.Security;

/// <summary>
/// SQL-injection whitelist for table/column identifiers (see docs/system-design.md §4.2).
/// Only identifiers matching <c>^[a-zA-Z_][a-zA-Z0-9_]*$</c> (max 128 chars) are accepted.
/// Validated identifiers are bracket-quoted (<c>[name]</c>) before being placed into SQL;
/// all values must still flow through parameters, never these helpers.
/// </summary>
public static partial class SafeIdentifier
{
    /// <summary>Max length of a SQL Server regular identifier.</summary>
    public const int MaxLength = 128;

    [GeneratedRegex("^[a-zA-Z_][a-zA-Z0-9_]*$", RegexOptions.CultureInvariant)]
    private static partial Regex IdentifierRegex();

    /// <summary>Returns true if <paramref name="identifier"/> is a safe, whitelisted identifier.</summary>
    public static bool IsValid(string? identifier)
    {
        if (string.IsNullOrEmpty(identifier))
            return false;
        if (identifier.Length > MaxLength)
            return false;
        return IdentifierRegex().IsMatch(identifier);
    }

    /// <summary>
    /// Validates <paramref name="identifier"/> and returns it unchanged, or throws
    /// <see cref="UnsafeIdentifierException"/> if it is null/empty/too-long/non-whitelisted.
    /// </summary>
    public static string Validate(string? identifier, string paramNameForError)
    {
        if (string.IsNullOrEmpty(identifier))
            throw new UnsafeIdentifierException(
                $"Identifier for '{paramNameForError}' is null or empty.");

        if (identifier.Length > MaxLength)
            throw new UnsafeIdentifierException(
                $"Identifier for '{paramNameForError}' exceeds the maximum length of {MaxLength} characters.");

        if (!IdentifierRegex().IsMatch(identifier))
            throw new UnsafeIdentifierException(
                $"Identifier for '{paramNameForError}' ('{identifier}') is not a valid SQL identifier. " +
                $"Only letters, digits and underscore are allowed, and it must not start with a digit.");

        return identifier;
    }

    /// <summary>Validates then bracket-quotes an identifier: <c>Posts</c> → <c>[Posts]</c>.</summary>
    public static string Quote(string? identifier, string paramNameForError = "identifier")
    {
        var valid = Validate(identifier, paramNameForError);
        return $"[{valid}]";
    }

    /// <summary>Validates and quotes a schema-qualified name: → <c>[schema].[table]</c>.</summary>
    public static string QualifiedName(string? schema, string? table)
    {
        var s = Validate(schema, "schema");
        var t = Validate(table, "table");
        return $"[{s}].[{t}]";
    }
}

/// <summary>Thrown when an identifier fails the SQL-injection whitelist.</summary>
public sealed class UnsafeIdentifierException : Exception
{
    public UnsafeIdentifierException(string message) : base(message)
    {
    }
}
