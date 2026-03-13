namespace APITemplate.Application.Common.Email;

public interface IEmailQueue
{
    ValueTask EnqueueAsync(EmailMessage message, CancellationToken ct = default);
}
