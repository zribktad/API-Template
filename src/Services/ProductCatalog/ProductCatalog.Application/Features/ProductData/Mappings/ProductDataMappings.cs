using ProductCatalog.Application.Features.ProductData.DTOs;
using ImageProductDataEntity = ProductCatalog.Domain.Entities.ProductData.ImageProductData;
using ProductDataEntity = ProductCatalog.Domain.Entities.ProductData.ProductData;
using VideoProductDataEntity = ProductCatalog.Domain.Entities.ProductData.VideoProductData;

namespace ProductCatalog.Application.Features.ProductData.Mappings;

/// <summary>
/// Provides mapping utilities from product data domain entities to their polymorphic response DTOs.
/// </summary>
public static class ProductDataMappings
{
    /// <summary>
    /// Maps a <see cref="ProductDataEntity"/> to the appropriate <see cref="ProductDataResponse"/> subtype.
    /// </summary>
    public static ProductDataResponse ToResponse(this ProductDataEntity data) =>
        data switch
        {
            ImageProductDataEntity image => image.ToImageResponse(),
            VideoProductDataEntity video => video.ToVideoResponse(),
            _ => throw new InvalidOperationException(
                $"Unknown ProductData type: {data.GetType().Name}"
            ),
        };

    private static T MapCommon<T>(this ProductDataEntity data, T response, string type)
        where T : ProductDataResponse =>
        response with
        {
            Id = data.Id,
            Title = data.Title,
            Description = data.Description,
            CreatedAt = data.CreatedAt,
            Type = type,
        };

    private static ImageProductDataResponse ToImageResponse(this ImageProductDataEntity image) =>
        image.MapCommon(
            new ImageProductDataResponse
            {
                Width = image.Width,
                Height = image.Height,
                Format = image.Format,
                FileSizeBytes = image.FileSizeBytes,
            },
            "image"
        );

    private static VideoProductDataResponse ToVideoResponse(this VideoProductDataEntity video) =>
        video.MapCommon(
            new VideoProductDataResponse
            {
                DurationSeconds = video.DurationSeconds,
                Resolution = video.Resolution,
                Format = video.Format,
                FileSizeBytes = video.FileSizeBytes,
            },
            "video"
        );
}
