using Notifications.Domain.Entities;
using Notifications.Domain.Interfaces;
using Wolverine.Http;

namespace Notifications.Application.Features.Emails.Endpoints;

/// <summary>
/// Wolverine HTTP endpoint that returns all failed email records for administrative review.
/// </summary>
public static class GetFailedEmailsEndpoint
{
    [WolverineGet("/api/v1/notifications/failed-emails")]
    public static async Task<IReadOnlyList<FailedEmail>> HandleAsync(
        IFailedEmailRepository repository,
        CancellationToken ct
    )
    {
        return await repository.GetAllAsync(ct);
    }
}
