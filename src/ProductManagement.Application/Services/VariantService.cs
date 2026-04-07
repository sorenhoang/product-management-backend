using MapsterMapper;
using ProductManagement.Application.Common;
using ProductManagement.Application.Common.Exceptions;
using ProductManagement.Application.Common.Interfaces;
using ProductManagement.Application.DTOs.Requests;
using ProductManagement.Application.DTOs.Responses;
using ProductManagement.Domain.Entities;

namespace ProductManagement.Application.Services;

public class VariantService(
    IUnitOfWork   unitOfWork,
    ICacheService cacheService,
    IMapper       mapper) : IVariantService
{
    public async Task<IEnumerable<ProductVariantResponse>> GetByProductIdAsync(
        Guid productId,
        CancellationToken ct = default)
    {
        var product = await unitOfWork.Products.GetByIdAsync(productId, ct);
        if (product is null)
            throw new NotFoundException("Product", productId);

        var variants = await unitOfWork.Variants.GetByProductIdAsync(productId, ct);
        return mapper.Map<IEnumerable<ProductVariantResponse>>(variants);
    }

    public async Task<ProductVariantResponse> GetByIdAsync(
        Guid variantId,
        CancellationToken ct = default)
    {
        var variant = await unitOfWork.Variants.GetByIdWithProductAsync(variantId, ct);
        if (variant is null)
            throw new NotFoundException("ProductVariant", variantId);

        return mapper.Map<ProductVariantResponse>(variant);
    }

    public async Task<ProductVariantResponse> CreateAsync(
        Guid productId,
        CreateVariantRequest request,
        CancellationToken ct = default)
    {
        var product = await unitOfWork.Products.GetByIdAsync(productId, ct);
        if (product is null)
            throw new NotFoundException("Product", productId);

        var variant = mapper.Map<ProductVariant>(request);
        variant.ProductId = productId;

        await unitOfWork.Variants.AddAsync(variant, ct);
        await unitOfWork.SaveChangesAsync(ct);

        await cacheService.RemoveAsync(CacheKeys.ProductDetail(productId), ct);
        await cacheService.RemoveByPrefixAsync(CacheKeys.ProductListPrefix, ct);

        return await GetByIdAsync(variant.Id, ct);
    }

    public async Task<ProductVariantResponse> UpdateAsync(
        Guid variantId,
        UpdateVariantRequest request,
        CancellationToken ct = default)
    {
        var entity = await unitOfWork.Variants.GetByIdWithProductAsync(variantId, ct);
        if (entity is null)
            throw new NotFoundException("ProductVariant", variantId);

        if (await unitOfWork.Variants.SkuExistsForOtherVariantAsync(request.Sku, variantId, ct))
            throw new ConflictException($"SKU '{request.Sku}' is already used by another variant.");

        entity.Sku        = request.Sku;
        entity.Price      = request.Price;
        entity.Attributes = request.Attributes;

        await unitOfWork.SaveChangesAsync(ct);

        await cacheService.RemoveAsync(CacheKeys.ProductDetail(entity.ProductId), ct);
        await cacheService.RemoveByPrefixAsync(CacheKeys.ProductListPrefix, ct);

        return await GetByIdAsync(variantId, ct);
    }

    public async Task<ProductVariantResponse> AdjustStockAsync(
        Guid variantId,
        AdjustStockRequest request,
        CancellationToken ct = default)
    {
        var entity = await unitOfWork.Variants.GetByIdWithProductAsync(variantId, ct);
        if (entity is null)
            throw new NotFoundException("ProductVariant", variantId);

        if (entity.Stock + request.Quantity < 0)
            throw new BusinessException(
                $"Insufficient stock. Current stock: {entity.Stock}, " +
                $"requested deduction: {Math.Abs(request.Quantity)}.");

        var adjusted = await unitOfWork.Variants.AdjustStockAtomicAsync(
            variantId, request.Quantity, entity.RowVersion, ct);

        if (!adjusted)
        {
            var current = await unitOfWork.Variants.GetByIdWithProductAsync(variantId, ct);
            if (current is null)
                throw new NotFoundException("ProductVariant", variantId);

            if (current.Stock + request.Quantity < 0)
                throw new BusinessException(
                    $"Insufficient stock. Current stock: {current.Stock}, " +
                    $"requested deduction: {Math.Abs(request.Quantity)}.");

            throw new ConcurrencyException();
        }

        await cacheService.RemoveAsync(CacheKeys.ProductDetail(entity.ProductId), ct);
        await cacheService.RemoveByPrefixAsync(CacheKeys.ProductListPrefix, ct);

        return await GetByIdAsync(variantId, ct);
    }

    public async Task DeleteAsync(Guid variantId, CancellationToken ct = default)
    {
        var entity = await unitOfWork.Variants.GetByIdWithProductAsync(variantId, ct);
        if (entity is null)
            throw new NotFoundException("ProductVariant", variantId);

        var allVariants = await unitOfWork.Variants.GetByProductIdAsync(entity.ProductId, ct);
        if (allVariants.Count() <= 1)
            throw new BusinessException(
                "Cannot delete the only variant of a product. " +
                "A product must have at least one variant.");

        unitOfWork.Variants.Delete(entity);
        await unitOfWork.SaveChangesAsync(ct);

        await cacheService.RemoveAsync(CacheKeys.ProductDetail(entity.ProductId), ct);
        await cacheService.RemoveByPrefixAsync(CacheKeys.ProductListPrefix, ct);
    }
}
