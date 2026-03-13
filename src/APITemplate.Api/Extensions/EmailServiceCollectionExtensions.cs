using APITemplate.Application.Common.Email;
using APITemplate.Application.Common.Options;
using APITemplate.Application.Common.Resilience;
using APITemplate.Infrastructure.Email;
using APITemplate.Infrastructure.Security;
using Polly;

namespace APITemplate.Extensions;

public static class EmailServiceCollectionExtensions
{
    public static IServiceCollection AddEmailServices(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var emailOptions =
            configuration.GetSection("Email").Get<EmailOptions>() ?? new EmailOptions();
        services.Configure<EmailOptions>(configuration.GetSection("Email"));

        var queue = new ChannelEmailQueue();
        services.AddSingleton(queue);
        services.AddSingleton<IEmailQueue>(queue);
        services.AddSingleton<IEmailTemplateRenderer, FluidEmailTemplateRenderer>();
        services.AddSingleton<ISecureTokenGenerator, SecureTokenGenerator>();
        services.AddTransient<IEmailSender, MailKitEmailSender>();

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

        services.AddHostedService<EmailSendingBackgroundService>();

        return services;
    }
}
