using Microsoft.AspNetCore.Http;
using Notifications.Domain.Entities;
using Notifications.Domain.Interfaces;
using Notifications.Domain.ValueObjects;
using Wolverine.Http;

namespace Notifications.Application.Features.Emails.Endpoints;

/// <summary>
/// Wolverine HTTP endpoint that re-enqueues a failed email for retry delivery.
/// </summary>
public static class RetryFailedEmailEndpoint
{
    [WolverinePost("/api/v1/notifications/failed-emails/{id}/retry")]
    public static async Task<IResult> HandleAsync(
        Guid id,
        IFailedEmailRepository repository,
        IEmailQueue queue,
        CancellationToken ct
    )
    {
        FailedEmail? email = await repository.GetByIdAsync(id, ct);
        if (email is null)
        {
            return Results.NotFound($"Failed email {id} not found");
        }

        email.RetryCount++;
        email.LastAttemptAtUtc = null;
        email.ClaimedBy = null;
        await repository.UpdateAsync(email, ct);
        await repository.SaveChangesAsync(ct);

        await queue.EnqueueAsync(
            new EmailMessage(email.To, email.Subject, email.HtmlBody, email.TemplateName),
            ct
        );

        return Results.Ok();
    }
}
