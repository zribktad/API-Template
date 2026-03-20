namespace APITemplate.Application.Common.CQRS;

/// <summary>Handles a void command of type <typeparamref name="TCommand"/>.</summary>
public interface ICommandHandler<in TCommand>
    where TCommand : ICommand
{
    Task HandleAsync(TCommand command, CancellationToken ct);
}

/// <summary>Handles a command of type <typeparamref name="TCommand"/> that returns <typeparamref name="TResult"/>.</summary>
public interface ICommandHandler<in TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken ct);
}
