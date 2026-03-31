using Contracts.IntegrationEvents.Identity;
using Microsoft.Extensions.Options;
using Notifications.Application.Options;
using Notifications.Domain.Constants;
using Notifications.Domain.Interfaces;
using Notifications.Domain.ValueObjects;

namespace Notifications.Application.Features.Emails.EventHandlers;

/// <summary>
/// Wolverine message handler that consumes <see cref="TenantInvitationCreatedIntegrationEvent"/> from RabbitMQ
/// and enqueues a tenant invitation email for delivery.
/// </summary>
public static class TenantInvitationNotificationHandler
{
    public static async Task HandleAsync(
        TenantInvitationCreatedIntegrationEvent @event,
        IEmailTemplateRenderer renderer,
        IEmailQueue queue,
        IOptions<EmailOptions> options,
        CancellationToken ct
    )
    {
        string invitationUrl = $"{options.Value.BaseUrl}/invitations/accept?token={@event.Token}";

        string html = await renderer.RenderAsync(
            EmailTemplateNames.TenantInvitation,
            new
            {
                @event.TenantName,
                InvitationUrl = invitationUrl,
                ExpiryHours = options.Value.InvitationTokenExpiryHours,
            },
            ct
        );

        await queue.EnqueueAsync(
            new EmailMessage(
                @event.Email,
                string.Format(EmailSubjects.TenantInvitation, @event.TenantName),
                html,
                EmailTemplateNames.TenantInvitation,
                Retryable: true
            ),
            ct
        );
    }
}
