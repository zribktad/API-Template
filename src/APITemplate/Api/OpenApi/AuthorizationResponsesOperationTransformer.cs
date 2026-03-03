using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.OpenApi;

namespace APITemplate.Api.OpenApi;

/// <summary>
/// Adds 401/403 responses only for operations that require authorization metadata.
/// This avoids brittle path-based heuristics.
/// </summary>
public sealed class AuthorizationResponsesOperationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        var endpointMetadata = context.Description.ActionDescriptor.EndpointMetadata;
        var hasAllowAnonymous = endpointMetadata.OfType<IAllowAnonymous>().Any();
        var hasAuthorize = endpointMetadata.OfType<IAuthorizeData>().Any();

        if (hasAuthorize && !hasAllowAnonymous)
        {
            AddErrorResponse(operation, StatusCodes.Status401Unauthorized);
            AddErrorResponse(operation, StatusCodes.Status403Forbidden);
        }

        return Task.CompletedTask;
    }

    private static void AddErrorResponse(OpenApiOperation operation, int statusCode, string? description = null)
    {
        var statusCodeKey = statusCode.ToString();
        operation.Responses ??= new OpenApiResponses();
        if (operation.Responses.ContainsKey(statusCodeKey))
            return;

        var resolvedDescription = string.IsNullOrWhiteSpace(description)
            ? ReasonPhrases.GetReasonPhrase(statusCode)
            : description;

        operation.Responses[statusCodeKey] = new OpenApiResponse
        {
            Description = resolvedDescription,
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["application/problem+json"] = new OpenApiMediaType
                {
                    Schema = new OpenApiSchemaReference("ApiProblemDetails", null)
                }
            }
        };
    }
}
