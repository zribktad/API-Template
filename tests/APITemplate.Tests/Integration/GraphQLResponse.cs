namespace APITemplate.Tests.Integration;

public sealed record GraphQLResponse<TData>(TData? Data, IReadOnlyList<GraphQLError>? Errors);

public sealed record GraphQLError(string Message);
