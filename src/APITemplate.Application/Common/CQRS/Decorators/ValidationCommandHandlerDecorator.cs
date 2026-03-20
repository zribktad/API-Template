using FluentValidation;

namespace APITemplate.Application.Common.CQRS.Decorators;

/// <summary>
/// Decorator that runs FluentValidation before delegating to the inner command handler (with result).
/// </summary>
public sealed class ValidationCommandHandlerDecorator<TCommand, TResult>
    : ICommandHandler<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    private readonly ICommandHandler<TCommand, TResult> _inner;
    private readonly IServiceProvider _serviceProvider;
    private readonly IEnumerable<IValidator<TCommand>> _requestValidators;

    public ValidationCommandHandlerDecorator(
        ICommandHandler<TCommand, TResult> inner,
        IServiceProvider serviceProvider,
        IEnumerable<IValidator<TCommand>> requestValidators
    )
    {
        _inner = inner;
        _serviceProvider = serviceProvider;
        _requestValidators = requestValidators;
    }

    public async Task<TResult> HandleAsync(TCommand command, CancellationToken ct)
    {
        await CommandValidation.ValidateAndThrowAsync(
            command,
            _requestValidators,
            _serviceProvider,
            ct
        );
        return await _inner.HandleAsync(command, ct);
    }
}

/// <summary>
/// Decorator that runs FluentValidation before delegating to the inner void command handler.
/// </summary>
public sealed class ValidationCommandHandlerDecorator<TCommand> : ICommandHandler<TCommand>
    where TCommand : ICommand
{
    private readonly ICommandHandler<TCommand> _inner;
    private readonly IServiceProvider _serviceProvider;
    private readonly IEnumerable<IValidator<TCommand>> _requestValidators;

    public ValidationCommandHandlerDecorator(
        ICommandHandler<TCommand> inner,
        IServiceProvider serviceProvider,
        IEnumerable<IValidator<TCommand>> requestValidators
    )
    {
        _inner = inner;
        _serviceProvider = serviceProvider;
        _requestValidators = requestValidators;
    }

    public async Task HandleAsync(TCommand command, CancellationToken ct)
    {
        await CommandValidation.ValidateAndThrowAsync(
            command,
            _requestValidators,
            _serviceProvider,
            ct
        );
        await _inner.HandleAsync(command, ct);
    }
}
