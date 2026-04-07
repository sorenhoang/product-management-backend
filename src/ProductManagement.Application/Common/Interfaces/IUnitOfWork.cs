namespace ProductManagement.Application.Common.Interfaces;

public interface IUnitOfWork
{
    IProductRepository  Products   { get; }
    ICategoryRepository Categories { get; }
    IVariantRepository  Variants   { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
