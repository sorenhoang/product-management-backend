using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ProductManagement.API.Common;
using ProductManagement.Application.Common.Interfaces;
using ProductManagement.Application.DTOs.Requests;
using ProductManagement.Application.DTOs.Responses;

namespace ProductManagement.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/variants")]
public class VariantController(IVariantService variantService) : ControllerBase
{
    [HttpGet("{id:guid}")]
    [EnableRateLimiting("read-operations")]
    [ProducesResponseType<ApiResponse<ProductVariantResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await variantService.GetByIdAsync(id, HttpContext.RequestAborted);
        return Ok(ApiResponse<ProductVariantResponse>.Ok(
            result,
            traceId: HttpContext.TraceIdentifier));
    }

    [HttpPut("{id:guid}")]
    [EnableRateLimiting("write-operations")]
    [ProducesResponseType<ApiResponse<ProductVariantResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateVariantRequest request)
    {
        var result = await variantService.UpdateAsync(id, request, HttpContext.RequestAborted);
        return Ok(ApiResponse<ProductVariantResponse>.Ok(
            result,
            message: "Variant updated successfully.",
            traceId: HttpContext.TraceIdentifier));
    }

    /// <remarks>
    /// Quantity can be positive (restock) or negative (deduct).
    /// Rate limited to 30 requests/minute per IP.
    /// Returns 409 on concurrency conflict — client should retry.
    /// Returns 422 if resulting stock would be negative.
    /// Returns 429 if rate limit exceeded (check Retry-After header).
    /// </remarks>
    [HttpPatch("{id:guid}/stock")]
    [EnableRateLimiting("stock-adjust")]
    [ProducesResponseType<ApiResponse<ProductVariantResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> AdjustStock(Guid id, [FromBody] AdjustStockRequest request)
    {
        var result = await variantService.AdjustStockAsync(id, request, HttpContext.RequestAborted);
        return Ok(ApiResponse<ProductVariantResponse>.Ok(
            result,
            message: "Stock adjusted successfully.",
            traceId: HttpContext.TraceIdentifier));
    }

    [HttpDelete("{id:guid}")]
    [EnableRateLimiting("write-operations")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await variantService.DeleteAsync(id, HttpContext.RequestAborted);
        return NoContent();
    }
}
