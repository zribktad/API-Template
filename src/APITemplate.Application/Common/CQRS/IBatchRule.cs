namespace APITemplate.Application.Common.CQRS;

internal interface IBatchRule<TItem>
{
    Task ApplyAsync(BatchFailureContext<TItem> context, CancellationToken ct);
}
