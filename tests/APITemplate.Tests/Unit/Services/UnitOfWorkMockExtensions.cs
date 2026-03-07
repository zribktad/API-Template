using APITemplate.Domain.Interfaces;
using Moq;

namespace APITemplate.Tests.Unit.Services;

internal static class UnitOfWorkMockExtensions
{
    public static void SetupImmediateTransactionExecution(this Mock<IUnitOfWork> unitOfWorkMock)
    {
        unitOfWorkMock
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<Task> action, CancellationToken _) => action());
    }

    public static void SetupImmediateTransactionExecution<T>(this Mock<IUnitOfWork> unitOfWorkMock)
    {
        unitOfWorkMock
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task<T>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<Task<T>> action, CancellationToken _) => action());
    }
}
