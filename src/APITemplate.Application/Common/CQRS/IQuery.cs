namespace APITemplate.Application.Common.CQRS;

/// <summary>Marker interface for queries handled by <see cref="IQueryHandler{TQuery, TResult}"/>.</summary>
public interface IQuery<TResult> { }
