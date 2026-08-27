using Asp.Versioning;
using CustomerOrderManagement.API.Authorization;
using CustomerOrderManagement.Business.Interfaces.Services;
using CustomerOrderManagement.Domain.DTOs.Customers;
using CustomerOrderManagement.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerOrderManagement.API.Controllers;

/// <summary>
/// Manages customer records. Staff (Admin/User) can read any customer and Admin can write
/// any customer. A caller with the <c>Customer</c> role can only read or update their own
/// linked record.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public sealed class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomersController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    /// <summary>
    /// Retrieves every active customer. Requires a staff (Admin/User) account.
    /// </summary>
    /// <param name="cancellationToken">Propagates request cancellation.</param>
    /// <response code="200">The list of customers.</response>
    /// <response code="403">The caller has the <c>Customer</c> role.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CustomerResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<CustomerResponseDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        if (User.IsCustomer())
        {
            return Forbid();
        }

        var customers = await _customerService.GetAllAsync(cancellationToken);
        return Ok(customers);
    }

    /// <summary>
    /// Retrieves a single customer by id. A caller with the <c>Customer</c> role may only
    /// fetch their own linked record.
    /// </summary>
    /// <param name="id">The customer's unique identifier.</param>
    /// <param name="cancellationToken">Propagates request cancellation.</param>
    /// <response code="200">The matching customer.</response>
    /// <response code="403">The caller has the <c>Customer</c> role and <paramref name="id"/> isn't their own.</response>
    /// <response code="404">No customer exists with the given id.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CustomerResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerResponseDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (User.IsCustomer() && User.GetCustomerId() != id)
        {
            return Forbid();
        }

        var customer = await _customerService.GetByIdAsync(id, cancellationToken);
        return Ok(customer);
    }

    /// <summary>
    /// Creates a new customer. Requires the <c>Admin</c> role.
    /// </summary>
    /// <param name="dto">The customer to create.</param>
    /// <param name="cancellationToken">Propagates request cancellation.</param>
    /// <response code="201">The customer was created.</response>
    /// <response code="400">The request body failed validation.</response>
    /// <response code="403">The caller is authenticated but not an Admin.</response>
    /// <response code="409">A customer with the same email already exists.</response>
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [ProducesResponseType(typeof(CustomerResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CustomerResponseDto>> Create(
        [FromBody] CustomerCreateDto dto,
        CancellationToken cancellationToken)
    {
        var customer = await _customerService.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = customer.Id }, customer);
    }

    /// <summary>
    /// Replaces an existing customer's details. Requires the <c>Admin</c> role, or the
    /// <c>Customer</c> role updating their own linked record.
    /// </summary>
    /// <param name="id">The customer's unique identifier.</param>
    /// <param name="dto">The new customer details.</param>
    /// <param name="cancellationToken">Propagates request cancellation.</param>
    /// <response code="200">The updated customer.</response>
    /// <response code="400">The request body failed validation.</response>
    /// <response code="403">The caller is staff below Admin, or a Customer updating someone else's record.</response>
    /// <response code="404">No customer exists with the given id.</response>
    /// <response code="409">Another customer already uses the given email.</response>
    [HttpPut("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(CustomerResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CustomerResponseDto>> Update(
        Guid id,
        [FromBody] CustomerUpdateDto dto,
        CancellationToken cancellationToken)
    {
        var isOwnRecord = User.IsCustomer() && User.GetCustomerId() == id;
        if (!User.IsInRole(nameof(UserRole.Admin)) && !isOwnRecord)
        {
            return Forbid();
        }

        var customer = await _customerService.UpdateAsync(id, dto, cancellationToken);
        return Ok(customer);
    }

    /// <summary>
    /// Soft-deletes a customer. Requires the <c>Admin</c> role.
    /// </summary>
    /// <param name="id">The customer's unique identifier.</param>
    /// <param name="cancellationToken">Propagates request cancellation.</param>
    /// <response code="204">The customer was deleted.</response>
    /// <response code="403">The caller is authenticated but not an Admin.</response>
    /// <response code="404">No customer exists with the given id.</response>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _customerService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
