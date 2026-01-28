using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Pos.Infrastructure.Security
{
    public static class TokenHelper
    {

        public static string GenerateRefreshToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(64);
            return Convert.ToBase64String(bytes);
        }

        public static string Sha256(string input)
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = SHA256.HashData(bytes);
            return Convert.ToHexString(hash);
        }

    }
}
