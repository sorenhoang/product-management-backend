using ProductManagement.Domain.Common;
using ProductManagement.Domain.Enums;

namespace ProductManagement.Domain.Entities;

public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal BasePrice { get; set; }
    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public ProductStatus Status { get; set; } = ProductStatus.Active;
    public ICollection<ProductVariant> Variants { get; set; } = [];
}
