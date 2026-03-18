using APITemplate.Application.Common.Email;
using APITemplate.Infrastructure.BackgroundJobs.Services;

namespace APITemplate.Infrastructure.Email;

public sealed class ChannelEmailQueue : BoundedChannelQueue<EmailMessage>, IEmailQueue
{
    private const int DefaultCapacity = 1000;

    public ChannelEmailQueue()
        : base(DefaultCapacity) { }
}
