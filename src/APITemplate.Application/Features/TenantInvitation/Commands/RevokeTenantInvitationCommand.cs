using APITemplate.Application.Common.Events;
using APITemplate.Application.Common.Extensions;
using APITemplate.Domain.Enums;
using APITemplate.Domain.Exceptions;
using APITemplate.Domain.Interfaces;
using Wolverine;

namespace APITemplate.Application.Features.TenantInvitation;

public sealed record RevokeTenantInvitationCommand(Guid InvitationId);

public sealed class RevokeTenantInvitationCommandHandler
{
    public static async Task HandleAsync(
        RevokeTenantInvitationCommand command,
        ITenantInvitationRepository invitationRepository,
        IUnitOfWork unitOfWork,
        IMessageBus bus,
        CancellationToken ct
    )
    {
        var invitation = await invitationRepository.GetByIdOrThrowAsync(
            command.InvitationId,
            ErrorCatalog.Invitations.NotFound,
            ct
        );

        invitation.Status = InvitationStatus.Revoked;
        await invitationRepository.UpdateAsync(invitation, ct);
        await unitOfWork.CommitAsync(ct);

        await bus.PublishAsync(new CacheInvalidationNotification(CacheTags.TenantInvitations));
    }
}
