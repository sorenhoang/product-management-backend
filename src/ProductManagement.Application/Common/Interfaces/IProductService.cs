using ProductManagement.Application.DTOs.Requests;
using ProductManagement.Application.DTOs.Responses;
using ProductManagement.Domain.Enums;

namespace ProductManagement.Application.Common.Interfaces;

public interface IProductService
{
    Task<PagedResponse<ProductResponse>> GetPagedAsync(
        int page,
        int pageSize,
        string?        search     = null,
        Guid?          categoryId = null,
        ProductStatus? status     = null,
        decimal?       minPrice   = null,
        decimal?       maxPrice   = null,
        string?        sortBy     = null,
        string?        sortOrder  = null,
        CancellationToken ct      = default);

    Task<ProductDetailResponse> GetByIdAsync(
        Guid id,
        CancellationToken ct = default);

    Task<ProductDetailResponse> CreateAsync(
        CreateProductRequest request,
        CancellationToken ct = default);

    Task<ProductDetailResponse> UpdateAsync(
        Guid id,
        UpdateProductRequest request,
        CancellationToken ct = default);

    Task DeleteAsync(
        Guid id,
        CancellationToken ct = default);
}
