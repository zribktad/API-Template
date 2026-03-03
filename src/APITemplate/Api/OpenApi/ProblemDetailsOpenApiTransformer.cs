using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.OpenApi;

namespace APITemplate.Api.OpenApi;

/// <summary>
/// Improved alternative to repeating <c>[ProducesResponseType(typeof(ProblemDetails), 400/404/500)]</c>
/// on every controller action. This transformer adds ProblemDetails responses globally
/// and avoids duplication across individual controllers.
/// </summary>
public sealed class ProblemDetailsOpenApiTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.Schemas ??= new Dictionary<string, IOpenApiSchema>();

        document.Components.Schemas["ApiProblemDetails"] = BuildProblemDetailsSchema();

        foreach (var pathEntry in document.Paths)
        {
            var path = pathEntry.Value;
            if (path.Operations is null)
                continue;

            foreach (var operation in path.Operations.Values)
            {
                AddErrorResponse(operation, StatusCodes.Status400BadRequest);
                AddErrorResponse(operation, StatusCodes.Status404NotFound);
                AddErrorResponse(operation, StatusCodes.Status500InternalServerError);
            }
        }

        return Task.CompletedTask;
    }

    private static IOpenApiSchema BuildProblemDetailsSchema()
    {
        return new OpenApiSchema
        {
            Type = JsonSchemaType.Object,
            Description = "Standard RFC 7807 ProblemDetails payload used by REST error responses.",
            Properties = new Dictionary<string, IOpenApiSchema>
            {
                ["type"] = new OpenApiSchema { Type = JsonSchemaType.String, Description = "Error documentation URI." },
                ["title"] = new OpenApiSchema { Type = JsonSchemaType.String },
                ["status"] = new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "int32" },
                ["detail"] = new OpenApiSchema { Type = JsonSchemaType.String },
                ["instance"] = new OpenApiSchema { Type = JsonSchemaType.String },
                ["traceId"] = new OpenApiSchema { Type = JsonSchemaType.String },
                ["errorCode"] = new OpenApiSchema { Type = JsonSchemaType.String },
                ["metadata"] = new OpenApiSchema
                {
                    Type = JsonSchemaType.Object | JsonSchemaType.Null,
                    AdditionalProperties = new OpenApiSchema { Type = JsonSchemaType.String | JsonSchemaType.Integer | JsonSchemaType.Number | JsonSchemaType.Boolean | JsonSchemaType.Null }
                }
            },
            Required = new HashSet<string> { "type", "title", "status", "detail", "traceId", "errorCode" }
        };
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
