using APITemplate.Api.Authorization;
using APITemplate.Api.Controllers;
using APITemplate.Application.Common.Security;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using SharedKernel.Application.Common.Events;
using Wolverine;

namespace APITemplate.Api.Controllers.V1;

[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/product-data")]
/// <summary>
/// Presentation-layer controller that manages product supplementary data (images and videos)
/// stored in MongoDB, with output-cache integration for read endpoints.
/// </summary>
public sealed class ProductDataController(IMessageBus bus) : ApiControllerBase
{
    [HttpGet]
    [RequirePermission(Permission.ProductData.Read)]
    [OutputCache(PolicyName = CacheTags.ProductData)]
    public Task<ActionResult<List<ProductDataResponse>>> GetAll(
        [FromQuery] string? type,
        CancellationToken ct
    ) =>
        InvokeToActionResultAsync<List<ProductDataResponse>>(
            bus,
            new GetProductDataQuery(type),
            ct
        );

    [HttpGet("{id:guid}")]
    [RequirePermission(Permission.ProductData.Read)]
    [OutputCache(PolicyName = CacheTags.ProductData)]
    public Task<ActionResult<ProductDataResponse>> GetById(Guid id, CancellationToken ct) =>
        InvokeToActionResultAsync<ProductDataResponse>(bus, new GetProductDataByIdQuery(id), ct);

    [HttpPost("image")]
    [RequirePermission(Permission.ProductData.Create)]
    public Task<ActionResult<ProductDataResponse>> CreateImage(
        CreateImageProductDataRequest request,
        CancellationToken ct
    ) =>
        InvokeToCreatedResultAsync<ProductDataResponse>(
            bus,
            new CreateImageProductDataCommand(request),
            v => new { id = v.Id, version = this.GetApiVersion() },
            ct
        );

    [HttpPost("video")]
    [RequirePermission(Permission.ProductData.Create)]
    public Task<ActionResult<ProductDataResponse>> CreateVideo(
        CreateVideoProductDataRequest request,
        CancellationToken ct
    ) =>
        InvokeToCreatedResultAsync<ProductDataResponse>(
            bus,
            new CreateVideoProductDataCommand(request),
            v => new { id = v.Id, version = this.GetApiVersion() },
            ct
        );

    [HttpDelete("{id:guid}")]
    [RequirePermission(Permission.ProductData.Delete)]
    public Task<IActionResult> Delete(Guid id, CancellationToken ct) =>
        InvokeToNoContentResultAsync(bus, new DeleteProductDataCommand(id), ct);
}
