using Microsoft.AspNetCore.Mvc;
using SharedKernel.Application.DTOs;

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
}
