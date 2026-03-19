using APITemplate.Application.Common.Email;
using APITemplate.Application.Common.Options;
using APITemplate.Application.Common.Resilience;
using APITemplate.Infrastructure.Email;
using APITemplate.Infrastructure.Security;
using Polly;

namespace APITemplate.Api.Extensions;

public static class EmailServiceCollectionExtensions
{
    public static IServiceCollection AddEmailServices(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var emailSection = configuration.SectionFor<EmailOptions>();
        var emailOptions = emailSection.Get<EmailOptions>() ?? new EmailOptions();
        services.Configure<EmailOptions>(emailSection);

        services.AddQueueWithConsumer<
            ChannelEmailQueue,
            IEmailQueue,
            IEmailQueueReader,
            EmailSendingBackgroundService
        >();
        services.AddSingleton<IEmailTemplateRenderer, FluidEmailTemplateRenderer>();
        services.AddSingleton<ISecureTokenGenerator, SecureTokenGenerator>();
        services.AddTransient<IEmailSender, MailKitEmailSender>();

        services.AddSingleton<IFailedEmailStore, FailedEmailStore>();

        services.AddResiliencePipeline(
            ResiliencePipelineKeys.SmtpSend,
            builder =>
            {
                builder.AddRetry(
                    new()
                    {
                        MaxRetryAttempts = emailOptions.MaxRetryAttempts,
                        BackoffType = DelayBackoffType.Exponential,
                        Delay = TimeSpan.FromSeconds(emailOptions.RetryBaseDelaySeconds),
                        UseJitter = true,
                    }
                );
            }
        );

        return services;
    }
}
