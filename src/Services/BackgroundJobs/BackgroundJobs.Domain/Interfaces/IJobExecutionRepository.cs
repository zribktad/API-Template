using BackgroundJobs.Domain.Entities;
using SharedKernel.Domain.Interfaces;

namespace BackgroundJobs.Domain.Interfaces;

/// <summary>
/// Repository contract for <see cref="JobExecution"/> entities, inheriting all generic CRUD operations from <see cref="IRepository{T}"/>.
/// </summary>
public interface IJobExecutionRepository : IRepository<JobExecution>;
