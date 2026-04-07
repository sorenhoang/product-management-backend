using ProductManagement.Application.DTOs.Requests;
using ProductManagement.Application.DTOs.Responses;

namespace ProductManagement.Application.Common.Interfaces;

public interface ICategoryService
{
    Task<IEnumerable<CategoryResponse>> GetTreeAsync(CancellationToken ct = default);

    Task<CategoryResponse> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<CategoryResponse> CreateAsync(
        CreateCategoryRequest request,
        CancellationToken ct = default);

    Task<CategoryResponse> UpdateAsync(
        Guid id,
        UpdateCategoryRequest request,
        CancellationToken ct = default);

    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
