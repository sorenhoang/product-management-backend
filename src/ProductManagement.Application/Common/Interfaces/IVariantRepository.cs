using ProductManagement.Domain.Entities;

namespace ProductManagement.Application.Common.Interfaces;

public interface IVariantRepository : IRepository<ProductVariant>
{
    /// <summary>Gets a variant by id, including its parent Product (needed for EffectivePrice mapping).</summary>
    Task<ProductVariant?> GetByIdWithProductAsync(
        Guid id,
        CancellationToken ct = default);

    /// <summary>Gets all variants belonging to a product.</summary>
    Task<IEnumerable<ProductVariant>> GetByProductIdAsync(
        Guid productId,
        CancellationToken ct = default);

    /// <summary>
    /// Atomically increments/decrements stock at the DB level.
    /// Includes a stock-non-negative guard and optimistic-concurrency check in the WHERE clause.
    /// Returns true if exactly one row was updated; false if the guard or row_version check failed.
    /// </summary>
    Task<bool> AdjustStockAtomicAsync(
        Guid      id,
        int       quantity,
        byte[]    rowVersion,
        CancellationToken ct = default);

    /// <summary>Returns true if a SKU is already used by a variant other than <paramref name="excludeVariantId"/>.</summary>
    Task<bool> SkuExistsForOtherVariantAsync(
        string sku,
        Guid   excludeVariantId,
        CancellationToken ct = default);
}
