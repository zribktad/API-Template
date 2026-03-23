using APITemplate.Application.Common.Context;
using APITemplate.Application.Common.Email;
using APITemplate.Application.Common.Events;
using APITemplate.Application.Common.Extensions;
using APITemplate.Domain.Enums;
using APITemplate.Domain.Exceptions;
using APITemplate.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Wolverine;

namespace APITemplate.Application.Features.TenantInvitation;

public sealed record ResendTenantInvitationCommand(Guid InvitationId);

public sealed class ResendTenantInvitationCommandHandler
{
    public static async Task HandleAsync(
        ResendTenantInvitationCommand command,
        ITenantInvitationRepository invitationRepository,
        ITenantRepository tenantRepository,
        IUnitOfWork unitOfWork,
        ISecureTokenGenerator tokenGenerator,
        IMessageBus bus,
        ITenantProvider tenantProvider,
        TimeProvider timeProvider,
        ILogger<ResendTenantInvitationCommandHandler> logger,
        CancellationToken ct
    )
    {
        var invitation = await invitationRepository.GetByIdOrThrowAsync(
            command.InvitationId,
            ErrorCatalog.Invitations.NotFound,
            ct
        );

        if (invitation.Status != InvitationStatus.Pending)
            throw new ConflictException(
                ErrorCatalog.Invitations.NotPendingMessage,
                ErrorCatalog.Invitations.NotPending
            );

        var now = timeProvider.GetUtcNow().UtcDateTime;
        if (invitation.ExpiresAtUtc < now)
            throw new ConflictException(
                ErrorCatalog.Invitations.ExpiredCreateNewMessage,
                ErrorCatalog.Invitations.Expired
            );

        var tenant = await tenantRepository.GetByIdOrThrowAsync(
            tenantProvider.TenantId,
            ErrorCatalog.Tenants.NotFound,
            ct
        );

        var rawToken = tokenGenerator.GenerateToken();
        invitation.TokenHash = tokenGenerator.HashToken(rawToken);

        await invitationRepository.UpdateAsync(invitation, ct);
        await unitOfWork.CommitAsync(ct);

        await bus.PublishSafeAsync(
            new TenantInvitationCreatedNotification(
                invitation.Id,
                invitation.Email,
                tenant.Name,
                rawToken
            ),
            logger
        );

        await bus.PublishAsync(new CacheInvalidationNotification(CacheTags.TenantInvitations));
    }
}
