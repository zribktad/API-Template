using System.Security.Cryptography;
using Identity.Application.Security;

namespace Identity.Infrastructure.Security;

/// <summary>
/// Generates cryptographically random tokens and produces their SHA-256 hex digest
/// for safe storage in the database.
/// </summary>
public sealed class SecureTokenGenerator : ISecureTokenGenerator
{
    public string GenerateToken()
    {
        byte[] bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes);
    }

    public string HashToken(string token)
    {
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(token);
        byte[] hash = SHA256.HashData(bytes);
        return Convert.ToHexStringLower(hash);
    }
}
