using MediatR;

namespace APITemplate.Application.Common.Mediator;

public interface ITransactionalRequest;

public interface ICommand<out TResponse> : IRequest<TResponse>, ITransactionalRequest;

public interface ICommand : IRequest, ITransactionalRequest;

public interface IQuery<out TResponse> : IRequest<TResponse>;
