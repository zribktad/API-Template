using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace SharedKernel.Api.OpenApi;

/// <summary>
/// Adds 401/403 responses for endpoints that require authorization.
/// </summary>
public sealed class AuthorizationResponsesOperationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken
    )
    {
        var endpointMetadata = context.Description.ActionDescriptor.EndpointMetadata;
        bool hasAllowAnonymous = endpointMetadata.OfType<IAllowAnonymous>().Any();
        bool hasAuthorize = endpointMetadata.OfType<IAuthorizeData>().Any();

        if (hasAuthorize && !hasAllowAnonymous)
        {
            OpenApiErrorResponseHelper.AddErrorResponse(
                operation,
                StatusCodes.Status401Unauthorized
            );
            OpenApiErrorResponseHelper.AddErrorResponse(operation, StatusCodes.Status403Forbidden);
        }

        return Task.CompletedTask;
    }
}
