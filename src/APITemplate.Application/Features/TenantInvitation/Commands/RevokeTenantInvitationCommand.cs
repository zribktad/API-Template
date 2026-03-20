using APITemplate.Application.Common.CQRS;
using APITemplate.Application.Common.Events;
using APITemplate.Application.Common.Extensions;
using APITemplate.Domain.Enums;
using APITemplate.Domain.Exceptions;
using APITemplate.Domain.Interfaces;

namespace APITemplate.Application.Features.TenantInvitation;

public sealed record RevokeTenantInvitationCommand(Guid InvitationId) : ICommand;

public sealed class RevokeTenantInvitationCommandHandler
    : ICommandHandler<RevokeTenantInvitationCommand>
{
    private readonly ITenantInvitationRepository _invitationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _publisher;

    public RevokeTenantInvitationCommandHandler(
        ITenantInvitationRepository invitationRepository,
        IUnitOfWork unitOfWork,
        IEventPublisher publisher
    )
    {
        _invitationRepository = invitationRepository;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
    }

    public async Task HandleAsync(RevokeTenantInvitationCommand command, CancellationToken ct)
    {
        var invitation = await _invitationRepository.GetByIdOrThrowAsync(
            command.InvitationId,
            ErrorCatalog.Invitations.NotFound,
            ct
        );

        invitation.Status = InvitationStatus.Revoked;
        await _invitationRepository.UpdateAsync(invitation, ct);
        await _unitOfWork.CommitAsync(ct);

        await _publisher.PublishAsync(new TenantInvitationsChangedNotification(), ct);
    }
}
