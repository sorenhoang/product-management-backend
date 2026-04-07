using Microsoft.EntityFrameworkCore;
using Npgsql;
using ProductManagement.Application.Common.Exceptions;
using ProductManagement.Application.Common.Interfaces;
using ProductManagement.Infrastructure.Persistence;

namespace ProductManagement.Infrastructure.Repositories;

public class UnitOfWork(
    AppDbContext        context,
    ICategoryRepository categories,
    IProductRepository  products,
    IVariantRepository  variants) : IUnitOfWork
{
    public ICategoryRepository Categories { get; } = categories;
    public IProductRepository  Products   { get; } = products;
    public IVariantRepository  Variants   { get; } = variants;

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            return await context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyException();
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            throw new ConflictException(
                "A record with the same unique value already exists.");
        }
    }
}
