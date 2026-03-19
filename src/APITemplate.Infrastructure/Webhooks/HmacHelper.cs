using System.Security.Cryptography;
using System.Text;

namespace APITemplate.Infrastructure.Webhooks;

internal static class HmacHelper
{
    public static byte[] ComputeHash(byte[] keyBytes, string timestamp, string payload)
    {
        var signedContent = $"{timestamp}.{payload}";
        var contentBytes = Encoding.UTF8.GetBytes(signedContent);
        return HMACSHA256.HashData(keyBytes, contentBytes);
    }
}
