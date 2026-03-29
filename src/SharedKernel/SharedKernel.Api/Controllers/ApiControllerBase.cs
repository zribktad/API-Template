using ErrorOr;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Api.ErrorOrMapping;
using SharedKernel.Application.DTOs;
using Wolverine;

namespace SharedKernel.Api.Controllers;

/// <summary>
/// Base controller for all API controllers, providing shared route conventions
/// and common response helpers.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
    internal ActionResult<BatchResponse> OkOrUnprocessable(BatchResponse response) =>
        response.FailureCount > 0 ? UnprocessableEntity(response) : Ok(response);

    /// <summary>Invokes a Wolverine message and maps <see cref="ErrorOr{T}"/> to an HTTP result.</summary>
    /// <remarks>
    /// Uses a single <typeparamref name="TResponse"/> type argument so callers are not forced to specify
    /// both response and message types (C# does not partially infer method type parameters).
    /// </remarks>
    protected async Task<ActionResult<TResponse>> InvokeToActionResultAsync<TResponse>(
        IMessageBus bus,
        object message,
        CancellationToken cancellationToken
    )
    {
        ErrorOr<TResponse> result = await bus.InvokeAsync<ErrorOr<TResponse>>(
            message,
            cancellationToken
        );
        return result.ToActionResult(this);
    }

    /// <summary>Invokes a batch command/query and maps the result via <see cref="ErrorOrExtensions.ToBatchResult"/>.</summary>
    protected async Task<ActionResult<BatchResponse>> InvokeToBatchResultAsync<TMessage>(
        IMessageBus bus,
        TMessage message,
        CancellationToken cancellationToken
    )
    {
        ErrorOr<BatchResponse> result = await bus.InvokeAsync<ErrorOr<BatchResponse>>(
            message!,
            cancellationToken
        );
        return result.ToBatchResult(this);
    }

    /// <summary>Invokes a command that returns <see cref="Success"/> and maps to 204 No Content.</summary>
    protected async Task<IActionResult> InvokeToNoContentResultAsync<TMessage>(
        IMessageBus bus,
        TMessage message,
        CancellationToken cancellationToken
    )
    {
        ErrorOr<Success> result = await bus.InvokeAsync<ErrorOr<Success>>(
            message!,
            cancellationToken
        );
        return result.ToNoContentResult(this);
    }

    /// <summary>Invokes a command that returns <see cref="Success"/> and maps to 200 OK with no body.</summary>
    protected async Task<IActionResult> InvokeToOkResultAsync<TMessage>(
        IMessageBus bus,
        TMessage message,
        CancellationToken cancellationToken
    )
    {
        ErrorOr<Success> result = await bus.InvokeAsync<ErrorOr<Success>>(
            message!,
            cancellationToken
        );
        return result.ToOkResult(this);
    }

    /// <summary>Invokes a command and maps success to 201 Created at <c>GetById</c>.</summary>
    protected async Task<ActionResult<TResponse>> InvokeToCreatedResultAsync<TResponse>(
        IMessageBus bus,
        object message,
        Func<TResponse, object> routeValuesFactory,
        CancellationToken cancellationToken
    )
    {
        ErrorOr<TResponse> result = await bus.InvokeAsync<ErrorOr<TResponse>>(
            message,
            cancellationToken
        );
        return result.ToCreatedResult(this, routeValuesFactory);
    }
}
