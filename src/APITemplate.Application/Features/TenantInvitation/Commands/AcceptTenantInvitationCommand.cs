using APITemplate.Application.Common.Email;
using APITemplate.Application.Common.Errors;
using APITemplate.Domain.Enums;
using APITemplate.Domain.Interfaces;
using ErrorOr;
using SharedKernel.Application.Common.Events;
using Wolverine;

namespace APITemplate.Application.Features.TenantInvitation;

public sealed record AcceptTenantInvitationCommand(string Token);

public sealed class AcceptTenantInvitationCommandHandler
{
    public static async Task<(ErrorOr<Success>, OutgoingMessages)> HandleAsync(
        AcceptTenantInvitationCommand command,
        ITenantInvitationRepository invitationRepository,
        IUnitOfWork unitOfWork,
        ISecureTokenGenerator tokenGenerator,
        TimeProvider timeProvider,
        CancellationToken ct
    )
    {
        var tokenHash = tokenGenerator.HashToken(command.Token);
        var invitation = await invitationRepository.GetValidByTokenHashAsync(tokenHash, ct);

        if (invitation is null)
            return (DomainErrors.Invitations.NotFoundOrExpired(), CacheInvalidationCascades.None);

        var now = timeProvider.GetUtcNow().UtcDateTime;

        if (invitation.ExpiresAtUtc < now)
            return (DomainErrors.Invitations.Expired(), CacheInvalidationCascades.None);

        if (invitation.Status == InvitationStatus.Accepted)
            return (DomainErrors.Invitations.AlreadyAccepted(), CacheInvalidationCascades.None);

        invitation.Status = InvitationStatus.Accepted;
        await invitationRepository.UpdateAsync(invitation, ct);
        await unitOfWork.CommitAsync(ct);

        return (Result.Success, CacheInvalidationCascades.ForTag(CacheTags.TenantInvitations));
    }
}
