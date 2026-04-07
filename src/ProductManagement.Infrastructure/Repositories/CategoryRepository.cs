using ProductManagement.Application.Common.Interfaces;
using ProductManagement.Domain.Entities;

namespace ProductManagement.Infrastructure.Repositories;

public class CategoryRepository : ICategoryRepository
{
    public Task<Category?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<IEnumerable<Category>> GetAllAsync(CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task AddAsync(Category entity, CancellationToken ct = default)
        => throw new NotImplementedException();

    public void Update(Category entity)
        => throw new NotImplementedException();

    public void Delete(Category entity)
        => throw new NotImplementedException();

    public Task<IEnumerable<Category>> GetTreeAsync(CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
        => throw new NotImplementedException();
}
