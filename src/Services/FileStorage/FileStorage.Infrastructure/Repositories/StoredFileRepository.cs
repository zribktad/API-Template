using FileStorage.Domain.Entities;
using FileStorage.Domain.Interfaces;
using FileStorage.Infrastructure.Persistence;
using SharedKernel.Infrastructure.Repositories;

namespace FileStorage.Infrastructure.Repositories;

/// <summary>EF Core repository for <see cref="StoredFile"/>, inheriting all standard CRUD and specification query support from <see cref="RepositoryBase{T}"/>.</summary>
public sealed class StoredFileRepository : RepositoryBase<StoredFile>, IStoredFileRepository
{
    public StoredFileRepository(FileStorageDbContext dbContext)
        : base(dbContext) { }
}
