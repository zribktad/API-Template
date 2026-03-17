using APITemplate.Domain.Entities;

namespace APITemplate.Infrastructure.Email;

internal static class FailedEmailErrorNormalizer
{
    public static string? Normalize(string? error)
    {
        if (string.IsNullOrEmpty(error) || error.Length <= FailedEmail.LastErrorMaxLength)
        {
            return error;
        }

        return error[..FailedEmail.LastErrorMaxLength];
    }
}
