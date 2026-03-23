using APITemplate.Application.Features.User.Specifications;
using APITemplate.Domain.Entities.Contracts;
using APITemplate.Domain.Interfaces;

namespace APITemplate.Application.Features.User;

public sealed record GetUserByIdQuery(Guid Id) : IHasId;

public sealed class GetUserByIdQueryHandler
{
    public static async Task<UserResponse?> HandleAsync(
        GetUserByIdQuery request,
        IUserRepository repository,
        CancellationToken ct
    ) => await repository.FirstOrDefaultAsync(new UserByIdSpecification(request.Id), ct);
}
