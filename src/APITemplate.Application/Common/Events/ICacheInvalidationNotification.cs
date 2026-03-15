using MediatR;

namespace APITemplate.Application.Common.Events;

public interface ICacheInvalidationNotification : INotification
{
    string CacheTag { get; }
}
