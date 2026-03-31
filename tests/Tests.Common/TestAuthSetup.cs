using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace TestCommon;

public static class TestAuthSetup
{
    private static readonly RSA RsaKey = RSA.Create(2048);

    public static readonly RsaSecurityKey SecurityKey = new(RsaKey);

    public const string Issuer = "http://localhost:8180/realms/api-template";
    public const string Audience = "api-template";

    public static void ConfigureTestJwtBearer(IServiceCollection services)
    {
        services.PostConfigure<JwtBearerOptions>(
            JwtBearerDefaults.AuthenticationScheme,
            options =>
            {
                options.Authority = null;
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = Issuer,
                    ValidateAudience = true,
                    ValidAudience = Audience,
                    ValidateLifetime = true,
                    IssuerSigningKey = SecurityKey,
                    ValidateIssuerSigningKey = true,
                };
            }
        );
    }
}
