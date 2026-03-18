namespace APITemplate.Application.Common.BackgroundJobs;

public interface IJobQueue : IQueue<Guid>;

public interface IJobQueueReader : IQueueReader<Guid>;
