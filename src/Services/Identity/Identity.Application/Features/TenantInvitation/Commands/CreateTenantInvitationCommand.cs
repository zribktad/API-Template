using Contracts.IntegrationEvents.Identity;
using ErrorOr;
using Identity.Application.Errors;
using Identity.Application.Features.TenantInvitation.DTOs;
using Identity.Application.Features.TenantInvitation.Mappings;
using Identity.Application.Options;
using Identity.Application.Security;
using Identity.Domain.Entities;
using Identity.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharedKernel.Application.Context;
using SharedKernel.Application.Extensions;
using SharedKernel.Domain.Interfaces;
using Wolverine;
using TenantInvitationEntity = Identity.Domain.Entities.TenantInvitation;

namespace Identity.Application.Features.TenantInvitation.Commands;

public sealed record CreateTenantInvitationCommand(CreateTenantInvitationRequest Request);

public sealed class CreateTenantInvitationCommandHandler
{
    public static async Task<ErrorOr<TenantInvitationResponse>> HandleAsync(
        CreateTenantInvitationCommand command,
        ITenantInvitationRepository invitationRepository,
        ITenantRepository tenantRepository,
        IUnitOfWork unitOfWork,
        ISecureTokenGenerator tokenGenerator,
        IMessageBus bus,
        ITenantProvider tenantProvider,
        TimeProvider timeProvider,
        IOptions<InvitationOptions> invitationOptions,
        ILogger<CreateTenantInvitationCommandHandler> logger,
        CancellationToken ct
    )
    {
        InvitationOptions opts = invitationOptions.Value;
        string normalizedEmail = AppUser.NormalizeEmail(command.Request.Email);

        if (await invitationRepository.HasPendingInvitationAsync(normalizedEmail, ct))
            return DomainErrors.Invitations.AlreadyPending(command.Request.Email);

        ErrorOr<Domain.Entities.Tenant> tenantResult = await tenantRepository.GetByIdOrError(
            tenantProvider.TenantId,
            DomainErrors.Tenants.NotFound(tenantProvider.TenantId),
            ct
        );
        if (tenantResult.IsError)
            return tenantResult.Errors;
        Domain.Entities.Tenant tenant = tenantResult.Value;

        string rawToken = tokenGenerator.GenerateToken();
        string tokenHash = tokenGenerator.HashToken(rawToken);

        TenantInvitationEntity invitation = new()
        {
            Id = Guid.NewGuid(),
            Email = command.Request.Email.Trim(),
            NormalizedEmail = normalizedEmail,
            TokenHash = tokenHash,
            ExpiresAtUtc = timeProvider
                .GetUtcNow()
                .UtcDateTime.AddHours(opts.InvitationTokenExpiryHours),
        };

        await invitationRepository.AddAsync(invitation, ct);
        await unitOfWork.CommitAsync(ct);

        try
        {
            await bus.PublishAsync(
                new TenantInvitationCreatedIntegrationEvent(
                    invitation.Id,
                    invitation.Email,
                    tenant.Name,
                    rawToken,
                    timeProvider.GetUtcNow().UtcDateTime
                )
            );
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(
                ex,
                "Failed to publish {EventType}.",
                nameof(TenantInvitationCreatedIntegrationEvent)
            );
        }

        return invitation.ToResponse();
    }
}
