namespace ProductManagement.Application.DTOs.Requests;

public class UpdateVariantRequest
{
    public string                     Sku        { get; init; } = string.Empty;
    public decimal?                   Price      { get; init; }
    public Dictionary<string, string> Attributes { get; init; } = [];
}
