using APITemplate.Application.Common.CQRS;
using APITemplate.Application.Common.Email;
using APITemplate.Application.Common.Events;
using APITemplate.Domain.Enums;
using APITemplate.Domain.Exceptions;
using APITemplate.Domain.Interfaces;

namespace APITemplate.Application.Features.TenantInvitation;

public sealed record AcceptTenantInvitationCommand(string Token) : ICommand;

public sealed class AcceptTenantInvitationCommandHandler
    : ICommandHandler<AcceptTenantInvitationCommand>
{
    private readonly ITenantInvitationRepository _invitationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISecureTokenGenerator _tokenGenerator;
    private readonly IEventPublisher _publisher;
    private readonly TimeProvider _timeProvider;

    public AcceptTenantInvitationCommandHandler(
        ITenantInvitationRepository invitationRepository,
        IUnitOfWork unitOfWork,
        ISecureTokenGenerator tokenGenerator,
        IEventPublisher publisher,
        TimeProvider timeProvider
    )
    {
        _invitationRepository = invitationRepository;
        _unitOfWork = unitOfWork;
        _tokenGenerator = tokenGenerator;
        _publisher = publisher;
        _timeProvider = timeProvider;
    }

    public async Task HandleAsync(AcceptTenantInvitationCommand command, CancellationToken ct)
    {
        var tokenHash = _tokenGenerator.HashToken(command.Token);
        var invitation =
            await _invitationRepository.GetValidByTokenHashAsync(tokenHash, ct)
            ?? throw new NotFoundException(
                "Invitation not found or expired.",
                ErrorCatalog.Invitations.NotFound
            );

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        if (invitation.ExpiresAtUtc < now)
            throw new ConflictException(
                "Invitation has expired.",
                ErrorCatalog.Invitations.Expired
            );

        if (invitation.Status == InvitationStatus.Accepted)
            throw new ConflictException(
                "Invitation has already been accepted.",
                ErrorCatalog.Invitations.AlreadyAccepted
            );

        invitation.Status = InvitationStatus.Accepted;
        await _invitationRepository.UpdateAsync(invitation, ct);
        await _unitOfWork.CommitAsync(ct);

        await _publisher.PublishAsync(new TenantInvitationsChangedNotification(), ct);
    }
}
