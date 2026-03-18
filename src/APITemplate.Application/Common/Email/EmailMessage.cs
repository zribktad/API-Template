namespace APITemplate.Application.Common.Email;

public sealed record EmailMessage(
    string To,
    string Subject,
    string HtmlBody,
    string? TemplateName = null,
    bool Retryable = false
);
