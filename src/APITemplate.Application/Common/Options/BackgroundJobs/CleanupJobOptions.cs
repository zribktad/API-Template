namespace APITemplate.Application.Common.Options.BackgroundJobs;

public sealed class CleanupJobOptions
{
    public bool Enabled { get; set; }
    public string Cron { get; set; } = "0 * * * *";
    public int ExpiredInvitationRetentionHours { get; set; } = 168;
    public int SoftDeleteRetentionDays { get; set; } = 30;
    public int OrphanedProductDataRetentionDays { get; set; } = 7;
    public int BatchSize { get; set; } = 100;
}
