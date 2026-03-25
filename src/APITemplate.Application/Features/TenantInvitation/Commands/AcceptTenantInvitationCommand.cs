using APITemplate.Application.Common.Email;
using APITemplate.Application.Common.Errors;
using APITemplate.Application.Common.Events;
using APITemplate.Domain.Enums;
using APITemplate.Domain.Interfaces;
using ErrorOr;
using Wolverine;
using TenantInvitationEntity = APITemplate.Domain.Entities.TenantInvitation;

namespace APITemplate.Application.Features.TenantInvitation;

public sealed record AcceptTenantInvitationCommand(string Token);

public sealed class AcceptTenantInvitationCommandHandler
{
    public static async Task<(
        HandlerContinuation,
        TenantInvitationEntity?,
        OutgoingMessages
    )> LoadAsync(
        AcceptTenantInvitationCommand command,
        ITenantInvitationRepository invitationRepository,
        ISecureTokenGenerator tokenGenerator,
        TimeProvider timeProvider,
        CancellationToken ct
    )
    {
        OutgoingMessages messages = new();

        var tokenHash = tokenGenerator.HashToken(command.Token);
        var invitation = await invitationRepository.GetValidByTokenHashAsync(tokenHash, ct);

        if (invitation is null)
        {
            messages.RespondToSender(
                (ErrorOr<Success>)DomainErrors.Invitations.NotFoundOrExpired()
            );
            return (HandlerContinuation.Stop, null, messages);
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;

        if (invitation.ExpiresAtUtc < now)
        {
            messages.RespondToSender((ErrorOr<Success>)DomainErrors.Invitations.Expired());
            return (HandlerContinuation.Stop, null, messages);
        }

        if (invitation.Status == InvitationStatus.Accepted)
        {
            messages.RespondToSender((ErrorOr<Success>)DomainErrors.Invitations.AlreadyAccepted());
            return (HandlerContinuation.Stop, null, messages);
        }

        return (HandlerContinuation.Continue, invitation, messages);
    }

    public static async Task<(ErrorOr<Success>, OutgoingMessages)> HandleAsync(
        AcceptTenantInvitationCommand command,
        TenantInvitationEntity invitation,
        ITenantInvitationRepository invitationRepository,
        IUnitOfWork unitOfWork,
        CancellationToken ct
    )
    {
        invitation.Status = InvitationStatus.Accepted;
        await invitationRepository.UpdateAsync(invitation, ct);
        await unitOfWork.CommitAsync(ct);

        return (Result.Success, [new CacheInvalidationNotification(CacheTags.TenantInvitations)]);
    }
}
