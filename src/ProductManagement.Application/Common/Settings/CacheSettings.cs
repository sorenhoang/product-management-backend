namespace ProductManagement.Application.Common.Settings;

public class CacheSettings
{
    public const string SectionName = "Cache";

    public int ProductListTtlSeconds   { get; init; } = 60;
    public int ProductDetailTtlSeconds { get; init; } = 300;
    public int CategoryTreeTtlSeconds  { get; init; } = 600;
}
