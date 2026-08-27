using Asp.Versioning;
using CustomerOrderManagement.API.Authorization;
using CustomerOrderManagement.Business.Interfaces.Services;
using CustomerOrderManagement.Domain.DTOs.Products;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerOrderManagement.API.Controllers;

/// <summary>
/// Manages the product catalog. Reads are available to any authenticated user;
/// writes require the <c>Admin</c> role.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public sealed class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    /// <summary>
    /// Retrieves every active product.
    /// </summary>
    /// <param name="cancellationToken">Propagates request cancellation.</param>
    /// <response code="200">The list of products.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ProductResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProductResponseDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        var products = await _productService.GetAllAsync(cancellationToken);
        return Ok(products);
    }

    /// <summary>
    /// Retrieves a single product by id.
    /// </summary>
    /// <param name="id">The product's unique identifier.</param>
    /// <param name="cancellationToken">Propagates request cancellation.</param>
    /// <response code="200">The matching product.</response>
    /// <response code="404">No product exists with the given id.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProductResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductResponseDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var product = await _productService.GetByIdAsync(id, cancellationToken);
        return Ok(product);
    }

    /// <summary>
    /// Creates a new product. Requires the <c>Admin</c> role.
    /// </summary>
    /// <param name="dto">The product to create.</param>
    /// <param name="cancellationToken">Propagates request cancellation.</param>
    /// <response code="201">The product was created.</response>
    /// <response code="400">The request body failed validation.</response>
    /// <response code="403">The caller is authenticated but not an Admin.</response>
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [ProducesResponseType(typeof(ProductResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ProductResponseDto>> Create(
        [FromBody] ProductCreateDto dto,
        CancellationToken cancellationToken)
    {
        var product = await _productService.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    /// <summary>
    /// Replaces an existing product's details. Requires the <c>Admin</c> role.
    /// </summary>
    /// <param name="id">The product's unique identifier.</param>
    /// <param name="dto">The new product details.</param>
    /// <param name="cancellationToken">Propagates request cancellation.</param>
    /// <response code="200">The updated product.</response>
    /// <response code="400">The request body failed validation.</response>
    /// <response code="403">The caller is authenticated but not an Admin.</response>
    /// <response code="404">No product exists with the given id.</response>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [ProducesResponseType(typeof(ProductResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductResponseDto>> Update(
        Guid id,
        [FromBody] ProductUpdateDto dto,
        CancellationToken cancellationToken)
    {
        var product = await _productService.UpdateAsync(id, dto, cancellationToken);
        return Ok(product);
    }

    /// <summary>
    /// Soft-deletes a product. Requires the <c>Admin</c> role.
    /// </summary>
    /// <param name="id">The product's unique identifier.</param>
    /// <param name="cancellationToken">Propagates request cancellation.</param>
    /// <response code="204">The product was deleted.</response>
    /// <response code="403">The caller is authenticated but not an Admin.</response>
    /// <response code="404">No product exists with the given id.</response>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _productService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
