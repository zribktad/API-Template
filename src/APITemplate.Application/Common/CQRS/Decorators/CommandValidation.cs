using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using APITemplate.Application.Common.Errors;
using FluentValidation;
using FluentValidation.Results;

namespace APITemplate.Application.Common.CQRS.Decorators;

/// <summary>
/// Shared validation logic for command handler decorators.
/// Validates the command itself and its nested complex objects using FluentValidation.
/// </summary>
internal static class CommandValidation
{
    private static readonly ConcurrentDictionary<
        Type,
        PropertyInfo[]
    > ReadablePublicInstancePropertiesCache = new();
    private static readonly ConcurrentDictionary<Type, Type> ValidatorsEnumerableTypeCache = new();

    internal static async Task ValidateAndThrowAsync<TCommand>(
        TCommand command,
        IEnumerable<IValidator<TCommand>> requestValidators,
        IServiceProvider serviceProvider,
        CancellationToken ct
    )
    {
        var failures = new List<ValidationFailure>();

        failures.AddRange(await ValidateAsync(command, requestValidators, ct));

        foreach (var nestedValue in GetNestedValues(command))
            failures.AddRange(await ValidateNestedAsync(nestedValue, serviceProvider, ct));

        if (failures.Count > 0)
        {
            var message = string.Join(
                "; ",
                failures
                    .Select(failure =>
                        string.IsNullOrWhiteSpace(failure.PropertyName)
                            ? failure.ErrorMessage
                            : $"{failure.PropertyName}: {failure.ErrorMessage}"
                    )
                    .Distinct()
            );

            throw new Domain.Exceptions.ValidationException(
                message,
                ErrorCatalog.General.ValidationFailed
            );
        }
    }

    private static async Task<List<ValidationFailure>> ValidateAsync<T>(
        T value,
        IEnumerable<IValidator<T>> validators,
        CancellationToken ct
    )
    {
        var failures = new List<ValidationFailure>();
        foreach (var validator in validators)
        {
            var result = await validator.ValidateAsync(value, ct);
            if (!result.IsValid)
                failures.AddRange(result.Errors);
        }
        return failures;
    }

    private static async Task<List<ValidationFailure>> ValidateNestedAsync(
        object value,
        IServiceProvider serviceProvider,
        CancellationToken ct
    )
    {
        var validatorsType = ValidatorsEnumerableTypeCache.GetOrAdd(
            value.GetType(),
            runtimeType =>
            {
                var validatorType = typeof(IValidator<>).MakeGenericType(runtimeType);
                return typeof(IEnumerable<>).MakeGenericType(validatorType);
            }
        );

        var validators = serviceProvider.GetService(validatorsType) as IEnumerable;
        if (validators is null)
            return [];

        var failures = new List<ValidationFailure>();
        var validationContext = new ValidationContext<object>(value);

        foreach (var validator in validators)
        {
            if (validator is not IValidator nonGenericValidator)
                continue;

            var result = await nonGenericValidator.ValidateAsync(validationContext, ct);
            if (!result.IsValid)
                failures.AddRange(result.Errors);
        }

        return failures;
    }

    private static IEnumerable<object> GetNestedValues<TCommand>(TCommand command)
    {
        var properties = ReadablePublicInstancePropertiesCache.GetOrAdd(
            typeof(TCommand),
            requestType =>
                requestType
                    .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
                    .ToArray()
        );

        foreach (var property in properties)
        {
            var propertyType = property.PropertyType;
            if (propertyType == typeof(string) || propertyType.IsValueType)
                continue;

            var value = property.GetValue(command);
            if (value is null)
                continue;

            if (value is IEnumerable enumerable and not string)
            {
                foreach (var item in enumerable)
                {
                    if (item is null)
                        continue;

                    var itemType = item.GetType();
                    if (itemType == typeof(string) || itemType.IsValueType)
                        continue;

                    yield return item;
                }
                continue;
            }

            if (!propertyType.IsValueType && propertyType != typeof(string))
                yield return value;
        }
    }
}
