using ProductManagement.Domain.Enums;

namespace ProductManagement.Application.DTOs.Requests;

public class UpdateProductRequest
{
    public string        Name        { get; init; } = string.Empty;
    public string?       Description { get; init; }
    public decimal       BasePrice   { get; init; }
    public Guid          CategoryId  { get; init; }
    public ProductStatus Status      { get; init; }
}
