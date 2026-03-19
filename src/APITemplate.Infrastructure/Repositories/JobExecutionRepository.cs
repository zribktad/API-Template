using APITemplate.Domain.Entities;
using APITemplate.Domain.Interfaces;
using APITemplate.Infrastructure.Persistence;
using Ardalis.Specification.EntityFrameworkCore;

namespace APITemplate.Infrastructure.Repositories;

public sealed class JobExecutionRepository : RepositoryBase<JobExecution>, IJobExecutionRepository
{
    public JobExecutionRepository(AppDbContext dbContext)
        : base(dbContext) { }
}
