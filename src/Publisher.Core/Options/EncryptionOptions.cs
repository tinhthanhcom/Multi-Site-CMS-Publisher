namespace Publisher.Core.Options;

/// <summary>
/// Options for connection-string encryption. Bound from config section "Encryption",
/// with <see cref="Key"/> resolved from env var PUBLISHER_ENCRYPTION_KEY (preferred)
/// or config fallback.
/// </summary>
/// <remarks>
/// Expected <see cref="Key"/> format: a Base64-encoded 32-byte (256-bit) key for AES-256-GCM.
/// As a fallback the implementation also accepts a raw 32-character ASCII string (treated as
/// 32 bytes). The key is validated to decode to exactly 32 bytes; otherwise a clear error is thrown.
/// NEVER log this value.
/// </remarks>
public sealed class EncryptionOptions
{
    public const string SectionName = "Encryption";

    public string Key { get; set; } = string.Empty;
}
