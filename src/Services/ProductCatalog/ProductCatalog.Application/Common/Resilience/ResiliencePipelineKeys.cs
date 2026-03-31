namespace ProductCatalog.Application.Common.Resilience;

/// <summary>
/// String constants that identify the named Polly resilience pipelines registered in the Product Catalog service.
/// </summary>
public static class ResiliencePipelineKeys
{
    public const string MongoProductDataDelete = "mongo-productdata-delete";
}
