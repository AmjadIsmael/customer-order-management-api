using Asp.Versioning;
using CustomerOrderManagement.API.Authorization;
using CustomerOrderManagement.Business.Interfaces.Services;
using CustomerOrderManagement.Domain.DTOs.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerOrderManagement.API.Controllers;

/// <summary>
/// Manages customer orders and their line items. Staff (Admin/User) can read every order;
/// writes require the <c>Admin</c> role. A caller with the <c>Customer</c> role can only see
/// their own orders. Creating or adding an item decrements the matching product's stock;
/// removing an item, cancelling, or deleting an order restores it.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public sealed class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    /// <summary>
    /// Retrieves every active order, including their line items. A caller with the
    /// <c>Customer</c> role only gets their own orders.
    /// </summary>
    /// <param name="cancellationToken">Propagates request cancellation.</param>
    /// <response code="200">The list of orders.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<OrderResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<OrderResponseDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        var orders = await _orderService.GetAllAsync(cancellationToken);

        if (User.IsCustomer())
        {
            var ownCustomerId = User.GetCustomerId();
            orders = [.. orders.Where(order => order.CustomerId == ownCustomerId)];
        }

        return Ok(orders);
    }

    /// <summary>
    /// Retrieves a single order by id, including its line items. A caller with the
    /// <c>Customer</c> role may only fetch their own orders.
    /// </summary>
    /// <param name="id">The order's unique identifier.</param>
    /// <param name="cancellationToken">Propagates request cancellation.</param>
    /// <response code="200">The matching order.</response>
    /// <response code="403">The caller has the <c>Customer</c> role and doesn't own this order.</response>
    /// <response code="404">No order exists with the given id.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderResponseDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var order = await _orderService.GetByIdAsync(id, cancellationToken);

        if (User.IsCustomer() && User.GetCustomerId() != order.CustomerId)
        {
            return Forbid();
        }

        return Ok(order);
    }

    /// <summary>
    /// Searches orders by an optional combination of customer, status and date range. A
    /// caller with the <c>Customer</c> role is always scoped to their own orders, regardless
    /// of the requested <see cref="OrderSearchDto.CustomerId"/>.
    /// </summary>
    /// <param name="filter">
    /// Filter criteria; any property left unset is ignored (backed by the "SearchOrders"
    /// PostgreSQL procedure).
    /// </param>
    /// <param name="cancellationToken">Propagates request cancellation.</param>
    /// <response code="200">The orders matching the filter (possibly empty).</response>
    [HttpGet("search")]
    [ProducesResponseType(typeof(IReadOnlyList<OrderSearchResultDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<OrderSearchResultDto>>> Search(
        [FromQuery] OrderSearchDto filter,
        CancellationToken cancellationToken)
    {
        if (User.IsCustomer())
        {
            filter.CustomerId = User.GetCustomerId();
        }

        var orders = await _orderService.SearchOrdersAsync(filter, cancellationToken);
        return Ok(orders);
    }

    /// <summary>
    /// Retrieves aggregate order statistics for a single customer. A caller with the
    /// <c>Customer</c> role may only request their own summary.
    /// </summary>
    /// <param name="customerId">The customer's unique identifier.</param>
    /// <param name="cancellationToken">Propagates request cancellation.</param>
    /// <response code="200">The customer's order summary.</response>
    /// <response code="403">The caller has the <c>Customer</c> role and <paramref name="customerId"/> isn't their own.</response>
    /// <response code="404">No customer exists with the given id.</response>
    [HttpGet("customers/{customerId:guid}/summary")]
    [ProducesResponseType(typeof(CustomerOrderSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerOrderSummaryDto>> GetCustomerOrderSummary(
        Guid customerId,
        CancellationToken cancellationToken)
    {
        if (User.IsCustomer() && User.GetCustomerId() != customerId)
        {
            return Forbid();
        }

        var summary = await _orderService.GetCustomerOrderSummaryAsync(customerId, cancellationToken);
        return Ok(summary);
    }

    /// <summary>
    /// Creates a new order with its initial line items. Requires the <c>Admin</c> role.
    /// </summary>
    /// <param name="dto">The customer, shipping address and line items for the new order.</param>
    /// <param name="cancellationToken">Propagates request cancellation.</param>
    /// <remarks>Decrements stock for every product in <paramref name="dto"/>.</remarks>
    /// <response code="201">The order was created.</response>
    /// <response code="400">The request body failed validation.</response>
    /// <response code="403">The caller is authenticated but not an Admin.</response>
    /// <response code="404">The customer or a referenced product does not exist.</response>
    /// <response code="409">A product does not have enough stock for the requested quantity.</response>
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [ProducesResponseType(typeof(OrderResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderResponseDto>> Create(
        [FromBody] OrderCreateDto dto,
        CancellationToken cancellationToken)
    {
        var order = await _orderService.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }

    /// <summary>
    /// Updates an order's status and shipping address. Requires the <c>Admin</c> role.
    /// </summary>
    /// <param name="id">The order's unique identifier.</param>
    /// <param name="dto">The new status and shipping address.</param>
    /// <param name="cancellationToken">Propagates request cancellation.</param>
    /// <remarks>Setting the status to <c>Cancelled</c> restores stock for every line item.</remarks>
    /// <response code="200">The updated order.</response>
    /// <response code="400">The request body failed validation.</response>
    /// <response code="403">The caller is authenticated but not an Admin.</response>
    /// <response code="404">No order exists with the given id.</response>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [ProducesResponseType(typeof(OrderResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderResponseDto>> Update(
        Guid id,
        [FromBody] OrderUpdateDto dto,
        CancellationToken cancellationToken)
    {
        var order = await _orderService.UpdateAsync(id, dto, cancellationToken);
        return Ok(order);
    }

    /// <summary>
    /// Soft-deletes an order. Requires the <c>Admin</c> role.
    /// </summary>
    /// <param name="id">The order's unique identifier.</param>
    /// <param name="cancellationToken">Propagates request cancellation.</param>
    /// <remarks>Restores stock for every line item unless the order was already cancelled.</remarks>
    /// <response code="204">The order was deleted.</response>
    /// <response code="403">The caller is authenticated but not an Admin.</response>
    /// <response code="404">No order exists with the given id.</response>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _orderService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Adds a line item to an existing order. Requires the <c>Admin</c> role.
    /// </summary>
    /// <param name="id">The order's unique identifier.</param>
    /// <param name="dto">The product and quantity to add.</param>
    /// <param name="cancellationToken">Propagates request cancellation.</param>
    /// <remarks>Decrements stock for the product. The order must not be cancelled.</remarks>
    /// <response code="200">The updated order.</response>
    /// <response code="400">The request body failed validation.</response>
    /// <response code="403">The caller is authenticated but not an Admin.</response>
    /// <response code="404">The order or the referenced product does not exist.</response>
    /// <response code="409">The order is cancelled, or the product lacks sufficient stock.</response>
    [HttpPost("{id:guid}/items")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [ProducesResponseType(typeof(OrderResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderResponseDto>> AddItem(
        Guid id,
        [FromBody] OrderItemCreateDto dto,
        CancellationToken cancellationToken)
    {
        var order = await _orderService.AddItemAsync(id, dto, cancellationToken);
        return Ok(order);
    }

    /// <summary>
    /// Removes a line item from an existing order. Requires the <c>Admin</c> role.
    /// </summary>
    /// <param name="id">The order's unique identifier.</param>
    /// <param name="itemId">The line item's unique identifier.</param>
    /// <param name="cancellationToken">Propagates request cancellation.</param>
    /// <remarks>Restores stock for the removed item's product. The order must not be cancelled.</remarks>
    /// <response code="200">The updated order.</response>
    /// <response code="403">The caller is authenticated but not an Admin.</response>
    /// <response code="404">The order or the line item does not exist.</response>
    /// <response code="409">The order is cancelled.</response>
    [HttpDelete("{id:guid}/items/{itemId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [ProducesResponseType(typeof(OrderResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderResponseDto>> RemoveItem(
        Guid id,
        Guid itemId,
        CancellationToken cancellationToken)
    {
        var order = await _orderService.RemoveItemAsync(id, itemId, cancellationToken);
        return Ok(order);
    }
}
