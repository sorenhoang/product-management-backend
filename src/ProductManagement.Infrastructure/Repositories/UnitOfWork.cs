using ProductManagement.Application.Common.Interfaces;

namespace ProductManagement.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    public IProductRepository Products
        => throw new NotImplementedException();

    public ICategoryRepository Categories
        => throw new NotImplementedException();

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => throw new NotImplementedException();
}
