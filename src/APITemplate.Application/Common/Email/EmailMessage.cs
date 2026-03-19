namespace APITemplate.Application.Common.Email;

/// <summary>
/// Immutable value object representing a single outbound email queued for delivery.
/// Passed through <see cref="IEmailQueue"/> and consumed by the email-sending background service.
/// </summary>
public sealed record EmailMessage(
    string To,
    string Subject,
    string HtmlBody,
    /// <summary>Optional template name used for logging and dead-letter categorisation.</summary>
    string? TemplateName = null,
    /// <summary>When <c>true</c> the email retry service will attempt redelivery on failure.</summary>
    bool Retryable = false
);
