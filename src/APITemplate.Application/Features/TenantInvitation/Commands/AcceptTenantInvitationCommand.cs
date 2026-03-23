using APITemplate.Application.Common.Email;
using APITemplate.Application.Common.Events;
using APITemplate.Domain.Enums;
using APITemplate.Domain.Exceptions;
using APITemplate.Domain.Interfaces;
using Wolverine;

namespace APITemplate.Application.Features.TenantInvitation;

public sealed record AcceptTenantInvitationCommand(string Token);

public sealed class AcceptTenantInvitationCommandHandler
{
    public static async Task HandleAsync(
        AcceptTenantInvitationCommand command,
        ITenantInvitationRepository invitationRepository,
        IUnitOfWork unitOfWork,
        ISecureTokenGenerator tokenGenerator,
        IMessageBus bus,
        TimeProvider timeProvider,
        CancellationToken ct
    )
    {
        var tokenHash = tokenGenerator.HashToken(command.Token);
        var invitation =
            await invitationRepository.GetValidByTokenHashAsync(tokenHash, ct)
            ?? throw new NotFoundException(
                ErrorCatalog.Invitations.NotFoundOrExpiredMessage,
                ErrorCatalog.Invitations.NotFound
            );

        var now = timeProvider.GetUtcNow().UtcDateTime;

        if (invitation.ExpiresAtUtc < now)
            throw new ConflictException(
                ErrorCatalog.Invitations.ExpiredMessage,
                ErrorCatalog.Invitations.Expired
            );

        if (invitation.Status == InvitationStatus.Accepted)
            throw new ConflictException(
                ErrorCatalog.Invitations.AlreadyAcceptedMessage,
                ErrorCatalog.Invitations.AlreadyAccepted
            );

        invitation.Status = InvitationStatus.Accepted;
        await invitationRepository.UpdateAsync(invitation, ct);
        await unitOfWork.CommitAsync(ct);

        await bus.PublishAsync(new CacheInvalidationNotification(CacheTags.TenantInvitations));
    }
}
