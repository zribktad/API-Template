namespace APITemplate.Application.Common.BackgroundJobs;

public interface IQueue<in T>
{
    ValueTask EnqueueAsync(T item, CancellationToken ct = default);
}
