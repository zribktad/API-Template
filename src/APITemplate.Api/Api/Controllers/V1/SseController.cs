using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using APITemplate.Api.Authorization;
using APITemplate.Application.Features.Examples.DTOs;
using APITemplate.Application.Features.Examples.Handlers;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace APITemplate.Api.Controllers.V1;

[ApiVersion(1.0)]
[ApiController]
[Route("api/v{version:apiVersion}/sse")]
public sealed class SseController : ControllerBase
{
    private const string EventStreamContentType = "text/event-stream";
    private const string NoCacheDirective = "no-cache";
    private const string KeepAliveConnection = "keep-alive";
    private const string SseDataPrefix = "data: ";

    private readonly ISender _sender;

    public SseController(ISender sender) => _sender = sender;

    [HttpGet("stream")]
    [RequirePermission(Permission.Examples.Read)]
    public async Task Stream([FromQuery] SseStreamRequest request, CancellationToken ct = default)
    {
        Response.ContentType = EventStreamContentType;
        Response.Headers.CacheControl = NoCacheDirective;
        Response.Headers.Connection = KeepAliveConnection;

        var stream = await _sender.Send(new GetNotificationStreamQuery(request), ct);
        await using var writer = new StreamWriter(Response.Body, leaveOpen: true);

        await foreach (var item in stream.WithCancellation(ct))
        {
            var json = JsonSerializer.Serialize(item, JsonSerializerOptions.Web);
            await writer.WriteAsync($"{SseDataPrefix}{json}\n\n");
            await writer.FlushAsync(ct);
        }
    }
}
