namespace BackgroundJobs.Infrastructure.TickerQ;

/// <summary>
/// Stable GUIDs that uniquely identify each recurring TickerQ job in the scheduler database.
/// These values must never change once the jobs have been seeded.
/// </summary>
internal static class TickerQJobIds
{
    public static readonly Guid Cleanup = new("4bc6790c-c877-43ed-8a32-85d5fa2dad95");
    public static readonly Guid Reindex = new("9cf4e6ef-a2dd-4ff7-8968-174a6236a59f");
}
