namespace APITemplate.Application.Common.BackgroundJobs;

public interface IQueueReader<out T>
{
    IAsyncEnumerable<T> ReadAllAsync(CancellationToken ct = default);
}
