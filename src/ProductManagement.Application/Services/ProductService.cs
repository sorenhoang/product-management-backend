using MapsterMapper;
using Microsoft.Extensions.Options;
using ProductManagement.Application.Common;
using ProductManagement.Application.Common.Exceptions;
using ProductManagement.Application.Common.Interfaces;
using ProductManagement.Application.Common.Settings;
using ProductManagement.Application.DTOs.Requests;
using ProductManagement.Application.DTOs.Responses;
using ProductManagement.Domain.Entities;
using ProductManagement.Domain.Enums;

namespace ProductManagement.Application.Services;

public class ProductService(
    IUnitOfWork              unitOfWork,
    ICacheService            cacheService,
    IOptions<CacheSettings>  cacheSettings,
    IMapper                  mapper) : IProductService
{
    private readonly CacheSettings _cache = cacheSettings.Value;

    public async Task<PagedResponse<ProductResponse>> GetPagedAsync(
        int page,
        int pageSize,
        string?        search     = null,
        Guid?          categoryId = null,
        ProductStatus? status     = null,
        decimal?       minPrice   = null,
        decimal?       maxPrice   = null,
        string?        sortBy     = null,
        string?        sortOrder  = null,
        CancellationToken ct      = default)
    {
        var key = CacheKeys.ProductList(
            page, pageSize, search, categoryId,
            status?.ToString(), minPrice, maxPrice, sortBy, sortOrder);

        var ttl = TimeSpan.FromSeconds(_cache.ProductListTtlSeconds);

        return await cacheService.GetOrSetAsync<PagedResponse<ProductResponse>>(
            key,
            async cancellationToken =>
            {
                var (items, totalCount) = await unitOfWork.Products.GetPagedAsync(
                    page, pageSize, search, categoryId, status,
                    minPrice, maxPrice, sortBy, sortOrder, cancellationToken);

                return new PagedResponse<ProductResponse>
                {
                    Data       = mapper.Map<IEnumerable<ProductResponse>>(items),
                    TotalCount = totalCount,
                    Page       = page,
                    PageSize   = pageSize,
                };
            },
            ttl,
            ct)!;
    }

    public async Task<ProductDetailResponse> GetByIdAsync(
        Guid id,
        CancellationToken ct = default)
    {
        var key = CacheKeys.ProductDetail(id);
        var ttl = TimeSpan.FromSeconds(_cache.ProductDetailTtlSeconds);

        return await cacheService.GetOrSetAsync<ProductDetailResponse>(
            key,
            async cancellationToken =>
            {
                var product = await unitOfWork.Products.GetWithVariantsAsync(id, cancellationToken);
                if (product is null)
                    throw new NotFoundException("Product", id);

                return mapper.Map<ProductDetailResponse>(product);
            },
            ttl,
            ct)!;
    }

    public async Task<ProductDetailResponse> CreateAsync(
        CreateProductRequest request,
        CancellationToken ct = default)
    {
        var product  = mapper.Map<Product>(request);
        product.Variants = request.InitialVariants
            .Select(v => mapper.Map<ProductVariant>(v))
            .ToList();

        await unitOfWork.Products.AddAsync(product, ct);
        await unitOfWork.SaveChangesAsync(ct);

        await cacheService.RemoveByPrefixAsync(CacheKeys.ProductListPrefix, ct);

        return await GetByIdAsync(product.Id, ct);
    }

    public async Task<ProductDetailResponse> UpdateAsync(
        Guid id,
        UpdateProductRequest request,
        CancellationToken ct = default)
    {
        var entity = await unitOfWork.Products.GetByIdAsync(id, ct);
        if (entity is null)
            throw new NotFoundException("Product", id);

        if (!await unitOfWork.Categories.ExistsAsync(request.CategoryId, ct))
            throw new NotFoundException("Category", request.CategoryId);

        entity.Name        = request.Name;
        entity.Description = request.Description;
        entity.BasePrice   = request.BasePrice;
        entity.CategoryId  = request.CategoryId;
        entity.Status      = request.Status;

        await unitOfWork.SaveChangesAsync(ct);

        await cacheService.RemoveAsync(CacheKeys.ProductDetail(id), ct);
        await cacheService.RemoveByPrefixAsync(CacheKeys.ProductListPrefix, ct);

        return await GetByIdAsync(id, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await unitOfWork.Products.GetByIdAsync(id, ct);
        if (entity is null)
            throw new NotFoundException("Product", id);

        entity.Status = ProductStatus.Inactive;

        await unitOfWork.SaveChangesAsync(ct);

        await cacheService.RemoveAsync(CacheKeys.ProductDetail(id), ct);
        await cacheService.RemoveByPrefixAsync(CacheKeys.ProductListPrefix, ct);
    }
}
