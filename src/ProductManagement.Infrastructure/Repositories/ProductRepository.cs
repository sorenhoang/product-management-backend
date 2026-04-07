using ProductManagement.Application.Common.Interfaces;
using ProductManagement.Domain.Entities;
using ProductManagement.Domain.Enums;

namespace ProductManagement.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    public Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<IEnumerable<Product>> GetAllAsync(CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task AddAsync(Product entity, CancellationToken ct = default)
        => throw new NotImplementedException();

    public void Update(Product entity)
        => throw new NotImplementedException();

    public void Delete(Product entity)
        => throw new NotImplementedException();

    public Task<(IEnumerable<Product> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? search,
        Guid? categoryId,
        ProductStatus? status,
        decimal? minPrice,
        decimal? maxPrice,
        string? sortBy,
        string? sortOrder,
        CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<bool> SkuExistsAsync(string sku, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<Product?> GetWithVariantsAsync(Guid id, CancellationToken ct = default)
        => throw new NotImplementedException();
}
