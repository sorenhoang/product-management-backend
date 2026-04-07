using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ProductManagement.API.Common;
using ProductManagement.API.Controllers;
using ProductManagement.Application.Common.Exceptions;
using ProductManagement.Application.Common.Interfaces;
using ProductManagement.Application.DTOs.Requests;
using ProductManagement.Application.DTOs.Responses;

namespace ProductManagement.UnitTests.Controllers;

public class CategoryControllerTests
{
    private readonly Mock<ICategoryService> _mockService;
    private readonly CategoryController     _controller;

    public CategoryControllerTests()
    {
        _mockService = new Mock<ICategoryService>();
        _controller  = new CategoryController(_mockService.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    [Fact]
    public async Task GetTree_Returns200_WithEnvelope()
    {
        var categories = new List<CategoryResponse>
        {
            new() { Id = Guid.NewGuid(), Name = "A" },
            new() { Id = Guid.NewGuid(), Name = "B" },
            new() { Id = Guid.NewGuid(), Name = "C" }
        };
        _mockService
            .Setup(x => x.GetTreeAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(categories);

        var result = await _controller.GetTree();

        var ok       = result.Should().BeOfType<OkObjectResult>().Subject;
        var envelope = ok.Value
            .Should().BeOfType<ApiResponse<IEnumerable<CategoryResponse>>>().Subject;
        envelope.Success.Should().BeTrue();
        envelope.Data!.Count().Should().Be(3);
    }

    [Fact]
    public async Task GetById_Returns404_WhenNotFound()
    {
        var id = Guid.NewGuid();
        _mockService
            .Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("Category", id));

        var act = async () => await _controller.GetById(id);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Create_Returns201_WithEnvelope_AndLocationHeader()
    {
        var categoryId = Guid.NewGuid();
        var response   = new CategoryResponse { Id = categoryId, Name = "Test" };
        _mockService
            .Setup(x => x.CreateAsync(It.IsAny<CreateCategoryRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _controller.Create(new CreateCategoryRequest { Name = "Test" });

        var created  = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var envelope = created.Value.Should().BeOfType<ApiResponse<CategoryResponse>>().Subject;
        envelope.Success.Should().BeTrue();
        envelope.Data!.Id.Should().Be(categoryId);
    }
}
