using Contracts.IntegrationEvents.Identity;
using Moq;
using Notifications.Application.Features.Emails.EventHandlers;
using Notifications.Domain.Constants;
using Notifications.Domain.Interfaces;
using Notifications.Domain.ValueObjects;
using Shouldly;
using Xunit;

namespace Notifications.Tests.Features.Emails.EventHandlers;

public sealed class UserRoleChangedNotificationHandlerTests
{
    private readonly Mock<IEmailTemplateRenderer> _rendererMock = new();
    private readonly Mock<IEmailQueue> _queueMock = new();

    [Fact]
    public async Task HandleAsync_RendersTemplateWithRoleInformation()
    {
        UserRoleChangedIntegrationEvent @event = new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "user@example.com",
            "testuser",
            "Viewer",
            "Admin",
            DateTime.UtcNow
        );
        _rendererMock
            .Setup(r =>
                r.RenderAsync(
                    EmailTemplateNames.UserRoleChanged,
                    It.IsAny<object>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync("<html>role changed</html>");

        await UserRoleChangedNotificationHandler.HandleAsync(
            @event,
            _rendererMock.Object,
            _queueMock.Object,
            CancellationToken.None
        );

        _rendererMock.Verify(
            r =>
                r.RenderAsync(
                    EmailTemplateNames.UserRoleChanged,
                    It.IsAny<object>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task HandleAsync_EnqueuesEmailWithCorrectRecipientAndSubject()
    {
        UserRoleChangedIntegrationEvent @event = new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "user@example.com",
            "testuser",
            "Viewer",
            "Admin",
            DateTime.UtcNow
        );
        _rendererMock
            .Setup(r =>
                r.RenderAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync("<html>role changed</html>");
        EmailMessage? capturedMessage = null;
        _queueMock
            .Setup(q => q.EnqueueAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Callback<EmailMessage, CancellationToken>((msg, _) => capturedMessage = msg)
            .Returns(ValueTask.CompletedTask);

        await UserRoleChangedNotificationHandler.HandleAsync(
            @event,
            _rendererMock.Object,
            _queueMock.Object,
            CancellationToken.None
        );

        capturedMessage.ShouldNotBeNull();
        capturedMessage.To.ShouldBe("user@example.com");
        capturedMessage.Subject.ShouldBe(EmailSubjects.UserRoleChanged);
        capturedMessage.HtmlBody.ShouldBe("<html>role changed</html>");
        capturedMessage.TemplateName.ShouldBe(EmailTemplateNames.UserRoleChanged);
    }
}
