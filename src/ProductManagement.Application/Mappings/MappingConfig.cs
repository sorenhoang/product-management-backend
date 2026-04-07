using Mapster;
using ProductManagement.Application.DTOs.Requests;
using ProductManagement.Application.DTOs.Responses;
using ProductManagement.Domain.Entities;

namespace ProductManagement.Application.Mappings;

public class MappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // Category → CategoryResponse (recursive, depth-limited)
        config.NewConfig<Category, CategoryResponse>()
            .MaxDepth(5);

        // Product → ProductResponse (flat list projection)
        config.NewConfig<Product, ProductResponse>()
            .Map(dest => dest.CategoryName,
                 src => src.Category != null ? src.Category.Name : string.Empty)
            .Map(dest => dest.Status,
                 src => src.Status.ToString())
            .Map(dest => dest.VariantCount,
                 src => src.Variants != null ? src.Variants.Count : 0);

        // Product → ProductDetailResponse (with full variant list)
        config.NewConfig<Product, ProductDetailResponse>()
            .Map(dest => dest.CategoryName,
                 src => src.Category != null ? src.Category.Name : string.Empty)
            .Map(dest => dest.Status,
                 src => src.Status.ToString())
            .Map(dest => dest.Variants,
                 src => src.Variants);

        // ProductVariant → ProductVariantResponse (resolves effective price)
        config.NewConfig<ProductVariant, ProductVariantResponse>()
            .Map(dest => dest.EffectivePrice,
                 src => src.Price ?? (src.Product != null ? src.Product.BasePrice : 0m));

        // CreateProductRequest → Product
        config.NewConfig<CreateProductRequest, Product>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.CreatedAt)
            .Ignore(dest => dest.UpdatedAt)
            .Ignore(dest => dest.Category!)
            .Ignore(dest => dest.Variants);

        // CreateVariantRequest → ProductVariant
        config.NewConfig<CreateVariantRequest, ProductVariant>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.CreatedAt)
            .Ignore(dest => dest.UpdatedAt)
            .Ignore(dest => dest.Product!)
            .Ignore(dest => dest.RowVersion);

        // CreateCategoryRequest → Category
        config.NewConfig<CreateCategoryRequest, Category>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.CreatedAt)
            .Ignore(dest => dest.UpdatedAt)
            .Ignore(dest => dest.Parent!)
            .Ignore(dest => dest.Children)
            .Ignore(dest => dest.Products);
    }
}
