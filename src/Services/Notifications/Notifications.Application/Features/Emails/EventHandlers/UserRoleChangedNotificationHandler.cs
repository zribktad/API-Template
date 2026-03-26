using Contracts.IntegrationEvents.Identity;
using Notifications.Domain.Constants;
using Notifications.Domain.Interfaces;
using Notifications.Domain.ValueObjects;

namespace Notifications.Application.Features.Emails.EventHandlers;

/// <summary>
/// Wolverine message handler that consumes <see cref="UserRoleChangedIntegrationEvent"/> from RabbitMQ
/// and enqueues a role-change notification email for delivery.
/// </summary>
public static class UserRoleChangedNotificationHandler
{
    public static async Task HandleAsync(
        UserRoleChangedIntegrationEvent @event,
        IEmailTemplateRenderer renderer,
        IEmailQueue queue,
        CancellationToken ct
    )
    {
        string html = await renderer.RenderAsync(
            EmailTemplateNames.UserRoleChanged,
            new
            {
                @event.Username,
                @event.OldRole,
                @event.NewRole,
            },
            ct
        );

        await queue.EnqueueAsync(
            new EmailMessage(
                @event.Email,
                EmailSubjects.UserRoleChanged,
                html,
                EmailTemplateNames.UserRoleChanged
            ),
            ct
        );
    }
}
