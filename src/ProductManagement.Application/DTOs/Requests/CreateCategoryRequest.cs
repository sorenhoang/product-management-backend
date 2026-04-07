namespace ProductManagement.Application.DTOs.Requests;

public class CreateCategoryRequest
{
    public string Name     { get; init; } = string.Empty;
    public Guid?  ParentId { get; init; }
}
