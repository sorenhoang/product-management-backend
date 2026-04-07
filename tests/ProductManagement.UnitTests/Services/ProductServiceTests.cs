using FluentAssertions;
using MapsterMapper;
using Microsoft.Extensions.Options;
using Moq;
using ProductManagement.Application.Common;
using ProductManagement.Application.Common.Exceptions;
using ProductManagement.Application.Common.Interfaces;
using ProductManagement.Application.Common.Settings;
using ProductManagement.Application.DTOs.Requests;
using ProductManagement.Application.DTOs.Responses;
using ProductManagement.Application.Services;
using ProductManagement.Domain.Entities;
using ProductManagement.Domain.Enums;

namespace ProductManagement.UnitTests.Services;

public class ProductServiceTests
{
    private readonly Mock<IUnitOfWork>          _mockUow;
    private readonly Mock<IProductRepository>   _mockProductRepo;
    private readonly Mock<ICategoryRepository>  _mockCategoryRepo;
    private readonly Mock<ICacheService>        _mockCache;
    private readonly Mock<IMapper>              _mockMapper;
    private readonly IOptions<CacheSettings>    _cacheOptions;
    private readonly ProductService             _service;

    public ProductServiceTests()
    {
        _mockProductRepo  = new Mock<IProductRepository>();
        _mockCategoryRepo = new Mock<ICategoryRepository>();
        _mockUow          = new Mock<IUnitOfWork>();
        _mockCache        = new Mock<ICacheService>();
        _mockMapper       = new Mock<IMapper>();
        _cacheOptions     = Options.Create(new CacheSettings());

        _mockUow.Setup(x => x.Products).Returns(_mockProductRepo.Object);
        _mockUow.Setup(x => x.Categories).Returns(_mockCategoryRepo.Object);

        _service = new ProductService(
            _mockUow.Object,
            _mockCache.Object,
            _cacheOptions,
            _mockMapper.Object);
    }

    // ── GetPagedAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPagedAsync_ReturnsCachedResult_WhenCacheHit()
    {
        var cached = new PagedResponse<ProductResponse>
        {
            Data       = [new ProductResponse { Id = Guid.NewGuid(), Name = "Shirt" }],
            TotalCount = 1,
            Page       = 1,
            PageSize   = 10,
        };

        _mockCache
            .Setup(x => x.GetOrSetAsync<PagedResponse<ProductResponse>>(
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, Task<PagedResponse<ProductResponse>>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cached);

        var result = await _service.GetPagedAsync(1, 10);

        result.Should().BeEquivalentTo(cached);
        _mockProductRepo.Verify(
            x => x.GetPagedAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(),
                It.IsAny<Guid?>(), It.IsAny<ProductStatus?>(),
                It.IsAny<decimal?>(), It.IsAny<decimal?>(),
                It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── GetByIdAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ThrowsNotFoundException_WhenProductDoesNotExist()
    {
        var id = Guid.NewGuid();

        _mockCache
            .Setup(x => x.GetOrSetAsync<ProductDetailResponse>(
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, Task<ProductDetailResponse>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, Func<CancellationToken, Task<ProductDetailResponse>>, TimeSpan?, CancellationToken>(
                async (_, factory, __, ct) => await factory(ct));

        _mockProductRepo
            .Setup(x => x.GetWithVariantsAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var act = async () => await _service.GetByIdAsync(id);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsMappedDetailResponse_WhenFound()
    {
        var id       = Guid.NewGuid();
        var category = new Category { Id = Guid.NewGuid(), Name = "Tops" };
        var product  = new Product
        {
            Id       = id,
            Name     = "T-Shirt",
            Category = category,
            Variants =
            [
                new ProductVariant { Id = Guid.NewGuid(), Sku = "SKU-001" },
                new ProductVariant { Id = Guid.NewGuid(), Sku = "SKU-002" },
            ],
        };
        var response = new ProductDetailResponse
        {
            Id       = id,
            Name     = "T-Shirt",
            Variants = product.Variants.Select(v => new ProductVariantResponse { Id = v.Id }),
        };

        _mockCache
            .Setup(x => x.GetOrSetAsync<ProductDetailResponse>(
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, Task<ProductDetailResponse>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, Func<CancellationToken, Task<ProductDetailResponse>>, TimeSpan?, CancellationToken>(
                async (_, factory, __, ct) => await factory(ct));

        _mockProductRepo
            .Setup(x => x.GetWithVariantsAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        _mockMapper
            .Setup(x => x.Map<ProductDetailResponse>(product))
            .Returns(response);

        var result = await _service.GetByIdAsync(id);

        result.Id.Should().Be(id);
        result.Name.Should().Be("T-Shirt");
        result.Variants.Should().HaveCount(2);
    }

    // ── CreateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_AddsVariants_AndInvalidatesListCache()
    {
        var productId = Guid.NewGuid();
        var request   = new CreateProductRequest
        {
            Name            = "New Shirt",
            BasePrice       = 29.99m,
            CategoryId      = Guid.NewGuid(),
            InitialVariants =
            [
                new CreateVariantRequest { Sku = "SKU-A", Stock = 10 },
                new CreateVariantRequest { Sku = "SKU-B", Stock = 5 },
            ],
        };

        var product = new Product { Id = productId, Name = request.Name, Variants = [] };
        var detail  = new ProductDetailResponse { Id = productId, Name = request.Name };

        _mockMapper.Setup(x => x.Map<Product>(It.IsAny<object>())).Returns(product);
        _mockMapper.Setup(x => x.Map<ProductVariant>(It.IsAny<object>())).Returns(new ProductVariant());
        _mockUow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // GetByIdAsync cache call — delegate to factory
        _mockCache
            .Setup(x => x.GetOrSetAsync<ProductDetailResponse>(
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, Task<ProductDetailResponse>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, Func<CancellationToken, Task<ProductDetailResponse>>, TimeSpan?, CancellationToken>(
                async (_, factory, __, ct) => await factory(ct));

        _mockProductRepo
            .Setup(x => x.GetWithVariantsAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        _mockMapper.Setup(x => x.Map<ProductDetailResponse>(product)).Returns(detail);

        await _service.CreateAsync(request);

        _mockUow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockCache.Verify(
            x => x.RemoveByPrefixAsync(CacheKeys.ProductListPrefix, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ThrowsConflictException_WhenDuplicateSku()
    {
        var request = new CreateProductRequest
        {
            Name            = "Duplicate SKU Product",
            BasePrice       = 10m,
            CategoryId      = Guid.NewGuid(),
            InitialVariants = [new CreateVariantRequest { Sku = "DUPE-001", Stock = 1 }],
        };

        _mockMapper.Setup(x => x.Map<Product>(It.IsAny<object>())).Returns(new Product { Variants = [] });
        _mockMapper.Setup(x => x.Map<ProductVariant>(It.IsAny<object>())).Returns(new ProductVariant());
        _mockUow
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConflictException("A record with the same unique value already exists."));

        var act = async () => await _service.CreateAsync(request);

        await act.Should().ThrowAsync<ConflictException>();
    }

    // ── UpdateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_ThrowsNotFoundException_WhenProductNotFound()
    {
        var id = Guid.NewGuid();
        _mockProductRepo
            .Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var act = async () => await _service.UpdateAsync(
            id, new UpdateProductRequest { Name = "X", CategoryId = Guid.NewGuid() });

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_ThrowsNotFoundException_WhenCategoryNotFound()
    {
        var id         = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var product    = new Product { Id = id, Name = "Shirt" };

        _mockProductRepo
            .Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        _mockCategoryRepo
            .Setup(x => x.ExistsAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var act = async () => await _service.UpdateAsync(
            id, new UpdateProductRequest { Name = "X", CategoryId = categoryId });

        await act.Should().ThrowAsync<NotFoundException>()
            .Where(e => e.Message.Contains("Category"));
    }

    [Fact]
    public async Task UpdateAsync_InvalidatesBothCaches_OnSuccess()
    {
        var id         = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var product    = new Product { Id = id, Name = "Shirt", Variants = [] };
        var detail     = new ProductDetailResponse { Id = id };

        _mockProductRepo
            .Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        _mockCategoryRepo
            .Setup(x => x.ExistsAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockUow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _mockCache
            .Setup(x => x.GetOrSetAsync<ProductDetailResponse>(
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, Task<ProductDetailResponse>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, Func<CancellationToken, Task<ProductDetailResponse>>, TimeSpan?, CancellationToken>(
                async (_, factory, __, ct) => await factory(ct));
        _mockProductRepo
            .Setup(x => x.GetWithVariantsAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        _mockMapper.Setup(x => x.Map<ProductDetailResponse>(product)).Returns(detail);

        await _service.UpdateAsync(id, new UpdateProductRequest
        {
            Name       = "Updated Shirt",
            BasePrice  = 49.99m,
            CategoryId = categoryId,
            Status     = ProductStatus.Active,
        });

        _mockCache.Verify(
            x => x.RemoveAsync(CacheKeys.ProductDetail(id), It.IsAny<CancellationToken>()),
            Times.Once);
        _mockCache.Verify(
            x => x.RemoveByPrefixAsync(CacheKeys.ProductListPrefix, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_SoftDeletes_BySettingStatusInactive()
    {
        var id      = Guid.NewGuid();
        var product = new Product { Id = id, Name = "Shirt", Status = ProductStatus.Active };

        _mockProductRepo
            .Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        _mockUow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        await _service.DeleteAsync(id);

        product.Status.Should().Be(ProductStatus.Inactive);
        _mockUow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
