using System.Net.Mime;
using System.Text.Json;
using APITemplate.Application.Common.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace APITemplate.Api.Filters;

public sealed class IdempotencyActionFilter : IAsyncActionFilter
{
    private readonly IIdempotencyStore _store;

    public IdempotencyActionFilter(IIdempotencyStore store)
    {
        _store = store;
    }

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next
    )
    {
        var hasAttribute = context.ActionDescriptor.EndpointMetadata.Any(m =>
            m is IdempotentAttribute
        );

        if (!hasAttribute)
        {
            await next();
            return;
        }

        if (
            !context.HttpContext.Request.Headers.TryGetValue(
                IdempotencyConstants.HeaderName,
                out var keyValues
            ) || string.IsNullOrWhiteSpace(keyValues)
        )
        {
            context.Result = new BadRequestObjectResult(
                "Idempotency-Key header is required for this endpoint."
            );
            return;
        }

        var key = keyValues.ToString();
        if (key.Length > IdempotencyConstants.MaxKeyLength)
        {
            context.Result = new BadRequestObjectResult(
                $"Idempotency key must not exceed {IdempotencyConstants.MaxKeyLength} characters."
            );
            return;
        }

        var resultTtl = TimeSpan.FromHours(IdempotencyConstants.DefaultTtlHours);
        var lockTimeout = TimeSpan.FromSeconds(IdempotencyConstants.LockTimeoutSeconds);
        var ct = context.HttpContext.RequestAborted;

        var existing = await _store.TryGetAsync(key, ct);
        if (existing is not null)
        {
            context.Result = new ContentResult
            {
                StatusCode = existing.StatusCode,
                Content = existing.ResponseBody,
                ContentType = existing.ResponseContentType,
            };
            return;
        }

        if (!await _store.TryAcquireAsync(key, lockTimeout, ct))
        {
            context.Result = new ConflictObjectResult(
                "A request with this idempotency key is already being processed."
            );
            return;
        }

        ActionExecutedContext executedContext;
        try
        {
            executedContext = await next();
        }
        catch
        {
            await _store.ReleaseAsync(key, ct);
            throw;
        }

        if (
            executedContext.Result is ObjectResult objectResult
            && objectResult.StatusCode is >= 200 and < 300
        )
        {
            var responseBody = objectResult.Value is not null
                ? JsonSerializer.Serialize(objectResult.Value)
                : null;

            var entry = new IdempotencyCacheEntry(
                objectResult.StatusCode ?? 200,
                responseBody,
                MediaTypeNames.Application.Json
            );

            await _store.SetAsync(key, entry, resultTtl, ct);
        }

        await _store.ReleaseAsync(key, ct);
    }
}
