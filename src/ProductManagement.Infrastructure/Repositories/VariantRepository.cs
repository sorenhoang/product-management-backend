using Microsoft.EntityFrameworkCore;
using ProductManagement.Application.Common.Interfaces;
using ProductManagement.Domain.Entities;
using ProductManagement.Infrastructure.Persistence;

namespace ProductManagement.Infrastructure.Repositories;

public class VariantRepository(AppDbContext context) : IVariantRepository
{
    public async Task<ProductVariant?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await context.ProductVariants
            .FirstOrDefaultAsync(v => v.Id == id, ct);

    public async Task<ProductVariant?> GetByIdWithProductAsync(Guid id, CancellationToken ct = default)
        => await context.ProductVariants
            .Include(v => v.Product)
            .FirstOrDefaultAsync(v => v.Id == id, ct);

    public async Task<IEnumerable<ProductVariant>> GetByProductIdAsync(Guid productId, CancellationToken ct = default)
        => await context.ProductVariants
            .AsNoTracking()
            .Include(v => v.Product)
            .Where(v => v.ProductId == productId)
            .ToListAsync(ct);

    public async Task<bool> AdjustStockAtomicAsync(
        Guid      id,
        int       quantity,
        byte[]    rowVersion,
        CancellationToken ct = default)
    {
        var rowsAffected = await context.Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE product_variants
            SET    stock      = stock + {quantity},
                   updated_at = NOW() AT TIME ZONE 'UTC'
            WHERE  id         = {id}
              AND  stock + {quantity} >= 0
              AND  row_version = {rowVersion}
            """, ct);

        return rowsAffected == 1;
    }

    public async Task<bool> SkuExistsForOtherVariantAsync(
        string sku,
        Guid   excludeVariantId,
        CancellationToken ct = default)
        => await context.ProductVariants
            .AnyAsync(v => v.Sku == sku && v.Id != excludeVariantId, ct);

    public async Task<IEnumerable<ProductVariant>> GetAllAsync(CancellationToken ct = default)
        => await context.ProductVariants
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task AddAsync(ProductVariant entity, CancellationToken ct = default)
        => await context.ProductVariants.AddAsync(entity, ct);

    public void Update(ProductVariant entity)
        => context.ProductVariants.Update(entity);

    public void Delete(ProductVariant entity)
        => context.ProductVariants.Remove(entity);
}
