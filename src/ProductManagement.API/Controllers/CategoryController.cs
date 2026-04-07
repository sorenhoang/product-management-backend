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
[Route("api/v{version:apiVersion}/categories")]
public class CategoryController(ICategoryService categoryService) : ControllerBase
{
    [HttpGet]
    [EnableRateLimiting("read-operations")]
    [ProducesResponseType<ApiResponse<IEnumerable<CategoryResponse>>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTree()
    {
        var result = await categoryService.GetTreeAsync(HttpContext.RequestAborted);
        return Ok(ApiResponse<IEnumerable<CategoryResponse>>.Ok(
            result,
            traceId: HttpContext.TraceIdentifier));
    }

    [HttpGet("{id:guid}")]
    [EnableRateLimiting("read-operations")]
    [ProducesResponseType<ApiResponse<CategoryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await categoryService.GetByIdAsync(id, HttpContext.RequestAborted);
        return Ok(ApiResponse<CategoryResponse>.Ok(
            result,
            traceId: HttpContext.TraceIdentifier));
    }

    [HttpPost]
    [EnableRateLimiting("write-operations")]
    [ProducesResponseType<ApiResponse<CategoryResponse>>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateCategoryRequest request)
    {
        var result = await categoryService.CreateAsync(request, HttpContext.RequestAborted);
        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Id },
            ApiResponse<CategoryResponse>.Ok(
                result,
                message: "Category created successfully.",
                traceId: HttpContext.TraceIdentifier));
    }

    [HttpPut("{id:guid}")]
    [EnableRateLimiting("write-operations")]
    [ProducesResponseType<ApiResponse<CategoryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCategoryRequest request)
    {
        var result = await categoryService.UpdateAsync(id, request, HttpContext.RequestAborted);
        return Ok(ApiResponse<CategoryResponse>.Ok(
            result,
            message: "Category updated successfully.",
            traceId: HttpContext.TraceIdentifier));
    }

    [HttpDelete("{id:guid}")]
    [EnableRateLimiting("write-operations")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await categoryService.DeleteAsync(id, HttpContext.RequestAborted);
        return NoContent();
    }
}
