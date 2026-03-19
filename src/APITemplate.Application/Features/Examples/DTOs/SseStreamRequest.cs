using System.ComponentModel.DataAnnotations;

namespace APITemplate.Application.Features.Examples.DTOs;

public sealed class SseStreamRequest
{
    [Range(1, 100)]
    public int Count { get; init; } = 5;
}
