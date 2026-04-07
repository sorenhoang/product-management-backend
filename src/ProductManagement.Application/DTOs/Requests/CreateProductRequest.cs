using ProductManagement.Domain.Enums;

namespace ProductManagement.Application.DTOs.Requests;

public class CreateProductRequest
{
    public string                          Name            { get; init; } = string.Empty;
    public string?                         Description     { get; init; }
    public decimal                         BasePrice       { get; init; }
    public Guid                            CategoryId      { get; init; }
    public ProductStatus                   Status          { get; init; } = ProductStatus.Active;
    public IEnumerable<CreateVariantRequest> InitialVariants { get; init; } = [];
}
