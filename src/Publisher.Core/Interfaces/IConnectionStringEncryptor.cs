namespace Publisher.Core.Interfaces;

/// <summary>Encrypts/decrypts site connection strings using AES-256-GCM.</summary>
public interface IConnectionStringEncryptor
{
    /// <summary>Encrypts a plaintext connection string. Returns base64(nonce || ciphertext || tag).</summary>
    string Encrypt(string plain);

    /// <summary>Decrypts a value previously produced by <see cref="Encrypt"/>.</summary>
    string Decrypt(string enc);
}
