namespace APITemplate.Application.Common.Email;

public interface IEmailTemplateRenderer
{
    Task<string> RenderAsync(string templateName, object model, CancellationToken ct = default);
}
