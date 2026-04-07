using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ProductManagement.API.Common;
using ProductManagement.Application.Common.Interfaces;
using ProductManagement.Application.DTOs.Requests;
using ProductManagement.Application.DTOs.Responses;
using ProductManagement.Domain.Enums;

namespace ProductManagement.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/products")]
public class ProductController(
    IProductService productService,
    IVariantService variantService) : ControllerBase
{
    [HttpGet]
    [EnableRateLimiting("read-operations")]
    [ProducesResponseType<ApiResponse<PagedResponse<ProductResponse>>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int            page       = 1,
        [FromQuery] int            pageSize   = 20,
        [FromQuery] string?        search     = null,
        [FromQuery] Guid?          categoryId = null,
        [FromQuery] ProductStatus? status     = null,
        [FromQuery] decimal?       minPrice   = null,
        [FromQuery] decimal?       maxPrice   = null,
        [FromQuery] string?        sortBy     = null,
        [FromQuery] string?        sortOrder  = "desc")
    {
        var result = await productService.GetPagedAsync(
            page, pageSize, search, categoryId, status,
            minPrice, maxPrice, sortBy, sortOrder,
            HttpContext.RequestAborted);
        return Ok(ApiResponse<PagedResponse<ProductResponse>>.Ok(
            result,
            traceId: HttpContext.TraceIdentifier));
    }

    [HttpGet("{id:guid}")]
    [EnableRateLimiting("read-operations")]
    [ProducesResponseType<ApiResponse<ProductDetailResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await productService.GetByIdAsync(id, HttpContext.RequestAborted);
        return Ok(ApiResponse<ProductDetailResponse>.Ok(
            result,
            traceId: HttpContext.TraceIdentifier));
    }

    [HttpPost]
    [EnableRateLimiting("write-operations")]
    [ProducesResponseType<ApiResponse<ProductDetailResponse>>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request)
    {
        var result = await productService.CreateAsync(request, HttpContext.RequestAborted);
        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Id },
            ApiResponse<ProductDetailResponse>.Ok(
                result,
                message: "Product created successfully.",
                traceId: HttpContext.TraceIdentifier));
    }

    [HttpPut("{id:guid}")]
    [EnableRateLimiting("write-operations")]
    [ProducesResponseType<ApiResponse<ProductDetailResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductRequest request)
    {
        var result = await productService.UpdateAsync(id, request, HttpContext.RequestAborted);
        return Ok(ApiResponse<ProductDetailResponse>.Ok(
            result,
            message: "Product updated successfully.",
            traceId: HttpContext.TraceIdentifier));
    }

    [HttpDelete("{id:guid}")]
    [EnableRateLimiting("write-operations")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await productService.DeleteAsync(id, HttpContext.RequestAborted);
        return NoContent();
    }

    [HttpGet("{id:guid}/variants")]
    [EnableRateLimiting("read-operations")]
    [ProducesResponseType<ApiResponse<IEnumerable<ProductVariantResponse>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetVariants(Guid id)
    {
        var result = await variantService.GetByProductIdAsync(id, HttpContext.RequestAborted);
        return Ok(ApiResponse<IEnumerable<ProductVariantResponse>>.Ok(
            result,
            traceId: HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/variants")]
    [EnableRateLimiting("write-operations")]
    [ProducesResponseType<ApiResponse<ProductVariantResponse>>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateVariant(Guid id, [FromBody] CreateVariantRequest request)
    {
        var result = await variantService.CreateAsync(id, request, HttpContext.RequestAborted);
        return CreatedAtAction(
            nameof(VariantController.GetById),
            "Variant",
            new { id = result.Id },
            ApiResponse<ProductVariantResponse>.Ok(
                result,
                message: "Variant created successfully.",
                traceId: HttpContext.TraceIdentifier));
    }
}
