using APITemplate.Application.Common.Context;
using APITemplate.Application.Common.CQRS;
using APITemplate.Application.Common.Email;
using APITemplate.Application.Common.Events;
using APITemplate.Application.Common.Extensions;
using APITemplate.Application.Common.Options;
using APITemplate.Application.Features.TenantInvitation.DTOs;
using APITemplate.Application.Features.TenantInvitation.Mappings;
using APITemplate.Domain.Entities;
using APITemplate.Domain.Exceptions;
using APITemplate.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TenantInvitationEntity = APITemplate.Domain.Entities.TenantInvitation;

namespace APITemplate.Application.Features.TenantInvitation;

public sealed record CreateTenantInvitationCommand(CreateTenantInvitationRequest Request)
    : ICommand<TenantInvitationResponse>;

public sealed class CreateTenantInvitationCommandHandler
    : ICommandHandler<CreateTenantInvitationCommand, TenantInvitationResponse>
{
    private readonly ITenantInvitationRepository _invitationRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISecureTokenGenerator _tokenGenerator;
    private readonly IEventPublisher _publisher;
    private readonly ITenantProvider _tenantProvider;
    private readonly TimeProvider _timeProvider;
    private readonly EmailOptions _emailOptions;
    private readonly ILogger<CreateTenantInvitationCommandHandler> _logger;

    public CreateTenantInvitationCommandHandler(
        ITenantInvitationRepository invitationRepository,
        ITenantRepository tenantRepository,
        IUnitOfWork unitOfWork,
        ISecureTokenGenerator tokenGenerator,
        IEventPublisher publisher,
        ITenantProvider tenantProvider,
        TimeProvider timeProvider,
        IOptions<EmailOptions> emailOptions,
        ILogger<CreateTenantInvitationCommandHandler> logger
    )
    {
        _invitationRepository = invitationRepository;
        _tenantRepository = tenantRepository;
        _unitOfWork = unitOfWork;
        _tokenGenerator = tokenGenerator;
        _publisher = publisher;
        _tenantProvider = tenantProvider;
        _timeProvider = timeProvider;
        _emailOptions = emailOptions.Value;
        _logger = logger;
    }

    public async Task<TenantInvitationResponse> HandleAsync(
        CreateTenantInvitationCommand command,
        CancellationToken ct
    )
    {
        var normalizedEmail = AppUser.NormalizeEmail(command.Request.Email);

        if (await _invitationRepository.HasPendingInvitationAsync(normalizedEmail, ct))
            throw new ConflictException(
                $"A pending invitation already exists for '{command.Request.Email}'.",
                ErrorCatalog.Invitations.AlreadyPending
            );

        var tenant = await _tenantRepository.GetByIdOrThrowAsync(
            _tenantProvider.TenantId,
            ErrorCatalog.Tenants.NotFound,
            ct
        );

        var rawToken = _tokenGenerator.GenerateToken();
        var tokenHash = _tokenGenerator.HashToken(rawToken);

        var invitation = new TenantInvitationEntity
        {
            Id = Guid.NewGuid(),
            Email = command.Request.Email.Trim(),
            NormalizedEmail = normalizedEmail,
            TokenHash = tokenHash,
            ExpiresAtUtc = _timeProvider
                .GetUtcNow()
                .UtcDateTime.AddHours(_emailOptions.InvitationTokenExpiryHours),
        };

        await _invitationRepository.AddAsync(invitation, ct);
        await _unitOfWork.CommitAsync(ct);

        try
        {
            await _publisher.PublishAsync(
                new TenantInvitationCreatedNotification(
                    invitation.Id,
                    invitation.Email,
                    tenant.Name,
                    rawToken
                ),
                ct
            );
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(
                ex,
                "Failed to publish TenantInvitationCreatedNotification for invitation {InvitationId}.",
                invitation.Id
            );
        }

        await _publisher.PublishAsync(new TenantInvitationsChangedNotification(), ct);
        return invitation.ToResponse();
    }
}
