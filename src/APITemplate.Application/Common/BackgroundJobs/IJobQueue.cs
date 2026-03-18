namespace APITemplate.Application.Common.BackgroundJobs;

public interface IJobQueue
{
    ValueTask EnqueueAsync(Guid jobId, CancellationToken ct = default);
}

public interface IJobQueueReader : IQueueReader<Guid>;
