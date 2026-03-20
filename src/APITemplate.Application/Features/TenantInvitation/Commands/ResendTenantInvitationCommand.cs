using APITemplate.Application.Common.Context;
using APITemplate.Application.Common.CQRS;
using APITemplate.Application.Common.Email;
using APITemplate.Application.Common.Events;
using APITemplate.Application.Common.Extensions;
using APITemplate.Domain.Enums;
using APITemplate.Domain.Exceptions;
using APITemplate.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace APITemplate.Application.Features.TenantInvitation;

public sealed record ResendTenantInvitationCommand(Guid InvitationId) : ICommand;

public sealed class ResendTenantInvitationCommandHandler
    : ICommandHandler<ResendTenantInvitationCommand>
{
    private readonly ITenantInvitationRepository _invitationRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISecureTokenGenerator _tokenGenerator;
    private readonly IEventPublisher _publisher;
    private readonly ITenantProvider _tenantProvider;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ResendTenantInvitationCommandHandler> _logger;

    public ResendTenantInvitationCommandHandler(
        ITenantInvitationRepository invitationRepository,
        ITenantRepository tenantRepository,
        IUnitOfWork unitOfWork,
        ISecureTokenGenerator tokenGenerator,
        IEventPublisher publisher,
        ITenantProvider tenantProvider,
        TimeProvider timeProvider,
        ILogger<ResendTenantInvitationCommandHandler> logger
    )
    {
        _invitationRepository = invitationRepository;
        _tenantRepository = tenantRepository;
        _unitOfWork = unitOfWork;
        _tokenGenerator = tokenGenerator;
        _publisher = publisher;
        _tenantProvider = tenantProvider;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task HandleAsync(ResendTenantInvitationCommand command, CancellationToken ct)
    {
        var invitation = await _invitationRepository.GetByIdOrThrowAsync(
            command.InvitationId,
            ErrorCatalog.Invitations.NotFound,
            ct
        );

        if (invitation.Status != InvitationStatus.Pending)
            throw new ConflictException(
                "Only pending invitations can be resent.",
                ErrorCatalog.Invitations.NotPending
            );

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        if (invitation.ExpiresAtUtc < now)
            throw new ConflictException(
                "Invitation has expired. Create a new one instead.",
                ErrorCatalog.Invitations.Expired
            );

        var tenant = await _tenantRepository.GetByIdOrThrowAsync(
            _tenantProvider.TenantId,
            ErrorCatalog.Tenants.NotFound,
            ct
        );

        var rawToken = _tokenGenerator.GenerateToken();
        invitation.TokenHash = _tokenGenerator.HashToken(rawToken);

        await _invitationRepository.UpdateAsync(invitation, ct);
        await _unitOfWork.CommitAsync(ct);

        await _publisher.PublishSafeAsync(
            new TenantInvitationCreatedNotification(
                invitation.Id,
                invitation.Email,
                tenant.Name,
                rawToken
            ),
            _logger,
            ct
        );

        await _publisher.PublishAsync(
            new CacheInvalidationNotification(CacheTags.TenantInvitations),
            ct
        );
    }
}
