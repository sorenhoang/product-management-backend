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
        => await context.Categories
            .AsNoTracking()
            .Where(c => c.ParentId == null)
            .Include(c => c.Children)
                .ThenInclude(c => c.Children)
                    .ThenInclude(c => c.Children)
                        .ThenInclude(c => c.Children)
                            .ThenInclude(c => c.Children)
            .ToListAsync(ct);

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
        => await context.Categories.AnyAsync(c => c.Id == id, ct);

    public async Task AddAsync(Category entity, CancellationToken ct = default)
        => await context.Categories.AddAsync(entity, ct);

    public void Update(Category entity)
        => context.Categories.Update(entity);

    public void Delete(Category entity)
        => context.Categories.Remove(entity);
}
