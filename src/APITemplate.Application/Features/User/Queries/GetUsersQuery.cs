using APITemplate.Application.Common.CQRS;
using APITemplate.Application.Features.User.Specifications;
using APITemplate.Domain.Interfaces;

namespace APITemplate.Application.Features.User;

public sealed record GetUsersQuery(UserFilter Filter) : IQuery<PagedResponse<UserResponse>>;

public sealed class GetUsersQueryHandler : IQueryHandler<GetUsersQuery, PagedResponse<UserResponse>>
{
    private readonly IUserRepository _repository;

    public GetUsersQueryHandler(IUserRepository repository) => _repository = repository;

    public async Task<PagedResponse<UserResponse>> HandleAsync(
        GetUsersQuery request,
        CancellationToken ct
    )
    {
        return await _repository.GetPagedAsync(
            new UserFilterSpecification(request.Filter),
            request.Filter.PageNumber,
            request.Filter.PageSize,
            ct
        );
    }
}
