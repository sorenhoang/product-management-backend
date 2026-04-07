using FluentAssertions;
using MapsterMapper;
using Moq;
using ProductManagement.Application.Common;
using ProductManagement.Application.Common.Exceptions;
using ProductManagement.Application.Common.Interfaces;
using ProductManagement.Application.DTOs.Requests;
using ProductManagement.Application.DTOs.Responses;
using ProductManagement.Application.Services;
using ProductManagement.Domain.Entities;

namespace ProductManagement.UnitTests.Services;

public class VariantServiceTests
{
    private readonly Mock<IUnitOfWork>          _mockUow;
    private readonly Mock<IProductRepository>   _mockProductRepo;
    private readonly Mock<IVariantRepository>   _mockVariantRepo;
    private readonly Mock<ICacheService>        _mockCache;
    private readonly Mock<IMapper>              _mockMapper;
    private readonly VariantService             _service;

    public VariantServiceTests()
    {
        _mockProductRepo = new Mock<IProductRepository>();
        _mockVariantRepo = new Mock<IVariantRepository>();
        _mockUow         = new Mock<IUnitOfWork>();
        _mockCache       = new Mock<ICacheService>();
        _mockMapper      = new Mock<IMapper>();

        _mockUow.Setup(x => x.Products).Returns(_mockProductRepo.Object);
        _mockUow.Setup(x => x.Variants).Returns(_mockVariantRepo.Object);

        _service = new VariantService(
            _mockUow.Object,
            _mockCache.Object,
            _mockMapper.Object);
    }

    // ── GetByProductIdAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetByProductIdAsync_ThrowsNotFoundException_WhenProductNotFound()
    {
        var productId = Guid.NewGuid();
        _mockProductRepo
            .Setup(x => x.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var act = async () => await _service.GetByProductIdAsync(productId);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ── GetByIdAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ThrowsNotFoundException_WhenVariantNotFound()
    {
        var variantId = Guid.NewGuid();
        _mockVariantRepo
            .Setup(x => x.GetByIdWithProductAsync(variantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductVariant?)null);

        var act = async () => await _service.GetByIdAsync(variantId);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ── CreateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ThrowsNotFoundException_WhenProductNotFound()
    {
        var productId = Guid.NewGuid();
        _mockProductRepo
            .Setup(x => x.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var act = async () => await _service.CreateAsync(
            productId, new CreateVariantRequest { Sku = "SKU-X", Stock = 5 });

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateAsync_InvalidatesCache_AfterCreation()
    {
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var product   = new Product { Id = productId, Name = "Shirt" };
        var variant   = new ProductVariant { Id = variantId, ProductId = productId, Sku = "SKU-NEW" };
        var response  = new ProductVariantResponse { Id = variantId, Sku = "SKU-NEW" };

        _mockProductRepo
            .Setup(x => x.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        _mockMapper
            .Setup(x => x.Map<ProductVariant>(It.IsAny<object>()))
            .Returns(variant);
        _mockUow
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mockVariantRepo
            .Setup(x => x.GetByIdWithProductAsync(variantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(variant);
        _mockMapper
            .Setup(x => x.Map<ProductVariantResponse>(variant))
            .Returns(response);

        await _service.CreateAsync(productId, new CreateVariantRequest { Sku = "SKU-NEW", Stock = 5 });

        _mockCache.Verify(
            x => x.RemoveAsync(CacheKeys.ProductDetail(productId), It.IsAny<CancellationToken>()),
            Times.Once);
        _mockCache.Verify(
            x => x.RemoveByPrefixAsync(CacheKeys.ProductListPrefix, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── UpdateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_ThrowsConflictException_WhenSkuUsedByOtherVariant()
    {
        var variantId = Guid.NewGuid();
        var variant   = new ProductVariant { Id = variantId, Sku = "OLD-SKU", ProductId = Guid.NewGuid() };

        _mockVariantRepo
            .Setup(x => x.GetByIdWithProductAsync(variantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(variant);
        _mockVariantRepo
            .Setup(x => x.SkuExistsForOtherVariantAsync("TAKEN-SKU", variantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var act = async () => await _service.UpdateAsync(
            variantId, new UpdateVariantRequest { Sku = "TAKEN-SKU" });

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*TAKEN-SKU*");
    }

    // ── AdjustStockAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task AdjustStockAsync_ThrowsBusinessException_WhenStockInsufficient()
    {
        var variantId = Guid.NewGuid();
        var variant   = new ProductVariant
        {
            Id         = variantId,
            Stock      = 5,
            RowVersion = [],
            ProductId  = Guid.NewGuid(),
        };

        _mockVariantRepo
            .Setup(x => x.GetByIdWithProductAsync(variantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(variant);

        var act = async () => await _service.AdjustStockAsync(
            variantId, new AdjustStockRequest { Quantity = -10 });

        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*Insufficient stock*");

        _mockVariantRepo.Verify(
            x => x.AdjustStockAtomicAsync(
                It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<byte[]>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task AdjustStockAsync_ThrowsConcurrencyException_WhenAtomicUpdateFails()
    {
        var variantId  = Guid.NewGuid();
        var productId  = Guid.NewGuid();
        var rowVersion = new byte[] { 1, 2, 3, 4 };

        var variant = new ProductVariant
        {
            Id         = variantId,
            Stock      = 10,
            RowVersion = rowVersion,
            ProductId  = productId,
        };
        // Re-fetch still has stock = 10, so it's not a stock problem → ConcurrencyException
        var refetched = new ProductVariant
        {
            Id        = variantId,
            Stock     = 10,
            ProductId = productId,
        };

        _mockVariantRepo
            .SetupSequence(x => x.GetByIdWithProductAsync(variantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(variant)
            .ReturnsAsync(refetched);
        _mockVariantRepo
            .Setup(x => x.AdjustStockAtomicAsync(variantId, -3, rowVersion, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var act = async () => await _service.AdjustStockAsync(
            variantId, new AdjustStockRequest { Quantity = -3 });

        await act.Should().ThrowAsync<ConcurrencyException>();
    }

    [Fact]
    public async Task AdjustStockAsync_ThrowsBusinessException_WhenAtomicFails_AndStockNowInsufficient()
    {
        var variantId  = Guid.NewGuid();
        var productId  = Guid.NewGuid();
        var rowVersion = new byte[] { 1, 2, 3, 4 };

        var variant = new ProductVariant
        {
            Id         = variantId,
            Stock      = 10,
            RowVersion = rowVersion,
            ProductId  = productId,
        };
        // Another request consumed stock; now only 2 remain
        var refetched = new ProductVariant
        {
            Id        = variantId,
            Stock     = 2,
            ProductId = productId,
        };

        _mockVariantRepo
            .SetupSequence(x => x.GetByIdWithProductAsync(variantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(variant)
            .ReturnsAsync(refetched);
        _mockVariantRepo
            .Setup(x => x.AdjustStockAtomicAsync(variantId, -8, rowVersion, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var act = async () => await _service.AdjustStockAsync(
            variantId, new AdjustStockRequest { Quantity = -8 });

        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*Insufficient stock*");
    }

    [Fact]
    public async Task AdjustStockAsync_InvalidatesCache_OnSuccess()
    {
        var variantId  = Guid.NewGuid();
        var productId  = Guid.NewGuid();
        var rowVersion = new byte[] { 1, 2, 3, 4 };

        var variant  = new ProductVariant
        {
            Id         = variantId,
            Stock      = 10,
            RowVersion = rowVersion,
            ProductId  = productId,
        };
        var response = new ProductVariantResponse { Id = variantId };

        _mockVariantRepo
            .Setup(x => x.GetByIdWithProductAsync(variantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(variant);
        _mockVariantRepo
            .Setup(x => x.AdjustStockAtomicAsync(variantId, -2, rowVersion, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockMapper
            .Setup(x => x.Map<ProductVariantResponse>(variant))
            .Returns(response);

        await _service.AdjustStockAsync(variantId, new AdjustStockRequest { Quantity = -2 });

        _mockCache.Verify(
            x => x.RemoveAsync(CacheKeys.ProductDetail(productId), It.IsAny<CancellationToken>()),
            Times.Once);
        _mockCache.Verify(
            x => x.RemoveByPrefixAsync(CacheKeys.ProductListPrefix, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ThrowsBusinessException_WhenOnlyVariant()
    {
        var variantId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var variant   = new ProductVariant { Id = variantId, ProductId = productId };

        _mockVariantRepo
            .Setup(x => x.GetByIdWithProductAsync(variantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(variant);
        _mockVariantRepo
            .Setup(x => x.GetByProductIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([variant]);

        var act = async () => await _service.DeleteAsync(variantId);

        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*only variant*");
    }

    [Fact]
    public async Task DeleteAsync_Succeeds_WhenMultipleVariantsExist()
    {
        var variantId  = Guid.NewGuid();
        var productId  = Guid.NewGuid();
        var variant    = new ProductVariant { Id = variantId, ProductId = productId };
        var sibling    = new ProductVariant { Id = Guid.NewGuid(), ProductId = productId };

        _mockVariantRepo
            .Setup(x => x.GetByIdWithProductAsync(variantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(variant);
        _mockVariantRepo
            .Setup(x => x.GetByProductIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([variant, sibling]);
        _mockUow
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        await _service.DeleteAsync(variantId);

        _mockVariantRepo.Verify(x => x.Delete(variant), Times.Once);
        _mockUow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
