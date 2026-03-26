using ErrorOr;
using Identity.Application.Errors;
using Identity.Application.Extensions;
using Identity.Domain.Enums;
using Identity.Domain.Interfaces;
using SharedKernel.Domain.Interfaces;

namespace Identity.Application.Features.TenantInvitation.Commands;

public sealed record RevokeTenantInvitationCommand(Guid InvitationId);

public sealed class RevokeTenantInvitationCommandHandler
{
    public static async Task<ErrorOr<Success>> HandleAsync(
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
            return invitationResult.Errors;
        Domain.Entities.TenantInvitation invitation = invitationResult.Value;

        invitation.Status = InvitationStatus.Revoked;
        await invitationRepository.UpdateAsync(invitation, ct);
        await unitOfWork.CommitAsync(ct);

        return Result.Success;
    }
}
