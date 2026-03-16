namespace APITemplate.Application.Common.Options;

public sealed class ReindexJobOptions
{
    public bool Enabled { get; set; }
    public int IntervalMinutes { get; set; } = 360;
}
