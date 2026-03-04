namespace APITemplate.Application.Features.Auth.Interfaces;
public interface IUserService
{
    Task<AuthenticatedUser?> AuthenticateAsync(string username, string password, CancellationToken ct = default);
}
