namespace APITemplate.Application.Common.CQRS.Rules;

internal sealed class MarkMissingIdsBatchRule(
    IReadOnlySet<Guid> foundIds,
    string notFoundMessageTemplate
) : IBatchRule<Guid>
{
    private readonly IReadOnlySet<Guid> _foundIds = foundIds;
    private readonly string _notFoundMessageTemplate = notFoundMessageTemplate;

    public Task ApplyAsync(BatchFailureContext<Guid> context, CancellationToken ct)
    {
        for (var i = 0; i < context.Items.Count; i++)
        {
            if (context.IsFailed(i))
                continue;

            var id = context.Items[i];
            if (!_foundIds.Contains(id))
                context.AddFailure(i, id, string.Format(_notFoundMessageTemplate, id));
        }

        return Task.CompletedTask;
    }
}
