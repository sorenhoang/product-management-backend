namespace ProductManagement.Application.DTOs.Responses;

public class CategoryResponse
{
    public Guid                        Id       { get; init; }
    public string                      Name     { get; init; } = string.Empty;
    public Guid?                       ParentId { get; init; }
    public IEnumerable<CategoryResponse> Children { get; init; } = [];
}
