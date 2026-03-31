using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace SharedKernel.Api.OpenApi;

/// <summary>
/// Attaches a shared ProblemDetails schema and standard error responses to operations.
/// </summary>
public sealed class ProblemDetailsOpenApiTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken
    )
    {
        document.Components ??= new OpenApiComponents();
        document.Components.Schemas ??= new Dictionary<string, IOpenApiSchema>();

        IOpenApiSchema problemDetailsSchema = BuildProblemDetailsSchema();
        document.Components.Schemas["ApiProblemDetails"] = problemDetailsSchema;

        foreach (OpenApiPathItem path in document.Paths.Values)
        {
            if (path.Operations is null)
            {
                continue;
            }

            foreach (OpenApiOperation operation in path.Operations.Values)
            {
                int[] errorStatusCodes =
                [
                    StatusCodes.Status400BadRequest,
                    StatusCodes.Status401Unauthorized,
                    StatusCodes.Status403Forbidden,
                    StatusCodes.Status404NotFound,
                    StatusCodes.Status409Conflict,
                    StatusCodes.Status500InternalServerError,
                ];

                foreach (int statusCode in errorStatusCodes)
                {
                    OpenApiErrorResponseHelper.AddErrorResponse(
                        operation,
                        statusCode,
                        problemDetailsSchema
                    );
                }
            }
        }

        return Task.CompletedTask;
    }

    private static IOpenApiSchema BuildProblemDetailsSchema() =>
        new OpenApiSchema
        {
            Type = JsonSchemaType.Object,
            Description = "RFC 7807 ProblemDetails payload used by API error responses.",
            Properties = new Dictionary<string, IOpenApiSchema>
            {
                ["type"] = new OpenApiSchema { Type = JsonSchemaType.String },
                ["title"] = new OpenApiSchema { Type = JsonSchemaType.String },
                ["status"] = new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "int32" },
                ["detail"] = new OpenApiSchema { Type = JsonSchemaType.String },
                ["instance"] = new OpenApiSchema { Type = JsonSchemaType.String },
                ["traceId"] = new OpenApiSchema { Type = JsonSchemaType.String },
                ["errorCode"] = new OpenApiSchema { Type = JsonSchemaType.String },
                ["metadata"] = new OpenApiSchema
                {
                    Type = JsonSchemaType.Object | JsonSchemaType.Null,
                    AdditionalProperties = new OpenApiSchema
                    {
                        Type =
                            JsonSchemaType.String
                            | JsonSchemaType.Integer
                            | JsonSchemaType.Number
                            | JsonSchemaType.Boolean
                            | JsonSchemaType.Null
                            | JsonSchemaType.Object
                            | JsonSchemaType.Array,
                    },
                },
            },
            Required = new HashSet<string> { "type", "title", "status", "traceId", "errorCode" },
        };
}
