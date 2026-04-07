namespace ProductManagement.API.Common;

public class ApiResponse<T>
{
    public bool    Success { get; init; }
    public string  Message { get; init; } = string.Empty;
    public T?      Data    { get; init; }
    public string? TraceId { get; init; }

    public static ApiResponse<T> Ok(
        T       data,
        string  message = "Request completed successfully.",
        string? traceId = null)
        => new() { Success = true, Message = message, Data = data, TraceId = traceId };

    public static ApiResponse<T> Fail(
        string  message,
        string? traceId = null)
        => new() { Success = false, Message = message, Data = default, TraceId = traceId };
}

public class ApiResponse : ApiResponse<object>
{
    public static ApiResponse NoContent(string? traceId = null)
        => new() { Success = true, Message = "Operation completed successfully." };
}
