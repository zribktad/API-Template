using System.Security.Cryptography;
using System.Text;
using Shouldly;
using Webhooks.Infrastructure.Hmac;
using Xunit;

namespace Webhooks.Tests.Infrastructure.Hmac;

public sealed class HmacHelperTests
{
    [Fact]
    public void ComputeHash_ReturnsConsistentResult()
    {
        byte[] key = Encoding.UTF8.GetBytes("test-secret-key");
        string timestamp = "1234567890";
        string payload = "{\"event\":\"test\"}";

        byte[] hash1 = HmacHelper.ComputeHash(key, timestamp, payload);
        byte[] hash2 = HmacHelper.ComputeHash(key, timestamp, payload);

        hash1.ShouldBe(hash2);
    }

    [Fact]
    public void ComputeHash_DifferentKeys_ProduceDifferentHashes()
    {
        byte[] key1 = Encoding.UTF8.GetBytes("key-one");
        byte[] key2 = Encoding.UTF8.GetBytes("key-two");
        string timestamp = "1234567890";
        string payload = "{\"event\":\"test\"}";

        byte[] hash1 = HmacHelper.ComputeHash(key1, timestamp, payload);
        byte[] hash2 = HmacHelper.ComputeHash(key2, timestamp, payload);

        hash1.ShouldNotBe(hash2);
    }

    [Fact]
    public void ComputeHash_DifferentTimestamps_ProduceDifferentHashes()
    {
        byte[] key = Encoding.UTF8.GetBytes("test-secret-key");
        string payload = "{\"event\":\"test\"}";

        byte[] hash1 = HmacHelper.ComputeHash(key, "1111111111", payload);
        byte[] hash2 = HmacHelper.ComputeHash(key, "2222222222", payload);

        hash1.ShouldNotBe(hash2);
    }

    [Fact]
    public void ComputeHash_MatchesExpectedHmacSha256()
    {
        byte[] key = Encoding.UTF8.GetBytes("secret");
        string timestamp = "ts";
        string payload = "body";

        byte[] result = HmacHelper.ComputeHash(key, timestamp, payload);

        // Verify against direct HMACSHA256 computation
        string signedContent = $"{timestamp}.{payload}";
        byte[] expectedHash = HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(signedContent));

        result.ShouldBe(expectedHash);
    }

    [Fact]
    public void ComputeHash_ProducesNonEmptyHash()
    {
        byte[] key = Encoding.UTF8.GetBytes("key");
        byte[] result = HmacHelper.ComputeHash(key, "ts", "payload");

        result.ShouldNotBeEmpty();
        result.Length.ShouldBe(32); // SHA-256 produces 32 bytes
    }
}
