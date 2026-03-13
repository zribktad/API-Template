namespace APITemplate.Application.Common.Email;

public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken ct = default);
}
