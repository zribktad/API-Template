using ErrorOr;
using Identity.Application.Errors;
using Identity.Application.Features.Tenant.Commands;
using Identity.Application.Features.Tenant.DTOs;
using Identity.Domain.Interfaces;
using Moq;
using SharedKernel.Domain.Interfaces;
using Shouldly;
using Xunit;
using TenantEntity = Identity.Domain.Entities.Tenant;

namespace Identity.Tests.Features.Tenant.Commands;

public sealed class CreateTenantCommandHandlerTests
{
    private readonly Mock<ITenantRepository> _repositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    public CreateTenantCommandHandlerTests()
    {
        _unitOfWorkMock
            .Setup(u =>
                u.ExecuteInTransactionAsync(
                    It.IsAny<Func<Task<TenantEntity>>>(),
                    It.IsAny<CancellationToken>(),
                    null
                )
            )
            .Returns<Func<Task<TenantEntity>>, CancellationToken, object?>(
                (action, _, _) => action()
            );
    }

    [Fact]
    public async Task HandleAsync_WhenCodeAlreadyExists_ReturnsConflictError()
    {
        CreateTenantRequest request = new("EXISTING", "Existing Tenant");
        CreateTenantCommand command = new(request);
        _repositoryMock
            .Setup(r => r.CodeExistsAsync("EXISTING", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var (result, _) = await CreateTenantCommandHandler.HandleAsync(
            command,
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            CancellationToken.None
        );

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe(IdentityErrorCatalog.Tenants.CodeAlreadyExists);
    }

    [Fact]
    public async Task HandleAsync_WhenCodeIsUnique_CreatesTenantAndReturnsResponse()
    {
        CreateTenantRequest request = new("NEW-CODE", "New Tenant");
        CreateTenantCommand command = new(request);
        _repositoryMock
            .Setup(r => r.CodeExistsAsync("NEW-CODE", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var (result, _) = await CreateTenantCommandHandler.HandleAsync(
            command,
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            CancellationToken.None
        );

        result.IsError.ShouldBeFalse();
        result.Value.Code.ShouldBe("NEW-CODE");
        result.Value.Name.ShouldBe("New Tenant");
        _repositoryMock.Verify(
            r => r.AddAsync(It.IsAny<TenantEntity>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }
}
