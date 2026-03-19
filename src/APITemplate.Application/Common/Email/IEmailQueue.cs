using APITemplate.Application.Common.BackgroundJobs;

namespace APITemplate.Application.Common.Email;

public interface IEmailQueue : IQueue<EmailMessage>;

public interface IEmailQueueReader : IQueueReader<EmailMessage>;
