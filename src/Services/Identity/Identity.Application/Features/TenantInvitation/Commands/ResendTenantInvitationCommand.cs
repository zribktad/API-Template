using Contracts.IntegrationEvents.Identity;
using ErrorOr;
using Identity.Application.Errors;
using Identity.Application.Security;
using Identity.Domain.Enums;
using Identity.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using SharedKernel.Application.Common.Events;
using SharedKernel.Application.Context;
using SharedKernel.Application.Extensions;
using SharedKernel.Domain.Interfaces;
using Wolverine;

namespace Identity.Application.Features.TenantInvitation.Commands;

public sealed record ResendTenantInvitationCommand(Guid InvitationId);

public sealed class ResendTenantInvitationCommandHandler
{
    public static async Task<(ErrorOr<Success>, OutgoingMessages)> HandleAsync(
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
        ErrorOr<Domain.Entities.TenantInvitation> invitationResult =
            await invitationRepository.GetByIdOrError(
                command.InvitationId,
                DomainErrors.Invitations.NotFound(command.InvitationId),
                ct
            );
        if (invitationResult.IsError)
            return (invitationResult.Errors, CacheInvalidationCascades.None);
        Domain.Entities.TenantInvitation invitation = invitationResult.Value;

        if (invitation.Status != InvitationStatus.Pending)
            return (DomainErrors.Invitations.NotPending(), CacheInvalidationCascades.None);

        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        if (invitation.ExpiresAtUtc < now)
            return (DomainErrors.Invitations.ExpiredCreateNew(), CacheInvalidationCascades.None);

        ErrorOr<Domain.Entities.Tenant> tenantResult = await tenantRepository.GetByIdOrError(
            tenantProvider.TenantId,
            DomainErrors.Tenants.NotFound(tenantProvider.TenantId),
            ct
        );
        if (tenantResult.IsError)
            return (tenantResult.Errors, CacheInvalidationCascades.None);
        Domain.Entities.Tenant tenant = tenantResult.Value;

        string rawToken = tokenGenerator.GenerateToken();
        invitation.TokenHash = tokenGenerator.HashToken(rawToken);

        await invitationRepository.UpdateAsync(invitation, ct);
        await unitOfWork.CommitAsync(ct);

        try
        {
            await bus.PublishAsync(
                new TenantInvitationCreatedIntegrationEvent(
                    invitation.Id,
                    invitation.Email,
                    tenant.Name,
                    rawToken,
                    timeProvider.GetUtcNow().UtcDateTime
                )
            );
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(
                ex,
                "Failed to publish {EventType}.",
                nameof(TenantInvitationCreatedIntegrationEvent)
            );
        }

        return (Result.Success, CacheInvalidationCascades.ForTag(CacheTags.TenantInvitations));
    }
}
