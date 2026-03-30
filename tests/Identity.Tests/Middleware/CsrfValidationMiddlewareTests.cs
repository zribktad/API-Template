using System.Security.Claims;
using Identity.Api.Middleware;
using Identity.Application.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shouldly;
using Xunit;

namespace Identity.Tests.Middleware;

public sealed class CsrfValidationMiddlewareTests
{
    // Builds a DefaultHttpContext wired with a mock IAuthenticationService.
    // Returns both the context and the auth mock so individual tests can verify
    // whether AuthenticateAsync was called (e.g. to assert the short-circuit path).
    // cookieAuthInUser and cookieAuthResult are mutually exclusive: when cookieAuthInUser
    // is true the middleware short-circuits before calling AuthenticateAsync.
    private static (DefaultHttpContext context, Mock<IAuthenticationService> authMock) BuildContext(
        string method = "POST",
        bool withBearerToken = false,
        bool cookieAuthInUser = false,
        AuthenticateResult? cookieAuthResult = null,
        bool withCsrfHeader = false,
        string? csrfHeaderValue = null
    )
    {
        DefaultHttpContext context = new();
        context.Request.Method = method;
        context.Response.Body = new MemoryStream();

        if (withBearerToken)
            context.Request.Headers.Authorization = "Bearer test-token";

        if (cookieAuthInUser)
        {
            ClaimsIdentity identity = new(authenticationType: AuthConstants.BffSchemes.Cookie);
            context.User = new ClaimsPrincipal(identity);
        }

        if (withCsrfHeader)
            context.Request.Headers[AuthConstants.Csrf.HeaderName] =
                csrfHeaderValue ?? AuthConstants.Csrf.HeaderValue;

        Mock<IAuthenticationService> authServiceMock = new();
        authServiceMock
            .Setup(s => s.AuthenticateAsync(context, AuthConstants.BffSchemes.Cookie))
            .ReturnsAsync(cookieAuthResult ?? AuthenticateResult.NoResult());

        ServiceCollection services = new();
        services.AddSingleton(authServiceMock.Object);
        context.RequestServices = services.BuildServiceProvider();

        return (context, authServiceMock);
    }

    private static RequestDelegate NextThatSets200() =>
        ctx =>
        {
            ctx.Response.StatusCode = 200;
            return Task.CompletedTask;
        };

    private static (
        CsrfValidationMiddleware middleware,
        Mock<IProblemDetailsService> mock
    ) BuildMiddleware()
    {
        Mock<IProblemDetailsService> problemDetailsMock = new();
        problemDetailsMock
            .Setup(p => p.TryWriteAsync(It.IsAny<ProblemDetailsContext>()))
            .ReturnsAsync(true);

        CsrfValidationMiddleware middleware = new(NextThatSets200(), problemDetailsMock.Object);
        return (middleware, problemDetailsMock);
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("HEAD")]
    [InlineData("OPTIONS")]
    public async Task SafeMethods_AlwaysPassThrough(string method)
    {
        (DefaultHttpContext context, _) = BuildContext(method: method);
        (CsrfValidationMiddleware middleware, _) = BuildMiddleware();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.ShouldBe(200);
    }

    [Fact]
    public async Task Post_WithBearerToken_PassesThrough_WithoutCsrfHeader()
    {
        (DefaultHttpContext context, _) = BuildContext(withBearerToken: true);
        (CsrfValidationMiddleware middleware, _) = BuildMiddleware();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.ShouldBe(200);
    }

    [Fact]
    public async Task Post_WithNoAuth_PassesThrough()
    {
        (DefaultHttpContext context, _) = BuildContext(
            cookieAuthResult: AuthenticateResult.NoResult()
        );
        (CsrfValidationMiddleware middleware, _) = BuildMiddleware();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.ShouldBe(200);
    }

    [Fact]
    public async Task Post_WithFailedCookieAuth_PassesThrough()
    {
        (DefaultHttpContext context, _) = BuildContext(
            cookieAuthResult: AuthenticateResult.Fail("Cookie expired")
        );
        (CsrfValidationMiddleware middleware, _) = BuildMiddleware();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.ShouldBe(200);
    }

    [Fact]
    public async Task Post_CookieInUser_WithCsrfHeader_PassesThrough_WithoutCallingAuthenticateAsync()
    {
        (DefaultHttpContext context, Mock<IAuthenticationService> authMock) = BuildContext(
            cookieAuthInUser: true,
            withCsrfHeader: true
        );
        (CsrfValidationMiddleware middleware, _) = BuildMiddleware();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.ShouldBe(200);
        authMock.Verify(
            s => s.AuthenticateAsync(context, AuthConstants.BffSchemes.Cookie),
            Times.Never
        );
    }

    [Fact]
    public async Task Post_CookieViaAuthenticateAsync_WithCsrfHeader_PassesThrough()
    {
        ClaimsIdentity identity = new(authenticationType: AuthConstants.BffSchemes.Cookie);
        AuthenticateResult succeeded = AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(identity), AuthConstants.BffSchemes.Cookie)
        );

        (DefaultHttpContext context, _) = BuildContext(
            cookieAuthResult: succeeded,
            withCsrfHeader: true
        );
        (CsrfValidationMiddleware middleware, _) = BuildMiddleware();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.ShouldBe(200);
    }

    [Fact]
    public async Task Post_CookieInUser_WithoutCsrfHeader_Returns403_WithoutCallingAuthenticateAsync()
    {
        (DefaultHttpContext context, Mock<IAuthenticationService> authMock) = BuildContext(
            cookieAuthInUser: true
        );
        (CsrfValidationMiddleware middleware, Mock<IProblemDetailsService> problemMock) =
            BuildMiddleware();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.ShouldBe(403);
        problemMock.Verify(p => p.TryWriteAsync(It.IsAny<ProblemDetailsContext>()), Times.Once);
        authMock.Verify(
            s => s.AuthenticateAsync(context, AuthConstants.BffSchemes.Cookie),
            Times.Never
        );
    }

    [Fact]
    public async Task Post_CookieViaAuthenticateAsync_WithoutCsrfHeader_Returns403()
    {
        ClaimsIdentity identity = new(authenticationType: AuthConstants.BffSchemes.Cookie);
        AuthenticateResult succeeded = AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(identity), AuthConstants.BffSchemes.Cookie)
        );

        (DefaultHttpContext context, _) = BuildContext(cookieAuthResult: succeeded);
        (CsrfValidationMiddleware middleware, Mock<IProblemDetailsService> mock) =
            BuildMiddleware();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.ShouldBe(403);
        mock.Verify(p => p.TryWriteAsync(It.IsAny<ProblemDetailsContext>()), Times.Once);
    }

    [Fact]
    public async Task Post_CookieInUser_WrongCsrfHeaderValue_Returns403()
    {
        (DefaultHttpContext context, _) = BuildContext(
            cookieAuthInUser: true,
            withCsrfHeader: true,
            csrfHeaderValue: "wrong-value"
        );
        (CsrfValidationMiddleware middleware, Mock<IProblemDetailsService> mock) =
            BuildMiddleware();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.ShouldBe(403);
        mock.Verify(p => p.TryWriteAsync(It.IsAny<ProblemDetailsContext>()), Times.Once);
    }
}
