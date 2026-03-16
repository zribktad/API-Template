namespace APITemplate.Application.Common.Options;

public sealed class ReindexJobOptions
{
    public bool Enabled { get; set; }
    public string Cron { get; set; } = "0 */6 * * *";
}
