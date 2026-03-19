using APITemplate.Api.GraphQL.Instrumentation;

namespace APITemplate.Api.Extensions;

/// <summary>
/// Presentation-layer extension class that configures the Hot Chocolate GraphQL server,
/// registering query/mutation types, object type mappings, data loaders, authorization,
/// instrumentation, and paging/depth-limit rules.
/// </summary>
public static class GraphQLServiceCollectionExtensions
{
    /// <summary>
    /// Adds the GraphQL server with product and review query/mutation types, object type
    /// configurations, the batch data loader, metrics listener, and a max execution depth of 5.
    /// </summary>
    public static IServiceCollection AddGraphQLConfiguration(this IServiceCollection services)
    {
        services.AddSingleton<GraphQlExecutionMetricsListener>();

        services
            .AddGraphQLServer()
            .AddQueryType<Api.GraphQL.Queries.ProductQueries>()
            .AddTypeExtension<Api.GraphQL.Queries.CategoryQueries>()
            .AddTypeExtension<Api.GraphQL.Queries.ProductReviewQueries>()
            .AddMutationType<Api.GraphQL.Mutations.ProductMutations>()
            .AddTypeExtension<Api.GraphQL.Mutations.ProductReviewMutations>()
            .AddType<Api.GraphQL.Types.ProductType>()
            .AddType<Api.GraphQL.Types.ProductReviewType>()
            .AddDataLoader<Api.GraphQL.DataLoaders.ProductReviewsByProductDataLoader>()
            .AddAuthorization()
            .AddInstrumentation()
            .AddDiagnosticEventListener(sp =>
                sp.GetRequiredService<GraphQlExecutionMetricsListener>()
            )
            .ModifyPagingOptions(o =>
            {
                o.MaxPageSize = PaginationFilter.MaxPageSize;
                o.DefaultPageSize = PaginationFilter.DefaultPageSize;
                o.IncludeTotalCount = true;
            })
            .AddMaxExecutionDepthRule(5);

        return services;
    }
}
