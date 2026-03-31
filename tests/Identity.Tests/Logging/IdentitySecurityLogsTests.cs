using System.Reflection;
using Identity.Infrastructure.Security.Keycloak;
using Identity.Infrastructure.Security.Tenant;
using Microsoft.Extensions.Logging;
using SharedKernel.Infrastructure.Logging;
using Shouldly;
using Xunit;

namespace Identity.Tests.Logging;

public sealed class IdentitySecurityLogsTests
{
    [Fact]
    public void TenantClaimValidator_UserAuthenticated_UsesExpectedClassifications()
    {
        Type logsType = GetRequiredType(
            "Identity.Infrastructure.Security.Tenant.TenantClaimValidatorLogs"
        );
        MethodInfo method = logsType.GetMethod(
            "UserAuthenticated",
            BindingFlags.Public | BindingFlags.Static
        )!;

        LoggerMessageAttribute loggerMessage = method.GetCustomAttribute<LoggerMessageAttribute>()!;
        loggerMessage.Level.ShouldBe(LogLevel.Information);

        method.GetParameters()[2].GetCustomAttribute<PersonalDataAttribute>().ShouldNotBeNull();
        method.GetParameters()[3].GetCustomAttribute<SensitiveDataAttribute>().ShouldNotBeNull();
    }

    [Fact]
    public void KeycloakAdminService_UserCreated_UsesExpectedClassifications()
    {
        Type logsType = GetRequiredType(
            "Identity.Infrastructure.Security.Keycloak.KeycloakAdminServiceLogs"
        );
        MethodInfo method = logsType.GetMethod(
            "UserCreated",
            BindingFlags.Public | BindingFlags.Static
        )!;

        LoggerMessageAttribute loggerMessage = method.GetCustomAttribute<LoggerMessageAttribute>()!;
        loggerMessage.Level.ShouldBe(LogLevel.Information);

        method.GetParameters()[1].GetCustomAttribute<PersonalDataAttribute>().ShouldNotBeNull();
        method.GetParameters()[2].GetCustomAttribute<SensitiveDataAttribute>().ShouldNotBeNull();
    }

    [Fact]
    public void KeycloakAdminTokenProvider_TokenAcquireFailed_RedactsResponseBody()
    {
        Type logsType = GetRequiredType(
            "Identity.Infrastructure.Security.Keycloak.KeycloakAdminTokenProviderLogs"
        );
        MethodInfo method = logsType.GetMethod(
            "TokenAcquireFailed",
            BindingFlags.Public | BindingFlags.Static
        )!;

        LoggerMessageAttribute loggerMessage = method.GetCustomAttribute<LoggerMessageAttribute>()!;
        loggerMessage.Level.ShouldBe(LogLevel.Error);

        method.GetParameters()[2].GetCustomAttribute<SensitiveDataAttribute>().ShouldNotBeNull();
    }

    private static Type GetRequiredType(string fullName) =>
        typeof(TenantClaimValidator).Assembly.GetType(fullName)
        ?? throw new InvalidOperationException($"Could not load type '{fullName}'.");
}
