using APITemplate.Application.Common.CQRS;
using APITemplate.Application.Common.Extensions;
using APITemplate.Application.Features.TenantInvitation.DTOs;
using APITemplate.Application.Features.TenantInvitation.Specifications;
using APITemplate.Domain.Interfaces;

namespace APITemplate.Application.Features.TenantInvitation;

public sealed record GetTenantInvitationsQuery(TenantInvitationFilter Filter)
    : IQuery<PagedResponse<TenantInvitationResponse>>;

public sealed class GetTenantInvitationsQueryHandler
    : IQueryHandler<GetTenantInvitationsQuery, PagedResponse<TenantInvitationResponse>>
{
    private readonly ITenantInvitationRepository _invitationRepository;

    public GetTenantInvitationsQueryHandler(ITenantInvitationRepository invitationRepository) =>
        _invitationRepository = invitationRepository;

    public async Task<PagedResponse<TenantInvitationResponse>> HandleAsync(
        GetTenantInvitationsQuery request,
        CancellationToken ct
    )
    {
        return await _invitationRepository.GetPagedAsync(
            new TenantInvitationFilterSpecification(request.Filter),
            new TenantInvitationCountSpecification(request.Filter),
            request.Filter.PageNumber,
            request.Filter.PageSize,
            ct
        );
    }
}
