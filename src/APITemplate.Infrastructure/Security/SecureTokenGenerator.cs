using System.Security.Cryptography;
using APITemplate.Application.Common.Email;

namespace APITemplate.Infrastructure.Security;

public sealed class SecureTokenGenerator : ISecureTokenGenerator
{
    public string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes);
    }

    public string HashToken(string token)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(token);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexStringLower(hash);
    }
}
