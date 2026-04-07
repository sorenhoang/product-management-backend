using ProductManagement.Domain.Common;

namespace ProductManagement.Domain.Entities;

public class ProductVariant : BaseEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string Sku { get; set; } = string.Empty;
    public decimal? Price { get; set; }
    public int Stock { get; set; } = 0;
    public Dictionary<string, string> Attributes { get; set; } = [];
    public byte[] RowVersion { get; set; } = [];
}
