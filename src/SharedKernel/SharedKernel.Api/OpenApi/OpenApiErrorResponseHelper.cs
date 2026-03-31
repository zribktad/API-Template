using Microsoft.AspNetCore.WebUtilities;
using Microsoft.OpenApi;

namespace SharedKernel.Api.OpenApi;

/// <summary>
/// Adds RFC 7807 <c>application/problem+json</c> response metadata to OpenAPI operations.
/// </summary>
internal static class OpenApiErrorResponseHelper
{
    internal static void AddErrorResponse(
        OpenApiOperation operation,
        int statusCode,
        IOpenApiSchema? schema = null,
        string? description = null
    )
    {
        string statusCodeKey = statusCode.ToString();
        operation.Responses ??= new OpenApiResponses();
        if (operation.Responses.ContainsKey(statusCodeKey))
        {
            return;
        }

        string resolvedDescription = string.IsNullOrWhiteSpace(description)
            ? ReasonPhrases.GetReasonPhrase(statusCode)
            : description;

        operation.Responses[statusCodeKey] = new OpenApiResponse
        {
            Description = resolvedDescription,
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["application/problem+json"] = new OpenApiMediaType { Schema = schema },
            },
        };
    }
}
