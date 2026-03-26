using ErrorOr;
using Identity.Application.Errors;
using Identity.Application.Security;
using Identity.Domain.Enums;
using Identity.Domain.Interfaces;
using SharedKernel.Domain.Interfaces;

namespace Identity.Application.Features.TenantInvitation.Commands;

public sealed record AcceptTenantInvitationCommand(string Token);

public sealed class AcceptTenantInvitationCommandHandler
{
    public static async Task<ErrorOr<Success>> HandleAsync(
        AcceptTenantInvitationCommand command,
        ITenantInvitationRepository invitationRepository,
        IUnitOfWork unitOfWork,
        ISecureTokenGenerator tokenGenerator,
        TimeProvider timeProvider,
        CancellationToken ct
    )
    {
        string tokenHash = tokenGenerator.HashToken(command.Token);
        Domain.Entities.TenantInvitation? invitation =
            await invitationRepository.GetValidByTokenHashAsync(tokenHash, ct);

        if (invitation is null)
            return DomainErrors.Invitations.NotFoundOrExpired();

        DateTime now = timeProvider.GetUtcNow().UtcDateTime;

        if (invitation.ExpiresAtUtc < now)
            return DomainErrors.Invitations.Expired();

        if (invitation.Status == InvitationStatus.Accepted)
            return DomainErrors.Invitations.AlreadyAccepted();

        invitation.Status = InvitationStatus.Accepted;
        await invitationRepository.UpdateAsync(invitation, ct);
        await unitOfWork.CommitAsync(ct);

        return Result.Success;
    }
}
