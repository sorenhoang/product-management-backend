using MapsterMapper;
using Microsoft.Extensions.Options;
using ProductManagement.Application.Common;
using ProductManagement.Application.Common.Exceptions;
using ProductManagement.Application.Common.Interfaces;
using ProductManagement.Application.Common.Settings;
using ProductManagement.Application.DTOs.Requests;
using ProductManagement.Application.DTOs.Responses;
using ProductManagement.Domain.Entities;

namespace ProductManagement.Application.Services;

public class CategoryService(
    IUnitOfWork unitOfWork,
    ICacheService cacheService,
    IOptions<CacheSettings> cacheSettings,
    IMapper mapper) : ICategoryService
{
    public async Task<IEnumerable<CategoryResponse>> GetTreeAsync(CancellationToken ct = default)
    {
        var ttl = TimeSpan.FromSeconds(cacheSettings.Value.CategoryTreeTtlSeconds);

        return await cacheService.GetOrSetAsync<IEnumerable<CategoryResponse>>(
            CacheKeys.CategoryTreeKey,
            async innerCt =>
            {
                var categories = await unitOfWork.Categories.GetTreeAsync(innerCt);
                return mapper.Map<IEnumerable<CategoryResponse>>(categories);
            },
            ttl,
            ct);
    }

    public async Task<CategoryResponse> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var category = await unitOfWork.Categories.GetByIdAsync(id, ct);
        if (category is null)
            throw new NotFoundException("Category", id);

        return mapper.Map<CategoryResponse>(category);
    }

    public async Task<CategoryResponse> CreateAsync(
        CreateCategoryRequest request,
        CancellationToken ct = default)
    {
        var category = mapper.Map<Category>(request);
        await unitOfWork.Categories.AddAsync(category, ct);
        await unitOfWork.SaveChangesAsync(ct);
        await cacheService.RemoveAsync(CacheKeys.CategoryTreeKey, ct);
        return mapper.Map<CategoryResponse>(category);
    }

    public async Task<CategoryResponse> UpdateAsync(
        Guid id,
        UpdateCategoryRequest request,
        CancellationToken ct = default)
    {
        var category = await unitOfWork.Categories.GetByIdAsync(id, ct);
        if (category is null)
            throw new NotFoundException("Category", id);

        category.Name = request.Name;
        await unitOfWork.SaveChangesAsync(ct);
        await cacheService.RemoveAsync(CacheKeys.CategoryTreeKey, ct);
        return mapper.Map<CategoryResponse>(category);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var category = await unitOfWork.Categories.GetByIdAsync(id, ct);
        if (category is null)
            throw new NotFoundException("Category", id);

        if (category.Children.Any())
            throw new BusinessException(
                "Cannot delete a category that has subcategories. " +
                "Please delete or reassign subcategories first.");

        if (category.Products.Any())
            throw new BusinessException(
                "Cannot delete a category that has associated products. " +
                "Please reassign or remove products first.");

        unitOfWork.Categories.Delete(category);
        await unitOfWork.SaveChangesAsync(ct);
        await cacheService.RemoveAsync(CacheKeys.CategoryTreeKey, ct);
    }
}
