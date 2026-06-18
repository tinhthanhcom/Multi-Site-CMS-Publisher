using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Publisher.Core.Interfaces;
using Publisher.Core.Options;

namespace Publisher.Infrastructure.Security;

/// <summary>
/// AES-256-GCM encryptor for connection strings.
/// Output format: base64( nonce(12 bytes) || ciphertext || tag(16 bytes) ).
/// NEVER logs keys or plaintext.
/// </summary>
public sealed class ConnectionStringEncryptor : IConnectionStringEncryptor
{
    private const int NonceSize = 12; // AesGcm.NonceByteSizes.MaxSize
    private const int TagSize = 16;   // AesGcm.TagByteSizes.MaxSize
    private const int KeySize = 32;   // 256-bit

    private readonly byte[] _key;

    public ConnectionStringEncryptor(IOptions<EncryptionOptions> options)
    {
        var raw = options.Value.Key;
        _key = ResolveKey(raw);
    }

    /// <summary>
    /// Resolves the 32-byte key. Accepts a Base64-encoded 32-byte key (preferred) or,
    /// as a fallback, a raw 32-character ASCII string. Throws a clear error otherwise.
    /// </summary>
    private static byte[] ResolveKey(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new InvalidOperationException(
                "Encryption key is not configured. Set PUBLISHER_ENCRYPTION_KEY (Base64 32-byte key) or the 'Encryption:Key' config value.");

        // Try Base64 first.
        if (TryDecodeBase64(raw, out var decoded) && decoded.Length == KeySize)
            return decoded;

        // Fallback: raw 32-char ASCII string.
        if (raw.Length == KeySize)
            return Encoding.UTF8.GetBytes(raw);

        throw new InvalidOperationException(
            "Encryption key must decode to exactly 32 bytes (Base64-encoded 256-bit key) or be a raw 32-character string.");
    }

    private static bool TryDecodeBase64(string value, out byte[] result)
    {
        try
        {
            result = Convert.FromBase64String(value);
            return true;
        }
        catch (FormatException)
        {
            result = Array.Empty<byte>();
            return false;
        }
    }

    public string Encrypt(string plain)
    {
        ArgumentNullException.ThrowIfNull(plain);

        var plaintext = Encoding.UTF8.GetBytes(plain);
        var nonce = new byte[NonceSize];
        RandomNumberGenerator.Fill(nonce);

        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSize];

        using (var aes = new AesGcm(_key, TagSize))
        {
            aes.Encrypt(nonce, plaintext, ciphertext, tag);
        }

        // nonce || ciphertext || tag
        var output = new byte[NonceSize + ciphertext.Length + TagSize];
        Buffer.BlockCopy(nonce, 0, output, 0, NonceSize);
        Buffer.BlockCopy(ciphertext, 0, output, NonceSize, ciphertext.Length);
        Buffer.BlockCopy(tag, 0, output, NonceSize + ciphertext.Length, TagSize);

        return Convert.ToBase64String(output);
    }

    public string Decrypt(string enc)
    {
        ArgumentNullException.ThrowIfNull(enc);

        byte[] data;
        try
        {
            data = Convert.FromBase64String(enc);
        }
        catch (FormatException ex)
        {
            throw new FormatException("Encrypted value is not valid Base64.", ex);
        }

        if (data.Length < NonceSize + TagSize)
            throw new CryptographicException("Encrypted value is too short to contain nonce and tag.");

        var nonce = new byte[NonceSize];
        var tag = new byte[TagSize];
        var ciphertextLength = data.Length - NonceSize - TagSize;
        var ciphertext = new byte[ciphertextLength];

        Buffer.BlockCopy(data, 0, nonce, 0, NonceSize);
        Buffer.BlockCopy(data, NonceSize, ciphertext, 0, ciphertextLength);
        Buffer.BlockCopy(data, NonceSize + ciphertextLength, tag, 0, TagSize);

        var plaintext = new byte[ciphertextLength];
        using (var aes = new AesGcm(_key, TagSize))
        {
            aes.Decrypt(nonce, ciphertext, tag, plaintext);
        }

        return Encoding.UTF8.GetString(plaintext);
    }
}
