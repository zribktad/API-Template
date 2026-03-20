using APITemplate.Application.Common.CQRS;
using APITemplate.Application.Features.User.Specifications;
using APITemplate.Domain.Interfaces;

namespace APITemplate.Application.Features.User;

public sealed record GetUserByIdQuery(Guid Id) : IQuery<UserResponse?>;

public sealed class GetUserByIdQueryHandler : IQueryHandler<GetUserByIdQuery, UserResponse?>
{
    private readonly IUserRepository _repository;

    public GetUserByIdQueryHandler(IUserRepository repository) => _repository = repository;

    public async Task<UserResponse?> HandleAsync(GetUserByIdQuery request, CancellationToken ct) =>
        await _repository.FirstOrDefaultAsync(new UserByIdSpecification(request.Id), ct);
}
