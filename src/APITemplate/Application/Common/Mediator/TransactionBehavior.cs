using APITemplate.Domain.Interfaces;
using MediatR;

namespace APITemplate.Application.Common.Mediator;

public sealed class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IUnitOfWork _unitOfWork;

    public TransactionBehavior(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not ITransactionalRequest)
            return await next();

        TResponse response = default!;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            response = await next();
        }, cancellationToken);

        return response;
    }
}
