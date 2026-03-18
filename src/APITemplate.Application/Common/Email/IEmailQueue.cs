using APITemplate.Application.Common.BackgroundJobs;

namespace APITemplate.Application.Common.Email;

public interface IEmailQueue
{
    ValueTask EnqueueAsync(EmailMessage message, CancellationToken ct = default);
}

public interface IEmailQueueReader : IQueueReader<EmailMessage>;
