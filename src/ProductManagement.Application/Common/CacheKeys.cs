namespace ProductManagement.Application.Common;

public static class CacheKeys
{
    public const string ProductListPrefix   = "products:list";
    public const string ProductDetailPrefix = "products:detail";
    public const string CategoryTreeKey     = "categories:tree";

    /// <summary>products:detail:{id}</summary>
    public static string ProductDetail(Guid id)
        => $"{ProductDetailPrefix}:{id}";

    /// <summary>products:list:{page}:{pageSize}:{search}:{categoryId}:{status}:{minPrice}:{maxPrice}:{sortBy}:{sortOrder}</summary>
    public static string ProductList(
        int page,
        int pageSize,
        string? search,
        Guid? categoryId,
        string? status,
        decimal? minPrice,
        decimal? maxPrice,
        string? sortBy,
        string? sortOrder)
        => $"{ProductListPrefix}:{page}:{pageSize}:{search ?? ""}:" +
           $"{categoryId?.ToString() ?? ""}:{status ?? ""}:" +
           $"{minPrice?.ToString() ?? ""}:{maxPrice?.ToString() ?? ""}:" +
           $"{sortBy ?? ""}:{sortOrder ?? ""}";
}
