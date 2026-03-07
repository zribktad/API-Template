namespace APITemplate.Domain.Interfaces;

public interface IUnitOfWork
{
    /// <summary>
    /// Persists all staged relational changes for the current service operation.
    /// Use this for single-write flows after repository calls.
    /// </summary>
    Task CommitAsync(CancellationToken ct = default);

    /// <summary>
    /// Runs a multi-step relational write flow in one explicit transaction.
    /// The delegate should stage repository changes only; do not call <see cref="CommitAsync"/> inside it.
    /// Example:
    /// await _unitOfWork.ExecuteInTransactionAsync(async () =>
    /// {
    ///     await _productRepository.UpdateAsync(product, ct);
    ///     await _reviewRepository.AddAsync(review, ct);
    /// }, ct);
    /// </summary>
    Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken ct = default);

    /// <summary>
    /// Runs a multi-step relational write flow in one explicit transaction and returns a value.
    /// The delegate should stage repository changes only; do not call <see cref="CommitAsync"/> inside it.
    /// </summary>
    Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action, CancellationToken ct = default);
}
