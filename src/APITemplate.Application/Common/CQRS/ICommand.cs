namespace APITemplate.Application.Common.CQRS;

/// <summary>Marker interface for void commands handled by <see cref="ICommandHandler{TCommand}"/>.</summary>
public interface ICommand { }

/// <summary>Marker interface for commands that return a result, handled by <see cref="ICommandHandler{TCommand, TResult}"/>.</summary>
public interface ICommand<TResult> { }
