using APITemplate.Application.Common.Context;
using APITemplate.Application.Common.Email;
using APITemplate.Application.Common.Errors;
using APITemplate.Application.Common.Events;
using APITemplate.Application.Common.Extensions;
using APITemplate.Application.Common.Options;
using APITemplate.Application.Features.TenantInvitation.DTOs;
using APITemplate.Application.Features.TenantInvitation.Mappings;
using APITemplate.Domain.Entities;
using APITemplate.Domain.Interfaces;
using ErrorOr;
using Microsoft.Extensions.Options;
using Wolverine;
using TenantEntity = APITemplate.Domain.Entities.Tenant;
using TenantInvitationEntity = APITemplate.Domain.Entities.TenantInvitation;

namespace APITemplate.Application.Features.TenantInvitation;

public sealed record CreateTenantInvitationCommand(CreateTenantInvitationRequest Request);

public sealed class CreateTenantInvitationCommandHandler
{
    public static async Task<(HandlerContinuation, TenantEntity?, OutgoingMessages)> LoadAsync(
        CreateTenantInvitationCommand command,
        ITenantInvitationRepository invitationRepository,
        ITenantRepository tenantRepository,
        ITenantProvider tenantProvider,
        CancellationToken ct
    )
    {
        OutgoingMessages messages = new();
        var normalizedEmail = AppUser.NormalizeEmail(command.Request.Email);

        if (await invitationRepository.HasPendingInvitationAsync(normalizedEmail, ct))
        {
            messages.RespondToSender(
                (ErrorOr<TenantInvitationResponse>)
                    DomainErrors.Invitations.AlreadyPending(command.Request.Email)
            );
            return (HandlerContinuation.Stop, null, messages);
        }

        var tenantResult = await tenantRepository.GetByIdOrError(
            tenantProvider.TenantId,
            DomainErrors.Tenants.NotFound(tenantProvider.TenantId),
            ct
        );
        if (tenantResult.IsError)
        {
            messages.RespondToSender((ErrorOr<TenantInvitationResponse>)tenantResult.Errors);
            return (HandlerContinuation.Stop, null, messages);
        }

        return (HandlerContinuation.Continue, tenantResult.Value, messages);
    }

    public static async Task<(ErrorOr<TenantInvitationResponse>, OutgoingMessages)> HandleAsync(
        CreateTenantInvitationCommand command,
        TenantEntity tenant,
        ITenantInvitationRepository invitationRepository,
        IUnitOfWork unitOfWork,
        ISecureTokenGenerator tokenGenerator,
        TimeProvider timeProvider,
        IOptions<EmailOptions> emailOptions,
        CancellationToken ct
    )
    {
        var emailOpts = emailOptions.Value;

        var rawToken = tokenGenerator.GenerateToken();
        var tokenHash = tokenGenerator.HashToken(rawToken);

        var invitation = new TenantInvitationEntity
        {
            Id = Guid.NewGuid(),
            Email = command.Request.Email.Trim(),
            NormalizedEmail = AppUser.NormalizeEmail(command.Request.Email),
            TokenHash = tokenHash,
            ExpiresAtUtc = timeProvider
                .GetUtcNow()
                .UtcDateTime.AddHours(emailOpts.InvitationTokenExpiryHours),
        };

        await invitationRepository.AddAsync(invitation, ct);
        await unitOfWork.CommitAsync(ct);

        return (
            invitation.ToResponse(),
            [
                new TenantInvitationCreatedNotification(
                    invitation.Id,
                    invitation.Email,
                    tenant.Name,
                    rawToken
                ),
                new CacheInvalidationNotification(CacheTags.TenantInvitations),
            ]
        );
    }
}
