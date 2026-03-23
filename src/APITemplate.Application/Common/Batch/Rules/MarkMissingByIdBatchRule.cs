namespace APITemplate.Application.Common.Batch.Rules;

internal sealed class MarkMissingByIdBatchRule<TItem>(
    IReadOnlySet<Guid> foundIds,
    string notFoundMessageTemplate
) : IBatchRule<TItem>
    where TItem : IHasId
{
    private readonly IReadOnlySet<Guid> _foundIds = foundIds;
    private readonly string _notFoundMessageTemplate = notFoundMessageTemplate;

    public Task ApplyAsync(BatchFailureContext<TItem> context, CancellationToken ct)
    {
        for (var i = 0; i < context.Items.Count; i++)
        {
            if (context.IsFailed(i))
                continue;

            var id = context.Items[i].Id;
            if (!_foundIds.Contains(id))
                context.AddFailure(i, id, string.Format(_notFoundMessageTemplate, id));
        }

        return Task.CompletedTask;
    }
}
