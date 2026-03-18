namespace APITemplate.Application.Features.Examples.DTOs;

public sealed record SseNotificationItem(int Sequence, string Message, DateTime TimestampUtc);
