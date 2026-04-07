using ProductManagement.Domain.Entities;
using ProductManagement.Domain.Enums;

namespace ProductManagement.Application.Common.Interfaces;

public interface IProductRepository : IRepository<Product>
{
    Task<(IEnumerable<Product> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? search,
        Guid? categoryId,
        ProductStatus? status,
        decimal? minPrice,
        decimal? maxPrice,
        string? sortBy,
        string? sortOrder,
        CancellationToken ct = default);

    Task<bool> SkuExistsAsync(string sku, CancellationToken ct = default);
    Task<Product?> GetWithVariantsAsync(Guid id, CancellationToken ct = default);
}
