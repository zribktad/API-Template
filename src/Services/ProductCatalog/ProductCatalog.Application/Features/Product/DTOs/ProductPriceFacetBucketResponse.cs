namespace ProductCatalog.Application.Features.Product.DTOs;

/// <summary>
/// Human-readable labels for the fixed price bucket ranges used in product search facets.
/// </summary>
public static class PriceBucketLabels
{
    public const string ZeroToFifty = "0 - 50";
    public const string FiftyToOneHundred = "50 - 100";
    public const string OneHundredToTwoHundredFifty = "100 - 250";
    public const string TwoHundredFiftyToFiveHundred = "250 - 500";
    public const string FiveHundredAndAbove = "500+";
}

/// <summary>
/// Represents a single price-range bucket in the product search facets, with a human-readable label and the count of matching products.
/// </summary>
public sealed record ProductPriceFacetBucketResponse(
    string Label,
    decimal MinPrice,
    decimal? MaxPrice,
    int Count
);
