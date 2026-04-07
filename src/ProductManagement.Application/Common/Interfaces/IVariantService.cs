using ProductManagement.Application.DTOs.Requests;
using ProductManagement.Application.DTOs.Responses;

namespace ProductManagement.Application.Common.Interfaces;

public interface IVariantService
{
    Task<IEnumerable<ProductVariantResponse>> GetByProductIdAsync(
        Guid productId,
        CancellationToken ct = default);

    Task<ProductVariantResponse> GetByIdAsync(
        Guid variantId,
        CancellationToken ct = default);

    Task<ProductVariantResponse> CreateAsync(
        Guid productId,
        CreateVariantRequest request,
        CancellationToken ct = default);

    Task<ProductVariantResponse> UpdateAsync(
        Guid variantId,
        UpdateVariantRequest request,
        CancellationToken ct = default);

    Task<ProductVariantResponse> AdjustStockAsync(
        Guid variantId,
        AdjustStockRequest request,
        CancellationToken ct = default);

    Task DeleteAsync(
        Guid variantId,
        CancellationToken ct = default);
}
