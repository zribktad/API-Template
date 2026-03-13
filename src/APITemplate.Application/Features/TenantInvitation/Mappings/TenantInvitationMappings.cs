using System.Linq.Expressions;
using APITemplate.Application.Features.TenantInvitation.DTOs;
using TenantInvitationEntity = APITemplate.Domain.Entities.TenantInvitation;

namespace APITemplate.Application.Features.TenantInvitation.Mappings;

public static class TenantInvitationMappings
{
    public static readonly Expression<
        Func<TenantInvitationEntity, TenantInvitationResponse>
    > Projection = i => new TenantInvitationResponse(
        i.Id,
        i.Email,
        i.Status,
        i.ExpiresAtUtc,
        i.Audit.CreatedAtUtc
    );

    private static readonly Func<
        TenantInvitationEntity,
        TenantInvitationResponse
    > CompiledProjection = Projection.Compile();

    public static TenantInvitationResponse ToResponse(this TenantInvitationEntity invitation) =>
        CompiledProjection(invitation);
}
