using APITemplate.Domain.Entities;
using APITemplate.Domain.Interfaces;
using APITemplate.Infrastructure.Persistence;
using Ardalis.Specification.EntityFrameworkCore;

namespace APITemplate.Infrastructure.Repositories;

public sealed class StoredFileRepository : RepositoryBase<StoredFile>, IStoredFileRepository
{
    public StoredFileRepository(AppDbContext dbContext)
        : base(dbContext) { }
}
