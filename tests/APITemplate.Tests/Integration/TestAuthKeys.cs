using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace APITemplate.Tests.Integration;

internal static class TestAuthKeys
{
    private static readonly RSA Rsa = RSA.Create(2048);
    public static readonly RsaSecurityKey RsaSecurityKey = new(Rsa);

    public static readonly SigningCredentials SigningCredentials =
        new(RsaSecurityKey, SecurityAlgorithms.RsaSha256);

    public const string Issuer = "TestIssuer";
    public const string Audience = "api-template";
}
