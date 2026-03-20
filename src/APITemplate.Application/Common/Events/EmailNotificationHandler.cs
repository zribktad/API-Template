using APITemplate.Application.Common.Email;
using APITemplate.Application.Common.Options;
using Microsoft.Extensions.Options;

namespace APITemplate.Application.Common.Events;

public sealed class UserRegisteredEmailHandler : IDomainEventHandler<UserRegisteredNotification>
{
    private readonly IEmailTemplateRenderer _templateRenderer;
    private readonly IEmailQueue _emailQueue;
    private readonly EmailOptions _options;

    public UserRegisteredEmailHandler(
        IEmailTemplateRenderer templateRenderer,
        IEmailQueue emailQueue,
        IOptions<EmailOptions> options
    )
    {
        _templateRenderer = templateRenderer;
        _emailQueue = emailQueue;
        _options = options.Value;
    }

    public async Task HandleAsync(UserRegisteredNotification @event, CancellationToken ct)
    {
        var html = await _templateRenderer.RenderAsync(
            EmailTemplateNames.UserRegistration,
            new
            {
                @event.Username,
                @event.Email,
                LoginUrl = $"{_options.BaseUrl}/login",
            },
            ct
        );

        await _emailQueue.EnqueueAsync(
            new EmailMessage(
                @event.Email,
                "Welcome to the platform!",
                html,
                EmailTemplateNames.UserRegistration
            ),
            ct
        );
    }
}

public sealed class TenantInvitationEmailHandler
    : IDomainEventHandler<TenantInvitationCreatedNotification>
{
    private readonly IEmailTemplateRenderer _templateRenderer;
    private readonly IEmailQueue _emailQueue;
    private readonly EmailOptions _options;

    public TenantInvitationEmailHandler(
        IEmailTemplateRenderer templateRenderer,
        IEmailQueue emailQueue,
        IOptions<EmailOptions> options
    )
    {
        _templateRenderer = templateRenderer;
        _emailQueue = emailQueue;
        _options = options.Value;
    }

    public async Task HandleAsync(TenantInvitationCreatedNotification @event, CancellationToken ct)
    {
        var html = await _templateRenderer.RenderAsync(
            EmailTemplateNames.TenantInvitation,
            new
            {
                @event.Email,
                @event.TenantName,
                InvitationUrl = $"{_options.BaseUrl}/invitations/accept?token={@event.Token}",
                ExpiryHours = _options.InvitationTokenExpiryHours,
            },
            ct
        );

        await _emailQueue.EnqueueAsync(
            new EmailMessage(
                @event.Email,
                $"You've been invited to {@event.TenantName}",
                html,
                EmailTemplateNames.TenantInvitation,
                Retryable: true
            ),
            ct
        );
    }
}

public sealed class UserRoleChangedEmailHandler : IDomainEventHandler<UserRoleChangedNotification>
{
    private readonly IEmailTemplateRenderer _templateRenderer;
    private readonly IEmailQueue _emailQueue;

    public UserRoleChangedEmailHandler(
        IEmailTemplateRenderer templateRenderer,
        IEmailQueue emailQueue
    )
    {
        _templateRenderer = templateRenderer;
        _emailQueue = emailQueue;
    }

    public async Task HandleAsync(UserRoleChangedNotification @event, CancellationToken ct)
    {
        var html = await _templateRenderer.RenderAsync(
            EmailTemplateNames.UserRoleChanged,
            new
            {
                @event.Username,
                @event.OldRole,
                @event.NewRole,
            },
            ct
        );

        await _emailQueue.EnqueueAsync(
            new EmailMessage(
                @event.Email,
                "Your role has been updated",
                html,
                EmailTemplateNames.UserRoleChanged
            ),
            ct
        );
    }
}
