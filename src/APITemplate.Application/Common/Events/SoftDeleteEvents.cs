using MediatR;

namespace APITemplate.Application.Common.Events;

public sealed record TenantSoftDeletedNotification(
    Guid TenantId,
    Guid ActorId,
    DateTime DeletedAtUtc
) : INotification;
