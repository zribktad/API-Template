namespace APITemplate.Application.DTOs;

public sealed record ProductReviewFilter(
    Guid? ProductId,
    string? ReviewerName,
    int? MinRating,
    int? MaxRating,
    DateTime? CreatedFrom,
    DateTime? CreatedTo);
