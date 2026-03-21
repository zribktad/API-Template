using System.Linq.Expressions;

namespace APITemplate.Infrastructure.Repositories.Pagination;

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
    internal static Expression<Func<T, PagedRow<TResult>>> Build<T, TResult>(
        Expression<Func<T, TResult>> selector,
        IQueryable<T> countSource
    )
    {
        var entityParam = selector.Parameters[0];

        var countCall = Expression.Call(
            typeof(Queryable),
            nameof(Queryable.Count),
            [typeof(T)],
            countSource.Expression
        );

        var ctor = typeof(PagedRow<TResult>).GetConstructors()[0];
        var newExpr = Expression.New(ctor, selector.Body, countCall);

        return Expression.Lambda<Func<T, PagedRow<TResult>>>(newExpr, entityParam);
    }
}
