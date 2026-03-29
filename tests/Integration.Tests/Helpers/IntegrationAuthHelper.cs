using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using SharedKernel.Application.Security;
using TestCommon;

namespace Integration.Tests.Helpers;

internal static class IntegrationAuthHelper
{
    private static readonly SigningCredentials SigningCredentials = new(
        TestAuthSetup.SecurityKey,
        SecurityAlgorithms.RsaSha256
    );

    public static void AuthenticateAsPlatformAdmin(
        HttpClient client,
        Guid tenantId,
        Guid? userId = null
    ) => Authenticate(client, SharedAuthConstants.Roles.PlatformAdmin, tenantId, userId);

    public static void AuthenticateAsTenantAdmin(
        HttpClient client,
        Guid tenantId,
        Guid? userId = null
    ) => Authenticate(client, SharedAuthConstants.Roles.TenantAdmin, tenantId, userId);

    private static void Authenticate(HttpClient client, string role, Guid tenantId, Guid? userId)
    {
        var id = userId ?? Guid.NewGuid();
        var token = new JwtSecurityToken(
            issuer: TestAuthSetup.Issuer,
            audience: TestAuthSetup.Audience,
            claims:
            [
                new Claim(SharedAuthConstants.Claims.Subject, id.ToString()),
                new Claim(SharedAuthConstants.Claims.TenantId, tenantId.ToString()),
                new Claim(ClaimTypes.Role, role),
                new Claim(ClaimTypes.Email, $"{id:N}@example.com"),
            ],
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: SigningCredentials
        );

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            new JwtSecurityTokenHandler().WriteToken(token)
        );
    }
}
