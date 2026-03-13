using APITemplate.Application.Common.DTOs;
using APITemplate.Domain.Enums;

namespace APITemplate.Application.Features.TenantInvitation.DTOs;

public sealed record TenantInvitationFilter(
    string? Email = null,
    InvitationStatus? Status = null,
    int PageNumber = 1,
    int PageSize = PaginationFilter.DefaultPageSize
) : PaginationFilter(PageNumber, PageSize);
