using System.Net.Mime;
using System.Text;
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
            await next();
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

        var ttl = TimeSpan.FromHours(IdempotencyConstants.DefaultTtlHours);
        var ct = context.HttpContext.RequestAborted;

        var existing = await _store.TryGetAsync(key, ct);
        if (existing is not null)
        {
            context.HttpContext.Response.StatusCode = existing.StatusCode;
            if (existing.ResponseContentType is not null)
                context.HttpContext.Response.ContentType = existing.ResponseContentType;
            if (existing.ResponseBody is not null)
                await context.HttpContext.Response.WriteAsync(
                    existing.ResponseBody,
                    Encoding.UTF8,
                    ct
                );
            return;
        }

        if (!await _store.TryAcquireAsync(key, ttl, ct))
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status409Conflict;
            return;
        }

        var executedContext = await next();

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

            await _store.SetAsync(key, entry, ttl, ct);
        }
    }
}
