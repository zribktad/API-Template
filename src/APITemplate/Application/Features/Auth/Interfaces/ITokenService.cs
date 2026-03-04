
namespace APITemplate.Application.Features.Auth.Interfaces;
public interface ITokenService
{
    TokenResponse GenerateToken(AuthenticatedUser user);
}
