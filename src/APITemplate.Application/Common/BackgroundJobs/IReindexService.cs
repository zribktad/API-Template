namespace APITemplate.Application.Common.BackgroundJobs;

public interface IReindexService
{
    Task ReindexFullTextSearchAsync(CancellationToken ct = default);
}
