using Wolverine;

namespace SharedKernel.Application.Common.Events;

/// <summary>
/// Builds Wolverine cascading <see cref="OutgoingMessages"/> for output-cache eviction
/// (<see cref="CacheInvalidationNotification"/>).
/// </summary>
public static class CacheInvalidationCascades
{
    /// <summary>No additional cascaded messages.</summary>
    public static OutgoingMessages None => new();

    public static OutgoingMessages ForTag(string cacheTag) =>
        new OutgoingMessages { new CacheInvalidationNotification(cacheTag) };

    public static OutgoingMessages ForTags(params string[] cacheTags) =>
        ForTags((IEnumerable<string>)cacheTags);

    public static OutgoingMessages ForTags(IEnumerable<string> cacheTags)
    {
        OutgoingMessages messages = new();
        foreach (string tag in cacheTags.Distinct(StringComparer.Ordinal))
            messages.Add(new CacheInvalidationNotification(tag));

        return messages;
    }
}
