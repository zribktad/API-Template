namespace APITemplate.Application.Common.Options;

public sealed class CleanupJobOptions
{
    public bool Enabled { get; set; }
    public int IntervalMinutes { get; set; } = 60;
    public int ExpiredInvitationRetentionHours { get; set; } = 168;
    public int SoftDeleteRetentionDays { get; set; } = 30;
    public int OrphanedProductDataRetentionDays { get; set; } = 7;
    public int BatchSize { get; set; } = 100;
}
