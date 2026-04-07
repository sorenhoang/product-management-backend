namespace ProductManagement.Application.DTOs.Responses;

public class ProductVariantResponse
{
    public Guid                       Id             { get; init; }
    public Guid                       ProductId      { get; init; }
    public string                     Sku            { get; init; } = string.Empty;
    public decimal?                   Price          { get; init; }
    public decimal                    EffectivePrice { get; init; }
    public int                        Stock          { get; init; }
    public Dictionary<string, string> Attributes     { get; init; } = [];
    public DateTime                   CreatedAt      { get; init; }
    public DateTime                   UpdatedAt      { get; init; }
}
