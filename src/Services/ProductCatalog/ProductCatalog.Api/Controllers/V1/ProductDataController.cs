using Asp.Versioning;
using ErrorOr;
using Microsoft.AspNetCore.Mvc;
using ProductCatalog.Application.Features.ProductData.Commands;
using ProductCatalog.Application.Features.ProductData.DTOs;
using ProductCatalog.Application.Features.ProductData.Queries;
using SharedKernel.Api.Authorization;
using SharedKernel.Api.Controllers;
using SharedKernel.Api.ErrorOrMapping;
using SharedKernel.Api.Extensions;
using SharedKernel.Application.Security;
using Wolverine;

namespace ProductCatalog.Api.Controllers.V1;

[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/[controller]")]
/// <summary>
/// Presentation-layer controller that manages product supplementary data (images and videos)
/// stored in MongoDB.
/// </summary>
public sealed class ProductDataController(IMessageBus bus) : ApiControllerBase
{
    [HttpGet]
    [RequirePermission(Permission.ProductData.Read)]
    public async Task<ActionResult<List<ProductDataResponse>>> GetAll(
        [FromQuery] string? type,
        CancellationToken ct
    )
    {
        ErrorOr<List<ProductDataResponse>> result = await bus.InvokeAsync<
            ErrorOr<List<ProductDataResponse>>
        >(new GetProductDataQuery(type), ct);
        return result.ToActionResult(this);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(Permission.ProductData.Read)]
    public async Task<ActionResult<ProductDataResponse>> GetById(Guid id, CancellationToken ct)
    {
        ErrorOr<ProductDataResponse> result = await bus.InvokeAsync<ErrorOr<ProductDataResponse>>(
            new GetProductDataByIdQuery(id),
            ct
        );
        return result.ToActionResult(this);
    }

    [HttpPost("image")]
    [RequirePermission(Permission.ProductData.Create)]
    public async Task<ActionResult<ProductDataResponse>> CreateImage(
        CreateImageProductDataRequest request,
        CancellationToken ct
    )
    {
        ErrorOr<ProductDataResponse> result = await bus.InvokeAsync<ErrorOr<ProductDataResponse>>(
            new CreateImageProductDataCommand(request),
            ct
        );
        return result.ToCreatedResult(this, v => new { id = v.Id, version = this.GetApiVersion() });
    }

    [HttpPost("video")]
    [RequirePermission(Permission.ProductData.Create)]
    public async Task<ActionResult<ProductDataResponse>> CreateVideo(
        CreateVideoProductDataRequest request,
        CancellationToken ct
    )
    {
        ErrorOr<ProductDataResponse> result = await bus.InvokeAsync<ErrorOr<ProductDataResponse>>(
            new CreateVideoProductDataCommand(request),
            ct
        );
        return result.ToCreatedResult(this, v => new { id = v.Id, version = this.GetApiVersion() });
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(Permission.ProductData.Delete)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        ErrorOr<Success> result = await bus.InvokeAsync<ErrorOr<Success>>(
            new DeleteProductDataCommand(id),
            ct
        );
        return result.ToNoContentResult(this);
    }
}
