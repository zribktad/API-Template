using APITemplate.Api.ErrorOrMapping;
using ErrorOr;
using Microsoft.AspNetCore.Mvc;
using Wolverine;

namespace APITemplate.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
    internal ActionResult<BatchResponse> OkOrUnprocessable(BatchResponse response) =>
        response.FailureCount > 0 ? UnprocessableEntity(response) : Ok(response);

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
