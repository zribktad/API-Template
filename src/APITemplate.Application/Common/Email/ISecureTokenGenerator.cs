namespace APITemplate.Application.Common.Email;

public interface ISecureTokenGenerator
{
    string GenerateToken();
    string HashToken(string token);
}
