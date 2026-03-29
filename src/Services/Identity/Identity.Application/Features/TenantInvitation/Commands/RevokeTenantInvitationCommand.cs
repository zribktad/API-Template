using ErrorOr;
using Identity.Application.Errors;
using Identity.Domain.Enums;
using Identity.Domain.Interfaces;
using SharedKernel.Application.Common.Events;
using SharedKernel.Application.Extensions;
using SharedKernel.Domain.Interfaces;
using Wolverine;

namespace Identity.Application.Features.TenantInvitation.Commands;

public sealed record RevokeTenantInvitationCommand(Guid InvitationId);

public sealed class RevokeTenantInvitationCommandHandler
{
    public static async Task<(ErrorOr<Success>, OutgoingMessages)> HandleAsync(
        RevokeTenantInvitationCommand command,
        ITenantInvitationRepository invitationRepository,
        IUnitOfWork unitOfWork,
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

        invitation.Status = InvitationStatus.Revoked;
        await invitationRepository.UpdateAsync(invitation, ct);
        await unitOfWork.CommitAsync(ct);
        return (Result.Success, CacheInvalidationCascades.ForTag(CacheTags.TenantInvitations));
    }
}
