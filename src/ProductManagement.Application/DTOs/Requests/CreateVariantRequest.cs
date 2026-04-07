namespace ProductManagement.Application.DTOs.Requests;

public class CreateVariantRequest
{
    public string                     Sku        { get; init; } = string.Empty;
    public decimal?                   Price      { get; init; }
    public int                        Stock      { get; init; } = 0;
    public Dictionary<string, string> Attributes { get; init; } = [];
}
