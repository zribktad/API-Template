using APITemplate.Application.Features.Auth.Interfaces;
using APITemplate.Application.Features.Auth.Mediator;
using Moq;
using Shouldly;
using Xunit;

namespace APITemplate.Tests.Unit.Mediator;

public class AuthHandlersTests
{
    [Fact]
    public async Task LoginCommandHandler_WithInvalidCredentials_ReturnsNull()
    {
        var userServiceMock = new Mock<IUserService>();
        var tokenServiceMock = new Mock<ITokenService>();

        userServiceMock
            .Setup(s => s.ValidateAsync("user", "bad", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var sut = new LoginCommandHandler(userServiceMock.Object, tokenServiceMock.Object);

        var result = await sut.Handle(new LoginCommand(new LoginRequest("user", "bad")), CancellationToken.None);

        result.ShouldBeNull();
        tokenServiceMock.Verify(t => t.GenerateToken(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task LoginCommandHandler_WithValidCredentials_ReturnsToken()
    {
        var userServiceMock = new Mock<IUserService>();
        var tokenServiceMock = new Mock<ITokenService>();

        var expected = new TokenResponse("token", DateTime.UtcNow.AddHours(1));

        userServiceMock
            .Setup(s => s.ValidateAsync("user", "pass", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        tokenServiceMock
            .Setup(t => t.GenerateToken("user"))
            .Returns(expected);

        var sut = new LoginCommandHandler(userServiceMock.Object, tokenServiceMock.Object);

        var result = await sut.Handle(new LoginCommand(new LoginRequest("user", "pass")), CancellationToken.None);

        result.ShouldBe(expected);
    }
}
