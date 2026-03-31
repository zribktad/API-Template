using ErrorOr;
using Identity.Application.Features.User.DTOs;
using Identity.Application.Features.User.Specifications;
using Identity.Domain.Interfaces;
using SharedKernel.Domain.Common;

namespace Identity.Application.Features.User.Queries;

public sealed record GetUsersQuery(UserFilter Filter);

public sealed class GetUsersQueryHandler
{
    public static async Task<ErrorOr<PagedResponse<UserResponse>>> HandleAsync(
        GetUsersQuery request,
        IUserRepository repository,
        CancellationToken ct
    )
    {
        return await repository.GetPagedAsync(
            new UserFilterSpecification(request.Filter),
            request.Filter.PageNumber,
            request.Filter.PageSize,
            ct
        );
    }
}
