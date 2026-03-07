using APITemplate.Domain.Interfaces;

namespace APITemplate.Application.Common.Persistence;

public static class UnitOfWorkWriteExtensions
{
    /// <summary>
    /// Wraps a service write in the template's uniform transaction pattern.
    /// Some single-write flows do not strictly need an explicit transaction, but the template uses one shape for all relational writes.
    /// </summary>
    public static Task ExecuteTransactionalWriteAsync(
        this IUnitOfWork unitOfWork,
        Func<Task> action,
        CancellationToken ct = default)
        => unitOfWork.ExecuteInTransactionAsync(action, ct);

    /// <summary>
    /// Wraps a service write in the template's uniform transaction pattern and returns a value created inside the write flow.
    /// Some single-write flows do not strictly need an explicit transaction, but the template uses one shape for all relational writes.
    /// </summary>
    public static Task<T> ExecuteTransactionalWriteAsync<T>(
        this IUnitOfWork unitOfWork,
        Func<Task<T>> action,
        CancellationToken ct = default)
        => unitOfWork.ExecuteInTransactionAsync(action, ct);
}
