using Contracts.IntegrationEvents.Identity;
using Microsoft.Extensions.Options;
using Notifications.Application.Options;
using Notifications.Domain.Constants;
using Notifications.Domain.Interfaces;
using Notifications.Domain.ValueObjects;

namespace Notifications.Application.Features.Emails.EventHandlers;

/// <summary>
/// Wolverine message handler that consumes <see cref="UserRegisteredIntegrationEvent"/> from RabbitMQ
/// and enqueues a welcome email for delivery.
/// </summary>
public static class UserRegisteredNotificationHandler
{
    public static async Task HandleAsync(
        UserRegisteredIntegrationEvent @event,
        IEmailTemplateRenderer renderer,
        IEmailQueue queue,
        IOptions<EmailOptions> options,
        CancellationToken ct
    )
    {
        string html = await renderer.RenderAsync(
            EmailTemplateNames.UserRegistration,
            new
            {
                @event.Username,
                @event.Email,
                LoginUrl = $"{options.Value.BaseUrl}/login",
            },
            ct
        );

        await queue.EnqueueAsync(
            new EmailMessage(
                @event.Email,
                EmailSubjects.UserRegistration,
                html,
                EmailTemplateNames.UserRegistration
            ),
            ct
        );
    }
}
