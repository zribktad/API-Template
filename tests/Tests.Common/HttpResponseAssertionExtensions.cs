using System.Net;
using Shouldly;

namespace TestCommon;

public static class HttpResponseAssertionExtensions
{
    /// <summary>
    /// Asserts the response status, reads the body once, and returns it for further assertions.
    /// </summary>
    public static async Task<string> ShouldHaveStatusAsync(
        this HttpResponseMessage response,
        HttpStatusCode expected,
        CancellationToken cancellationToken = default
    )
    {
        string body = await response.Content.ReadAsStringAsync(cancellationToken);
        response.StatusCode.ShouldBe(expected, body);
        return body;
    }
}
