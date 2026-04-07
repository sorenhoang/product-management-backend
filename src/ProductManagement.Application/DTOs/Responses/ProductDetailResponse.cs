namespace ProductManagement.Application.DTOs.Responses;

public class ProductDetailResponse
{
    public Guid                                Id           { get; init; }
    public string                              Name         { get; init; } = string.Empty;
    public string?                             Description  { get; init; }
    public decimal                             BasePrice    { get; init; }
    public Guid                                CategoryId   { get; init; }
    public string                              CategoryName { get; init; } = string.Empty;
    public string                              Status       { get; init; } = string.Empty;
    public IEnumerable<ProductVariantResponse> Variants     { get; init; } = [];
    public DateTime                            CreatedAt    { get; init; }
    public DateTime                            UpdatedAt    { get; init; }
}
