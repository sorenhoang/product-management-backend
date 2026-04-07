namespace ProductManagement.Application.DTOs.Responses;

public class ProductResponse
{
    public Guid     Id           { get; init; }
    public string   Name         { get; init; } = string.Empty;
    public string?  Description  { get; init; }
    public decimal  BasePrice    { get; init; }
    public Guid     CategoryId   { get; init; }
    public string   CategoryName { get; init; } = string.Empty;
    public string   Status       { get; init; } = string.Empty;
    public int      VariantCount { get; init; }
    public DateTime CreatedAt    { get; init; }
    public DateTime UpdatedAt    { get; init; }
}
