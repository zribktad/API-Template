using System.Reflection;
using ErrorOr;
using FluentValidation;

namespace APITemplate.Api.Extensions;

internal static class WolverineTypeExtensions
{
    internal static bool IsErrorOrReturnType(this Type returnType)
    {
        if (!returnType.IsGenericType)
            return false;

        var genericTypeDefinition = returnType.GetGenericTypeDefinition();

        if (genericTypeDefinition == typeof(Task<>) || genericTypeDefinition == typeof(ValueTask<>))
            return returnType.GetGenericArguments()[0].IsErrorOrReturnType();

        if (genericTypeDefinition == typeof(ErrorOr<>))
            return true;

        // Support cascading message tuples like (ErrorOr<T>, OutgoingMessages).
        if (returnType.IsValueTupleType())
            return returnType.GetGenericArguments().Any(arg => arg.IsErrorOrReturnType());

        return false;
    }

    private static bool IsValueTupleType(this Type type) =>
        type.IsGenericType
        && type.FullName is { } name
        && name.StartsWith("System.ValueTuple`", StringComparison.Ordinal);

    internal static bool HasValidatorIn(this Type messageType, Assembly assembly)
    {
        var validatorType = typeof(IValidator<>).MakeGenericType(messageType);
        return assembly
            .GetTypes()
            .Where(type => !type.IsAbstract && !type.IsGenericTypeDefinition)
            .Any(type => validatorType.IsAssignableFrom(type));
    }
}
