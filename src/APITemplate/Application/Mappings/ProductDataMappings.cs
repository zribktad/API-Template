using APITemplate.Application.DTOs.Responses;
using APITemplate.Domain.Entities;

namespace APITemplate.Application.Mappings;

public static class ProductDataMappings
{
    public static ProductDataResponse ToResponse(this ProductData data) =>
        data switch
        {
            ImageProductData image => new ProductDataResponse
            {
                Id = image.Id,
                Type = "image",
                Title = image.Title,
                Description = image.Description,
                CreatedAt = image.CreatedAt,
                Width = image.Width,
                Height = image.Height,
                Format = image.Format,
                FileSizeBytes = image.FileSizeBytes
            },
            VideoProductData video => new ProductDataResponse
            {
                Id = video.Id,
                Type = "video",
                Title = video.Title,
                Description = video.Description,
                CreatedAt = video.CreatedAt,
                DurationSeconds = video.DurationSeconds,
                Resolution = video.Resolution,
                Format = video.Format,
                FileSizeBytes = video.FileSizeBytes
            },
            _ => throw new InvalidOperationException($"Unknown ProductData type: {data.GetType().Name}")
        };
}
