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

        return genericTypeDefinition == typeof(ErrorOr<>);
    }

    internal static bool HasValidatorIn(this Type messageType, Assembly assembly)
    {
        var validatorTargets = assembly
            .GetTypes()
            .Where(type => !type.IsAbstract && !type.IsGenericTypeDefinition)
            .SelectMany(type =>
                type.GetInterfaces()
                    .Where(i =>
                        i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValidator<>)
                    )
                    .Select(i => i.GetGenericArguments()[0])
            )
            .ToHashSet();

        return messageType
            .GetValidationTargetTypes()
            .Any(validationTarget => validatorTargets.Contains(validationTarget));
    }

    private static IEnumerable<Type> GetValidationTargetTypes(this Type messageType)
    {
        yield return messageType;

        foreach (
            var propertyType in messageType
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(property => property.CanRead)
                .Select(property => property.PropertyType)
                .Distinct()
        )
        {
            yield return propertyType;
        }
    }
}
