using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using SharedKernel.Application.Security;

namespace SharedKernel.Api.Authorization;

public sealed class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallback;
    private readonly IReadOnlyList<string> _authenticationSchemes;
    private readonly ConcurrentDictionary<string, AuthorizationPolicy> _cache = new();

    public PermissionPolicyProvider(
        IOptions<AuthorizationOptions> authorizationOptions,
        IOptions<PermissionAuthorizationOptions> permissionOptions
    )
    {
        _fallback = new DefaultAuthorizationPolicyProvider(authorizationOptions);
        _authenticationSchemes = permissionOptions.Value.AuthenticationSchemes;
    }

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!Permission.All.Contains(policyName))
            return _fallback.GetPolicyAsync(policyName);

        AuthorizationPolicy policy = _cache.GetOrAdd(
            policyName,
            name =>
            {
                AuthorizationPolicyBuilder builder =
                    _authenticationSchemes.Count > 0
                        ? new AuthorizationPolicyBuilder(_authenticationSchemes.ToArray())
                        : new AuthorizationPolicyBuilder();

                return builder
                    .RequireAuthenticatedUser()
                    .AddRequirements(new PermissionRequirement(name))
                    .Build();
            }
        );

        return Task.FromResult<AuthorizationPolicy?>(policy);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() =>
        _fallback.GetFallbackPolicyAsync();
}
