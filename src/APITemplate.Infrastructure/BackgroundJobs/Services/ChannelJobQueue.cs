using APITemplate.Application.Common.BackgroundJobs;

namespace APITemplate.Infrastructure.BackgroundJobs.Services;

public sealed class ChannelJobQueue : BoundedChannelQueue<Guid>, IJobQueue
{
    private const int DefaultCapacity = 100;

    public ChannelJobQueue()
        : base(DefaultCapacity) { }
}
