using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ProductManagement.API.Common;
using ProductManagement.API.Controllers;
using ProductManagement.Application.Common.Interfaces;
using ProductManagement.Application.DTOs.Requests;
using ProductManagement.Application.DTOs.Responses;

namespace ProductManagement.UnitTests.Controllers;

public class ProductControllerTests
{
    private readonly Mock<IProductService> _mockProductService;
    private readonly Mock<IVariantService> _mockVariantService;
    private readonly ProductController     _controller;

    public ProductControllerTests()
    {
        _mockProductService = new Mock<IProductService>();
        _mockVariantService = new Mock<IVariantService>();
        _controller         = new ProductController(
            _mockProductService.Object,
            _mockVariantService.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    [Fact]
    public async Task GetPaged_Returns200_WithEnvelopedPagedResponse()
    {
        var paged = new PagedResponse<ProductResponse>
        {
            Data       = [],
            TotalCount = 50,
            Page       = 1,
            PageSize   = 20
        };
        _mockProductService
            .Setup(x => x.GetPagedAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(),
                It.IsAny<Guid?>(), It.IsAny<Domain.Enums.ProductStatus?>(),
                It.IsAny<decimal?>(), It.IsAny<decimal?>(),
                It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(paged);

        var result = await _controller.GetPaged();

        var ok       = result.Should().BeOfType<OkObjectResult>().Subject;
        var envelope = ok.Value
            .Should().BeOfType<ApiResponse<PagedResponse<ProductResponse>>>().Subject;
        envelope.Success.Should().BeTrue();
        envelope.Data!.TotalCount.Should().Be(50);
    }

    [Fact]
    public async Task Create_Returns201_WithSuccessMessage()
    {
        var productId = Guid.NewGuid();
        var detail    = new ProductDetailResponse { Id = productId, Name = "Shirt" };
        _mockProductService
            .Setup(x => x.CreateAsync(It.IsAny<CreateProductRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(detail);

        var result = await _controller.Create(new CreateProductRequest
        {
            Name        = "Shirt",
            CategoryId  = Guid.NewGuid(),
            BasePrice   = 29.99m,
            InitialVariants = []
        });

        var created  = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var envelope = created.Value.Should().BeOfType<ApiResponse<ProductDetailResponse>>().Subject;
        envelope.Success.Should().BeTrue();
        envelope.Message.Should().Be("Product created successfully.");
    }

    [Fact]
    public async Task Delete_Returns204_NoContent()
    {
        var id = Guid.NewGuid();
        _mockProductService
            .Setup(x => x.DeleteAsync(id, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.Delete(id);

        result.Should().BeOfType<NoContentResult>();
    }
}
