namespace APITemplate.Application.Common.Options.BackgroundJobs;

public sealed class ReindexJobOptions
{
    public bool Enabled { get; set; }
    public string Cron { get; set; } = "0 */6 * * *";
}
