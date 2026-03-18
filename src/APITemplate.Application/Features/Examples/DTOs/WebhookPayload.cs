using System.Text.Json;

namespace APITemplate.Application.Features.Examples.DTOs;

public sealed record WebhookPayload(string EventType, string EventId, JsonElement Data);
