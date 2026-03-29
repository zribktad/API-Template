using APITemplate.Application.Common.Errors;
using APITemplate.Application.Common.Extensions;
using APITemplate.Domain.Enums;
using APITemplate.Domain.Interfaces;
using ErrorOr;
using SharedKernel.Application.Common.Events;
using Wolverine;

namespace APITemplate.Application.Features.TenantInvitation;

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
        var invitationResult = await invitationRepository.GetByIdOrError(
            command.InvitationId,
            DomainErrors.Invitations.NotFound(command.InvitationId),
            ct
        );
        if (invitationResult.IsError)
            return (invitationResult.Errors, CacheInvalidationCascades.None);
        var invitation = invitationResult.Value;

        invitation.Status = InvitationStatus.Revoked;
        await invitationRepository.UpdateAsync(invitation, ct);
        await unitOfWork.CommitAsync(ct);

        return (Result.Success, CacheInvalidationCascades.ForTag(CacheTags.TenantInvitations));
    }
}
