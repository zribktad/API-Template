using APITemplate.Application.Features.Auth.DTOs;

namespace APITemplate.Application.Features.Auth.Interfaces;

public interface IAuthenticationProxy
{
    Task<TokenResponse?> AuthenticateAsync(string username, string password, CancellationToken ct = default);
}
