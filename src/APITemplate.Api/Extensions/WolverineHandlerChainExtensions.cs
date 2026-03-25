using System.Reflection;
using ErrorOr;
using FluentValidation;
using Wolverine.Runtime.Handlers;

namespace APITemplate.Api.Extensions;

/// <summary>
/// Wolverine handler-chain helpers used during bootstrapping to keep Program.cs focused on
/// orchestration rather than reflection-based policy rules.
/// </summary>
public static class WolverineHandlerChainExtensions
{
    /// <summary>
    /// Returns <see langword="true"/> when the chain handles a message with a registered validator
    /// and at least one handler returns <c>ErrorOr&lt;T&gt;</c> directly or through Task/ValueTask.
    /// </summary>
    public static bool ShouldApplyErrorOrValidation(
        this HandlerChain chain,
        Assembly validatorAssembly
    ) =>
        chain.MessageType.HasValidatorIn(validatorAssembly)
        && chain.Handlers.Any(h => h.Method.ReturnType.IsErrorOrReturnType());

    private static bool IsErrorOrReturnType(this Type returnType)
    {
        if (!returnType.IsGenericType)
            return false;

        var genericTypeDefinition = returnType.GetGenericTypeDefinition();

        if (genericTypeDefinition == typeof(Task<>) || genericTypeDefinition == typeof(ValueTask<>))
            return returnType.GetGenericArguments()[0].IsErrorOrReturnType();

        return genericTypeDefinition == typeof(ErrorOr<>);
    }

    private static bool HasValidatorIn(this Type messageType, Assembly assembly) =>
        assembly
            .GetTypes()
            .Where(type => !type.IsAbstract && !type.IsGenericTypeDefinition)
            .SelectMany(type =>
                type.GetInterfaces()
                    .Where(i =>
                        i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValidator<>)
                    )
                    .Select(i => i.GetGenericArguments()[0])
            )
            .Contains(messageType);
}
