using Microsoft.EntityFrameworkCore;
using ProductManagement.Application.Common.Interfaces;
using ProductManagement.Domain.Entities;
using ProductManagement.Infrastructure.Persistence;

namespace ProductManagement.Infrastructure.Repositories;

public class CategoryRepository(AppDbContext context) : ICategoryRepository
{
    public async Task<Category?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await context.Categories
            .Include(c => c.Children)
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IEnumerable<Category>> GetAllAsync(CancellationToken ct = default)
        => await context.Categories
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task<IEnumerable<Category>> GetTreeAsync(CancellationToken ct = default)
    {
        // Load all categories flat in a single query — no depth limit, no N+1 problem.
        // Then reconstruct the tree in memory by wiring parent→children relationships.
        var all = await context.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync(ct);

        var lookup = all.ToDictionary(c => c.Id);

        foreach (var category in all)
            category.Children = [];          // reset so AsNoTracking orphans don't linger

        foreach (var category in all)
        {
            if (category.ParentId.HasValue &&
                lookup.TryGetValue(category.ParentId.Value, out var parent))
            {
                parent.Children.Add(category);
            }
        }

        return all.Where(c => c.ParentId is null);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
        => await context.Categories.AnyAsync(c => c.Id == id, ct);

    public async Task AddAsync(Category entity, CancellationToken ct = default)
        => await context.Categories.AddAsync(entity, ct);

    public void Update(Category entity)
        => context.Categories.Update(entity);

    public void Delete(Category entity)
        => context.Categories.Remove(entity);
}
