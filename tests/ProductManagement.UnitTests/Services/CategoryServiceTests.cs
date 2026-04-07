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

namespace ProductManagement.UnitTests.Services;

public class CategoryServiceTests
{
    private readonly Mock<IUnitOfWork>          _mockUow;
    private readonly Mock<ICategoryRepository>  _mockCategoryRepo;
    private readonly Mock<ICacheService>        _mockCache;
    private readonly Mock<IMapper>              _mockMapper;
    private readonly IOptions<CacheSettings>    _cacheOptions;
    private readonly CategoryService            _service;

    public CategoryServiceTests()
    {
        _mockCategoryRepo = new Mock<ICategoryRepository>();
        _mockUow          = new Mock<IUnitOfWork>();
        _mockCache        = new Mock<ICacheService>();
        _mockMapper       = new Mock<IMapper>();
        _cacheOptions     = Options.Create(new CacheSettings());

        _mockUow.Setup(x => x.Categories).Returns(_mockCategoryRepo.Object);

        _service = new CategoryService(
            _mockUow.Object,
            _mockCache.Object,
            _cacheOptions,
            _mockMapper.Object);
    }

    // ── GetTreeAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetTreeAsync_ReturnsCachedResult_WhenCacheHit()
    {
        var cached = new List<CategoryResponse>
        {
            new() { Id = Guid.NewGuid(), Name = "Electronics" }
        };

        _mockCache
            .Setup(x => x.GetOrSetAsync<IEnumerable<CategoryResponse>>(
                CacheKeys.CategoryTreeKey,
                It.IsAny<Func<CancellationToken, Task<IEnumerable<CategoryResponse>>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cached);

        var result = await _service.GetTreeAsync();

        result.Should().BeEquivalentTo(cached);
        _mockCategoryRepo.Verify(x => x.GetTreeAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── GetByIdAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ThrowsNotFoundException_WhenCategoryDoesNotExist()
    {
        var id = Guid.NewGuid();
        _mockCategoryRepo
            .Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category?)null);

        var act = async () => await _service.GetByIdAsync(id);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsMappedResponse_WhenFound()
    {
        var id       = Guid.NewGuid();
        var category = new Category { Id = id, Name = "Books" };
        var response = new CategoryResponse { Id = id, Name = "Books" };

        _mockCategoryRepo
            .Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);
        _mockMapper
            .Setup(x => x.Map<CategoryResponse>(category))
            .Returns(response);

        var result = await _service.GetByIdAsync(id);

        result.Id.Should().Be(id);
        result.Name.Should().Be("Books");
    }

    // ── CreateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_InvalidatesCache_AfterCreation()
    {
        var request  = new CreateCategoryRequest { Name = "Clothing" };
        var category = new Category { Id = Guid.NewGuid(), Name = "Clothing" };
        var response = new CategoryResponse { Id = category.Id, Name = "Clothing" };

        _mockMapper.Setup(x => x.Map<Category>(It.IsAny<object>())).Returns(category);
        _mockMapper.Setup(x => x.Map<CategoryResponse>(category)).Returns(response);
        _mockUow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        await _service.CreateAsync(request);

        _mockUow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockCache.Verify(
            x => x.RemoveAsync(CacheKeys.CategoryTreeKey, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ThrowsNotFoundException_WhenNotFound()
    {
        var id = Guid.NewGuid();
        _mockCategoryRepo
            .Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category?)null);

        var act = async () => await _service.DeleteAsync(id);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeleteAsync_ThrowsBusinessException_WhenCategoryHasChildren()
    {
        var id = Guid.NewGuid();
        var category = new Category
        {
            Id       = id,
            Name     = "Parent",
            Children = [new Category { Name = "Child" }],
            Products = []
        };

        _mockCategoryRepo
            .Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        var act = async () => await _service.DeleteAsync(id);

        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*subcategories*");
    }

    [Fact]
    public async Task DeleteAsync_ThrowsBusinessException_WhenCategoryHasProducts()
    {
        var id = Guid.NewGuid();
        var category = new Category
        {
            Id       = id,
            Name     = "WithProducts",
            Children = [],
            Products = [new Product { Name = "Shirt" }]
        };

        _mockCategoryRepo
            .Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        var act = async () => await _service.DeleteAsync(id);

        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*products*");
    }
}
