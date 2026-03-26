using BackgroundJobs.Domain.Entities;
using BackgroundJobs.Domain.Interfaces;
using BackgroundJobs.Infrastructure.Persistence;
using SharedKernel.Infrastructure.Repositories;

namespace BackgroundJobs.Infrastructure.Repositories;

/// <summary>
/// EF Core repository for <see cref="JobExecution"/> entities.
/// </summary>
public sealed class JobExecutionRepository : RepositoryBase<JobExecution>, IJobExecutionRepository
{
    public JobExecutionRepository(BackgroundJobsDbContext dbContext)
        : base(dbContext) { }
}
