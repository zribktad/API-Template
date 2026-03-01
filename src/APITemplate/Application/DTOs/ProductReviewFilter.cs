namespace APITemplate.Application.DTOs;

public sealed record ProductReviewFilter(
    Guid? ProductId = null,
    string? ReviewerName = null,
    int? MinRating = null,
    int? MaxRating = null,
    DateTime? CreatedFrom = null,
    DateTime? CreatedTo = null);
