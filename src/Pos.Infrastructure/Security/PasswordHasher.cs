using System.Security.Cryptography;
using Pos.Application.Abstractions.Security;

namespace Pos.Infrastructure.Security;

public class PasswordHasher : IPasswordHasher
{
    public string Hash(string password)
    {
        using var derive = new Rfc2898DeriveBytes(password, 16, 100_000, HashAlgorithmName.SHA256);
        var salt = derive.Salt;
        var key = derive.GetBytes(32);

        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(key)}";
    }

    public bool Verify(string password, string hash)
    {
        var parts = hash.Split('.');
        if (parts.Length != 2) return false;

        var salt = Convert.FromBase64String(parts[0]);
        var key = Convert.FromBase64String(parts[1]);

        using var derive = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
        var newKey = derive.GetBytes(32);

        return CryptographicOperations.FixedTimeEquals(key, newKey);
    }
}