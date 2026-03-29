using Wolverine;

namespace SharedKernel.Application.Common.Events;

/// <summary>
/// Builds Wolverine cascading <see cref="OutgoingMessages"/> for output-cache eviction
/// (<see cref="CacheInvalidationNotification"/>).
/// </summary>
public static class CacheInvalidationCascades
{
    private static readonly OutgoingMessages EmptyMessages = new();

    /// <summary>No additional cascaded messages.</summary>
    public static OutgoingMessages None => EmptyMessages;

    public static OutgoingMessages ForTag(string cacheTag) =>
        new OutgoingMessages { new CacheInvalidationNotification(cacheTag) };

    public static OutgoingMessages ForTags(params string[] cacheTags)
    {
        OutgoingMessages messages = new();
        foreach (string tag in cacheTags)
            messages.Add(new CacheInvalidationNotification(tag));

        return messages;
    }

    public static OutgoingMessages ForTags(IEnumerable<string> cacheTags)
    {
        OutgoingMessages messages = new();
        foreach (string tag in cacheTags.Distinct(StringComparer.Ordinal))
            messages.Add(new CacheInvalidationNotification(tag));

        return messages;
    }
}
