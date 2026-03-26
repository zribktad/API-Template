using APITemplate.Application.Common.Context;
using APITemplate.Application.Common.Email;
using APITemplate.Application.Common.Errors;
using APITemplate.Application.Common.Events;
using APITemplate.Application.Common.Extensions;
using APITemplate.Domain.Enums;
using APITemplate.Domain.Interfaces;
using ErrorOr;
using Wolverine;
using TenantEntity = APITemplate.Domain.Entities.Tenant;
using TenantInvitationEntity = APITemplate.Domain.Entities.TenantInvitation;

namespace APITemplate.Application.Features.TenantInvitation;

/// <summary>Regenerates the token for a pending invitation and re-sends the notification email.</summary>
public sealed record ResendTenantInvitationCommand(Guid InvitationId);

public sealed class ResendTenantInvitationCommandHandler
{
    public static async Task<(
        HandlerContinuation,
        TenantInvitationEntity?,
        TenantEntity?,
        OutgoingMessages
    )> LoadAsync(
        ResendTenantInvitationCommand command,
        ITenantInvitationRepository invitationRepository,
        ITenantRepository tenantRepository,
        ITenantProvider tenantProvider,
        TimeProvider timeProvider,
        CancellationToken ct
    )
    {
        var invitationResult = await invitationRepository.GetByIdOrError(
            command.InvitationId,
            DomainErrors.Invitations.NotFound(command.InvitationId),
            ct
        );

        OutgoingMessages messages = new();

        if (invitationResult.IsError)
        {
            messages.RespondToSender((ErrorOr<Success>)invitationResult.Errors);
            return (HandlerContinuation.Stop, null, null, messages);
        }

        var invitation = invitationResult.Value;

        if (invitation.Status != InvitationStatus.Pending)
        {
            messages.RespondToSender((ErrorOr<Success>)DomainErrors.Invitations.NotPending());
            return (HandlerContinuation.Stop, null, null, messages);
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        if (invitation.ExpiresAtUtc < now)
        {
            messages.RespondToSender((ErrorOr<Success>)DomainErrors.Invitations.ExpiredCreateNew());
            return (HandlerContinuation.Stop, null, null, messages);
        }

        var tenantResult = await tenantRepository.GetByIdOrError(
            tenantProvider.TenantId,
            DomainErrors.Tenants.NotFound(tenantProvider.TenantId),
            ct
        );

        if (tenantResult.IsError)
        {
            messages.RespondToSender((ErrorOr<Success>)tenantResult.Errors);
            return (HandlerContinuation.Stop, null, null, messages);
        }

        return (HandlerContinuation.Continue, invitation, tenantResult.Value, messages);
    }

    public static async Task<(ErrorOr<Success>, OutgoingMessages)> HandleAsync(
        ResendTenantInvitationCommand command,
        TenantInvitationEntity invitation,
        TenantEntity tenant,
        ITenantInvitationRepository invitationRepository,
        IUnitOfWork unitOfWork,
        ISecureTokenGenerator tokenGenerator,
        CancellationToken ct
    )
    {
        var rawToken = tokenGenerator.GenerateToken();
        invitation.TokenHash = tokenGenerator.HashToken(rawToken);

        await invitationRepository.UpdateAsync(invitation, ct);
        await unitOfWork.CommitAsync(ct);

        return (
            Result.Success,
            [
                new TenantInvitationCreatedNotification(
                    invitation.Id,
                    invitation.Email,
                    tenant.Name,
                    rawToken
                ),
                new CacheInvalidationNotification(CacheTags.TenantInvitations),
            ]
        );
    }
}
