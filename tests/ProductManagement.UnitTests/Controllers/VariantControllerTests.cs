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

public class VariantControllerTests
{
    private readonly Mock<IVariantService> _mockService;
    private readonly VariantController     _controller;

    public VariantControllerTests()
    {
        _mockService = new Mock<IVariantService>();
        _controller  = new VariantController(_mockService.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    [Fact]
    public async Task AdjustStock_Returns200_WithEnvelope_OnSuccess()
    {
        var variantId = Guid.NewGuid();
        var response  = new ProductVariantResponse { Id = variantId, Stock = 7 };

        _mockService
            .Setup(x => x.AdjustStockAsync(variantId, It.IsAny<AdjustStockRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _controller.AdjustStock(variantId, new AdjustStockRequest { Quantity = -3 });

        var ok       = result.Should().BeOfType<OkObjectResult>().Subject;
        var envelope = ok.Value
            .Should().BeOfType<ApiResponse<ProductVariantResponse>>().Subject;
        envelope.Success.Should().BeTrue();
        envelope.Data!.Stock.Should().Be(7);
        envelope.Message.Should().Be("Stock adjusted successfully.");
    }

    [Fact]
    public async Task AdjustStock_PropagatesConcurrencyException()
    {
        var variantId = Guid.NewGuid();
        _mockService
            .Setup(x => x.AdjustStockAsync(variantId, It.IsAny<AdjustStockRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConcurrencyException());

        var act = async () => await _controller.AdjustStock(
            variantId, new AdjustStockRequest { Quantity = -3 });

        await act.Should().ThrowAsync<ConcurrencyException>();
    }
}
