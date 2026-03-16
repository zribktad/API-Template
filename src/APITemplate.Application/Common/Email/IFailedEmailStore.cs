namespace APITemplate.Application.Common.Email;

public interface IFailedEmailStore
{
    Task StoreFailedAsync(EmailMessage message, string error, CancellationToken ct = default);
}
