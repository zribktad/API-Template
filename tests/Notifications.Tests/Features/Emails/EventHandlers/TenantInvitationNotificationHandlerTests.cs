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

public sealed class TenantInvitationNotificationHandlerTests
{
    private readonly Mock<IEmailTemplateRenderer> _rendererMock = new();
    private readonly Mock<IEmailQueue> _queueMock = new();
    private readonly IOptions<EmailOptions> _options = Options.Create(
        new EmailOptions { BaseUrl = "https://app.example.com", InvitationTokenExpiryHours = 48 }
    );

    [Fact]
    public async Task HandleAsync_RendersTemplateWithInvitationUrl()
    {
        TenantInvitationCreatedIntegrationEvent @event = new(
            Guid.NewGuid(),
            "invite@example.com",
            "Acme Corp",
            "abc123token",
            DateTime.UtcNow
        );
        _rendererMock
            .Setup(r =>
                r.RenderAsync(
                    EmailTemplateNames.TenantInvitation,
                    It.IsAny<object>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync("<html>invitation</html>");

        await TenantInvitationNotificationHandler.HandleAsync(
            @event,
            _rendererMock.Object,
            _queueMock.Object,
            _options,
            CancellationToken.None
        );

        _rendererMock.Verify(
            r =>
                r.RenderAsync(
                    EmailTemplateNames.TenantInvitation,
                    It.IsAny<object>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task HandleAsync_EnqueuesEmailWithCorrectSubjectContainingTenantName()
    {
        TenantInvitationCreatedIntegrationEvent @event = new(
            Guid.NewGuid(),
            "invite@example.com",
            "Acme Corp",
            "abc123token",
            DateTime.UtcNow
        );
        _rendererMock
            .Setup(r =>
                r.RenderAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync("<html>invitation</html>");
        EmailMessage? capturedMessage = null;
        _queueMock
            .Setup(q => q.EnqueueAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Callback<EmailMessage, CancellationToken>((msg, _) => capturedMessage = msg)
            .Returns(ValueTask.CompletedTask);

        await TenantInvitationNotificationHandler.HandleAsync(
            @event,
            _rendererMock.Object,
            _queueMock.Object,
            _options,
            CancellationToken.None
        );

        capturedMessage.ShouldNotBeNull();
        capturedMessage.To.ShouldBe("invite@example.com");
        capturedMessage.Subject.ShouldContain("Acme Corp");
        capturedMessage.TemplateName.ShouldBe(EmailTemplateNames.TenantInvitation);
    }
}
