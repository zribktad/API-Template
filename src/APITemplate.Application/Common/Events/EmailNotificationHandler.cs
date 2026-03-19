using APITemplate.Application.Common.Email;
using APITemplate.Application.Common.Options;
using MediatR;
using Microsoft.Extensions.Options;

namespace APITemplate.Application.Common.Events;

/// <summary>
/// MediatR notification handler that reacts to user lifecycle events by rendering email templates
/// and placing the resulting <see cref="EmailMessage"/> instances onto the <see cref="IEmailQueue"/>.
/// Handles <see cref="UserRegisteredNotification"/>, <see cref="TenantInvitationCreatedNotification"/>,
/// and <see cref="UserRoleChangedNotification"/>.
/// </summary>
public sealed class EmailNotificationHandler
    : INotificationHandler<UserRegisteredNotification>,
        INotificationHandler<TenantInvitationCreatedNotification>,
        INotificationHandler<UserRoleChangedNotification>
{
    private readonly IEmailTemplateRenderer _templateRenderer;
    private readonly IEmailQueue _emailQueue;
    private readonly EmailOptions _options;

    public EmailNotificationHandler(
        IEmailTemplateRenderer templateRenderer,
        IEmailQueue emailQueue,
        IOptions<EmailOptions> options
    )
    {
        _templateRenderer = templateRenderer;
        _emailQueue = emailQueue;
        _options = options.Value;
    }

    /// <summary>Renders the welcome email template and enqueues it for delivery to the newly registered user.</summary>
    public async Task Handle(UserRegisteredNotification notification, CancellationToken ct)
    {
        var html = await _templateRenderer.RenderAsync(
            EmailTemplateNames.UserRegistration,
            new
            {
                notification.Username,
                notification.Email,
                LoginUrl = $"{_options.BaseUrl}/login",
            },
            ct
        );

        await _emailQueue.EnqueueAsync(
            new EmailMessage(
                notification.Email,
                "Welcome to the platform!",
                html,
                EmailTemplateNames.UserRegistration
            ),
            ct
        );
    }

    /// <summary>Renders the tenant invitation email template and enqueues it as a retryable message for the invitee.</summary>
    public async Task Handle(TenantInvitationCreatedNotification notification, CancellationToken ct)
    {
        var html = await _templateRenderer.RenderAsync(
            EmailTemplateNames.TenantInvitation,
            new
            {
                notification.Email,
                notification.TenantName,
                InvitationUrl = $"{_options.BaseUrl}/invitations/accept?token={notification.Token}",
                ExpiryHours = _options.InvitationTokenExpiryHours,
            },
            ct
        );

        await _emailQueue.EnqueueAsync(
            new EmailMessage(
                notification.Email,
                $"You've been invited to {notification.TenantName}",
                html,
                EmailTemplateNames.TenantInvitation,
                Retryable: true
            ),
            ct
        );
    }

    /// <summary>Renders the role-change notification email template and enqueues it for the affected user.</summary>
    public async Task Handle(UserRoleChangedNotification notification, CancellationToken ct)
    {
        var html = await _templateRenderer.RenderAsync(
            EmailTemplateNames.UserRoleChanged,
            new
            {
                notification.Username,
                notification.OldRole,
                notification.NewRole,
            },
            ct
        );

        await _emailQueue.EnqueueAsync(
            new EmailMessage(
                notification.Email,
                "Your role has been updated",
                html,
                EmailTemplateNames.UserRoleChanged
            ),
            ct
        );
    }
}
