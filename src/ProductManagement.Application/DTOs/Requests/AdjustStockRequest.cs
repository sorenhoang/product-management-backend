namespace ProductManagement.Application.DTOs.Requests;

public class AdjustStockRequest
{
    public int     Quantity { get; init; }
    public string? Reason   { get; init; }
}
