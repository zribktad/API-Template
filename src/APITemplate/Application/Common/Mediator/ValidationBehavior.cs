using APITemplate.Application.Common.Errors;
using APITemplate.Domain.Exceptions;
using FluentValidation;
using MediatR;

namespace APITemplate.Application.Common.Mediator;

public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);
        var results = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken)));
        var failures = results
            .SelectMany(r => r.Errors)
            .Where(e => e is not null)
            .ToList();

        if (failures.Count == 0)
            return await next();

        var groupedErrors = failures
            .GroupBy(f => string.IsNullOrWhiteSpace(f.PropertyName) ? "request" : f.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(f => f.ErrorMessage).Distinct().ToArray());

        var message = string.Join("; ", groupedErrors.Select(kvp => $"{kvp.Key}: {string.Join(", ", kvp.Value)}"));

        throw new APITemplate.Domain.Exceptions.ValidationException(
            message,
            ErrorCatalog.General.ValidationFailed,
            new Dictionary<string, object?>
            {
                ["errors"] = groupedErrors
            });
    }
}
