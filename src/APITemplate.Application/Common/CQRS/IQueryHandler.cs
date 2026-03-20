namespace APITemplate.Application.Common.CQRS;

/// <summary>Handles a query of type <typeparamref name="TQuery"/> returning <typeparamref name="TResult"/>.</summary>
public interface IQueryHandler<in TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken ct);
}
