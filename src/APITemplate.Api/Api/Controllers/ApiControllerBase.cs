using Microsoft.AspNetCore.Mvc;

namespace APITemplate.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
    protected ActionResult<T> OkOrNotFound<T>(T? value)
        where T : class => value is null ? NotFound() : Ok(value);

    protected CreatedAtActionResult CreatedAtGetById<T>(T entity, Guid id) =>
        CreatedAtAction("GetById", new { id, version = this.GetApiVersion() }, entity);

    protected ActionResult<BatchResponse> BatchResult(BatchResponse response) =>
        response.FailureCount > 0 ? UnprocessableEntity(response) : Ok(response);
}
