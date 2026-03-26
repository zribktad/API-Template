using System.Linq.Expressions;

namespace SharedKernel.Infrastructure.Repositories.Pagination;

/// <summary>
/// Composes an existing projection expression with a scalar COUNT sub-query
/// so that EF Core can retrieve both the projected items and the total count
/// in a single SQL round-trip.
/// </summary>
internal static class PagedProjectionBuilder
{
    /// <summary>
    /// Builds <c>entity =&gt; new PagedRow&lt;TResult&gt;(selector(entity), countSource.Count())</c>
    /// as an expression tree that EF Core translates into a scalar sub-query for the count.
    /// </summary>
    internal static Expression<Func<T, PagedRow<TResult>>> BuildPaged<T, TResult>(
        this Expression<Func<T, TResult>> selector,
        IQueryable<T> countSource
    )
    {
        // Build an expression node that represents Queryable.Count<T>(countSource).
        MethodCallExpression countCall = Expression.Call(
            typeof(Queryable),
            nameof(Queryable.Count),
            [typeof(T)],
            countSource.Expression
        );

        // Get the PagedRow<TResult>(TResult item, int totalCount) constructor via reflection.
        System.Reflection.ConstructorInfo ctor =
            typeof(PagedRow<TResult>).GetConstructor([typeof(TResult), typeof(int)])
            ?? throw new InvalidOperationException(
                $"No suitable constructor found for {typeof(PagedRow<TResult>)} with parameters (TResult, int)."
            );

        // Combine: new PagedRow<TResult>(selector.Body, countCall)
        NewExpression newExpr = Expression.New(ctor, selector.Body, countCall);

        // Reuse the lambda parameter from the original selector.
        ParameterExpression entityParam = selector.Parameters[0];

        return Expression.Lambda<Func<T, PagedRow<TResult>>>(newExpr, entityParam);
    }
}
