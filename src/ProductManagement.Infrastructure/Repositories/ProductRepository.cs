using Microsoft.EntityFrameworkCore;
using ProductManagement.Application.Common.Interfaces;
using ProductManagement.Domain.Entities;
using ProductManagement.Domain.Enums;
using ProductManagement.Infrastructure.Persistence;

namespace ProductManagement.Infrastructure.Repositories;

public class ProductRepository(AppDbContext context) : IProductRepository
{
    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await context.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<Product?> GetWithVariantsAsync(Guid id, CancellationToken ct = default)
        => await context.Products
            .Include(p => p.Category)
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IEnumerable<Product>> GetAllAsync(CancellationToken ct = default)
        => await context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .ToListAsync(ct);

    public async Task<(IEnumerable<Product> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string?        search,
        Guid?          categoryId,
        ProductStatus? status,
        decimal?       minPrice,
        decimal?       maxPrice,
        string?        sortBy,
        string?        sortOrder,
        CancellationToken ct = default)
    {
        var query = context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Variants)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p =>
                p.Name.Contains(search) ||
                (p.Description != null && p.Description.Contains(search)));

        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId.Value);

        if (status.HasValue)
            query = query.Where(p => p.Status == status.Value);

        if (minPrice.HasValue)
            query = query.Where(p => p.BasePrice >= minPrice.Value);

        if (maxPrice.HasValue)
            query = query.Where(p => p.BasePrice <= maxPrice.Value);

        var totalCount = await query.CountAsync(ct);

        query = sortBy?.ToLowerInvariant() switch
        {
            "name"      => sortOrder == "asc" ? query.OrderBy(p => p.Name)      : query.OrderByDescending(p => p.Name),
            "baseprice" => sortOrder == "asc" ? query.OrderBy(p => p.BasePrice) : query.OrderByDescending(p => p.BasePrice),
            "updatedat" => sortOrder == "asc" ? query.OrderBy(p => p.UpdatedAt) : query.OrderByDescending(p => p.UpdatedAt),
            _           => sortOrder == "asc" ? query.OrderBy(p => p.CreatedAt) : query.OrderByDescending(p => p.CreatedAt),
        };

        query = query.Skip((page - 1) * pageSize).Take(pageSize);

        var items = await query.ToListAsync(ct);
        return (items, totalCount);
    }

    public async Task<bool> SkuExistsAsync(string sku, CancellationToken ct = default)
        => await context.ProductVariants.AnyAsync(v => v.Sku == sku, ct);

    public async Task AddAsync(Product entity, CancellationToken ct = default)
        => await context.Products.AddAsync(entity, ct);

    public void Update(Product entity)
        => context.Products.Update(entity);

    public void Delete(Product entity)
        => context.Products.Remove(entity);
}
