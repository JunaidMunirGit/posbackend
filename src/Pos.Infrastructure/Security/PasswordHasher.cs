using System;
using System.Security.Cryptography;
using Pos.Application.Abstractions.Security;

namespace Pos.Infrastructure.Security;

public sealed class PasswordHasher : IPasswordHasher
{
    // Format:
    // v1.{algo}.{iterations}.{saltB64}.{keyB64}
    // Example:
    // v1.SHA256.200000.<salt>.<key>

    private const string Version = "v1";
    private const int SaltSize = 16;   // 128-bit salt
    private const int KeySize = 32;   // 256-bit derived key

    // Pick a value that’s acceptable for your server load.
    // 200k is a common modern baseline; tune in production.
    private const int DefaultIterations = 200_000;

    private static readonly HashAlgorithmName DefaultAlgorithm = HashAlgorithmName.SHA256;

    public string Hash(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be empty.", nameof(password));

        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);

        byte[] key = Rfc2898DeriveBytes.Pbkdf2(
            password: password,
            salt: salt,
            iterations: DefaultIterations,
            hashAlgorithm: DefaultAlgorithm,
            outputLength: KeySize);

        return string.Join('.',
            Version,
            DefaultAlgorithm.Name,               // "SHA256"
            DefaultIterations.ToString(),
            Convert.ToBase64String(salt),
            Convert.ToBase64String(key));
    }

    public bool Verify(string password, string hash)
    {
        if (string.IsNullOrWhiteSpace(password)) return false;
        if (string.IsNullOrWhiteSpace(hash)) return false;

        var parts = hash.Split('.', StringSplitOptions.None);
        if (parts.Length != 5) return false;

        // v1.{algo}.{iterations}.{salt}.{key}
        if (!string.Equals(parts[0], Version, StringComparison.Ordinal)) return false;

        string algoName = parts[1];
        if (!int.TryParse(parts[2], out int iterations) || iterations <= 0) return false;

        byte[] salt, expectedKey;
        try
        {
            salt = Convert.FromBase64String(parts[3]);
            expectedKey = Convert.FromBase64String(parts[4]);
        }
        catch
        {
            return false;
        }

        // Basic sanity checks (helps avoid weird inputs / DoS-ish cases)
        if (salt.Length < 8 || salt.Length > 64) return false;
        if (expectedKey.Length < 16 || expectedKey.Length > 64) return false;
        if (iterations < 50_000 || iterations > 5_000_000) return false;

        HashAlgorithmName algorithm = algoName switch
        {
            "SHA256" => HashAlgorithmName.SHA256,
            "SHA512" => HashAlgorithmName.SHA512,
            _ => default
        };
        if (algorithm == default) return false;

        byte[] actualKey = Rfc2898DeriveBytes.Pbkdf2(
            password: password,
            salt: salt,
            iterations: iterations,
            hashAlgorithm: algorithm,
            outputLength: expectedKey.Length);

        return CryptographicOperations.FixedTimeEquals(expectedKey, actualKey);
    }
}
