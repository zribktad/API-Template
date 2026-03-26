using Contracts.IntegrationEvents.Identity;
using Microsoft.Extensions.Options;
using Moq;
using Notifications.Application.Features.Emails.EventHandlers;
using Notifications.Application.Options;
using Notifications.Domain.Constants;
using Notifications.Domain.Interfaces;
using Notifications.Domain.ValueObjects;
using Shouldly;
using Xunit;

namespace Notifications.Tests.Features.Emails.EventHandlers;

public sealed class UserRegisteredNotificationHandlerTests
{
    private readonly Mock<IEmailTemplateRenderer> _rendererMock = new();
    private readonly Mock<IEmailQueue> _queueMock = new();
    private readonly IOptions<EmailOptions> _options = Options.Create(
        new EmailOptions { BaseUrl = "https://app.example.com" }
    );

    [Fact]
    public async Task HandleAsync_RendersTemplateWithCorrectModel()
    {
        UserRegisteredIntegrationEvent @event = new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "user@example.com",
            "testuser",
            DateTime.UtcNow
        );
        _rendererMock
            .Setup(r =>
                r.RenderAsync(
                    EmailTemplateNames.UserRegistration,
                    It.IsAny<object>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync("<html>welcome</html>");

        await UserRegisteredNotificationHandler.HandleAsync(
            @event,
            _rendererMock.Object,
            _queueMock.Object,
            _options,
            CancellationToken.None
        );

        _rendererMock.Verify(
            r =>
                r.RenderAsync(
                    EmailTemplateNames.UserRegistration,
                    It.IsAny<object>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task HandleAsync_EnqueuesEmailWithCorrectRecipientAndSubject()
    {
        UserRegisteredIntegrationEvent @event = new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "user@example.com",
            "testuser",
            DateTime.UtcNow
        );
        _rendererMock
            .Setup(r =>
                r.RenderAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync("<html>welcome</html>");
        EmailMessage? capturedMessage = null;
        _queueMock
            .Setup(q => q.EnqueueAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Callback<EmailMessage, CancellationToken>((msg, _) => capturedMessage = msg)
            .Returns(ValueTask.CompletedTask);

        await UserRegisteredNotificationHandler.HandleAsync(
            @event,
            _rendererMock.Object,
            _queueMock.Object,
            _options,
            CancellationToken.None
        );

        capturedMessage.ShouldNotBeNull();
        capturedMessage.To.ShouldBe("user@example.com");
        capturedMessage.Subject.ShouldBe(EmailSubjects.UserRegistration);
        capturedMessage.HtmlBody.ShouldBe("<html>welcome</html>");
        capturedMessage.TemplateName.ShouldBe(EmailTemplateNames.UserRegistration);
    }
}
