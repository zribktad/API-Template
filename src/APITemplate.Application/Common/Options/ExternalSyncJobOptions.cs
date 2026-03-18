namespace APITemplate.Application.Common.Options;

public sealed class ExternalSyncJobOptions
{
    public bool Enabled { get; set; }
    public string Cron { get; set; } = "0 */12 * * *";
}
