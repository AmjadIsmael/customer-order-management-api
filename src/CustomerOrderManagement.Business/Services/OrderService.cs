using CustomerOrderManagement.Business.Exceptions;
using CustomerOrderManagement.Business.Interfaces.Persistence;
using CustomerOrderManagement.Business.Interfaces.Services;
using CustomerOrderManagement.Business.Mapping;
using CustomerOrderManagement.Domain.DTOs.Orders;
using CustomerOrderManagement.Domain.Entities;
using CustomerOrderManagement.Domain.Enums;

namespace CustomerOrderManagement.Business.Services;

public sealed class OrderService : IOrderService
{
    private const string SystemUser = "system";

    private readonly IUnitOfWork _unitOfWork;

    public OrderService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<OrderResponseDto> CreateAsync(
        OrderCreateDto dto,
        CancellationToken cancellationToken = default)
    {
        _ = await _unitOfWork.Customers.GetByIdAsync(dto.CustomerId, cancellationToken)
            ?? throw new NotFoundException($"Customer '{dto.CustomerId}' was not found.");

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var items = new List<OrderItem>();

            foreach (var line in dto.Items)
            {
                var product = await _unitOfWork.Products.GetByIdAsync(line.ProductId, cancellationToken)
                    ?? throw new NotFoundException($"Product '{line.ProductId}' was not found.");

                DecrementStock(product, line.Quantity);

                items.Add(new OrderItem
                {
                    Product = product,
                    ProductId = product.Id,
                    Quantity = line.Quantity,
                    UnitPrice = product.Price,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = SystemUser,
                });
            }

            var order = new Order
            {
                CustomerId = dto.CustomerId,
                ShippingAddress = dto.ShippingAddress,
                Status = OrderStatus.Pending,
                Items = items,
                TotalAmount = CalculateTotal(items),
                CreatedDate = DateTime.UtcNow,
                CreatedBy = SystemUser,
            };

            await _unitOfWork.Orders.AddAsync(order, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return order.ToResponseDto();
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task<OrderResponseDto> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var order = await _unitOfWork.Orders.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Order '{id}' was not found.");

        return order.ToResponseDto();
    }

    public async Task<IReadOnlyList<OrderResponseDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var orders = await _unitOfWork.Orders.GetAllAsync(cancellationToken);

        return orders
            .Select(order => order.ToResponseDto())
            .ToList();
    }

    public async Task<OrderResponseDto> UpdateAsync(
        Guid id,
        OrderUpdateDto dto,
        CancellationToken cancellationToken = default)
    {
        var order = await _unitOfWork.Orders.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Order '{id}' was not found.");

        if (dto.Status == OrderStatus.Cancelled && order.Status != OrderStatus.Cancelled)
        {
            await RestoreStockAsync(order, cancellationToken);
        }

        order.Status = dto.Status;
        order.ShippingAddress = dto.ShippingAddress;
        order.UpdatedDate = DateTime.UtcNow;
        order.UpdatedBy = SystemUser;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return order.ToResponseDto();
    }

    public async Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var order = await _unitOfWork.Orders.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Order '{id}' was not found.");

        if (order.Status != OrderStatus.Cancelled)
        {
            await RestoreStockAsync(order, cancellationToken);
            order.Status = OrderStatus.Cancelled;
        }

        order.IsDeleted = true;
        order.IsActive = false;
        order.UpdatedDate = DateTime.UtcNow;
        order.UpdatedBy = SystemUser;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<OrderResponseDto> AddItemAsync(
        Guid orderId,
        OrderItemCreateDto dto,
        CancellationToken cancellationToken = default)
    {
        var order = await _unitOfWork.Orders.GetByIdAsync(orderId, cancellationToken)
            ?? throw new NotFoundException($"Order '{orderId}' was not found.");

        EnsureModifiable(order);

        var product = await _unitOfWork.Products.GetByIdAsync(dto.ProductId, cancellationToken)
            ?? throw new NotFoundException($"Product '{dto.ProductId}' was not found.");

        DecrementStock(product, dto.Quantity);

        order.Items.Add(new OrderItem
        {
            OrderId = order.Id,
            Product = product,
            ProductId = product.Id,
            Quantity = dto.Quantity,
            UnitPrice = product.Price,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = SystemUser,
        });

        order.TotalAmount = CalculateTotal(order.Items);
        order.UpdatedDate = DateTime.UtcNow;
        order.UpdatedBy = SystemUser;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return order.ToResponseDto();
    }

    public async Task<OrderResponseDto> RemoveItemAsync(
        Guid orderId,
        Guid itemId,
        CancellationToken cancellationToken = default)
    {
        var order = await _unitOfWork.Orders.GetByIdAsync(orderId, cancellationToken)
            ?? throw new NotFoundException($"Order '{orderId}' was not found.");

        EnsureModifiable(order);

        var item = order.Items.FirstOrDefault(i => i.Id == itemId && !i.IsDeleted)
            ?? throw new NotFoundException($"Order item '{itemId}' was not found.");

        var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId, cancellationToken);
        if (product is not null)
        {
            product.StockQuantity += item.Quantity;
            product.UpdatedDate = DateTime.UtcNow;
            product.UpdatedBy = SystemUser;
        }

        item.IsDeleted = true;
        item.IsActive = false;
        item.UpdatedDate = DateTime.UtcNow;
        item.UpdatedBy = SystemUser;

        order.TotalAmount = CalculateTotal(order.Items);
        order.UpdatedDate = DateTime.UtcNow;
        order.UpdatedBy = SystemUser;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return order.ToResponseDto();
    }

    private static void EnsureModifiable(Order order)
    {
        if (order.Status == OrderStatus.Cancelled)
        {
            throw new ConflictException("Cannot modify a cancelled order.");
        }
    }

    private static void DecrementStock(Product product, int quantity)
    {
        if (product.StockQuantity < quantity)
        {
            throw new ConflictException(
                $"Insufficient stock for product '{product.Name}'. " +
                $"Requested {quantity}, available {product.StockQuantity}.");
        }

        product.StockQuantity -= quantity;
        product.UpdatedDate = DateTime.UtcNow;
        product.UpdatedBy = SystemUser;
    }

    private async Task RestoreStockAsync(Order order, CancellationToken cancellationToken)
    {
        foreach (var item in order.Items.Where(i => !i.IsDeleted))
        {
            var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId, cancellationToken);
            if (product is null)
            {
                continue;
            }

            product.StockQuantity += item.Quantity;
            product.UpdatedDate = DateTime.UtcNow;
            product.UpdatedBy = SystemUser;
        }
    }

    public async Task<CustomerOrderSummaryDto> GetCustomerOrderSummaryAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        _ = await _unitOfWork.Customers.GetByIdAsync(customerId, cancellationToken)
            ?? throw new NotFoundException($"Customer '{customerId}' was not found.");

        return await _unitOfWork.Orders.GetCustomerOrderSummaryAsync(customerId, cancellationToken);
    }

    public async Task<IReadOnlyList<OrderSearchResultDto>> SearchOrdersAsync(
        OrderSearchDto filter,
        CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.Orders.SearchOrdersAsync(filter, cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> ExpireStalePendingOrdersAsync(
        TimeSpan pendingThreshold,
        CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow - pendingThreshold;

        var staleOrders = await _unitOfWork.Orders.GetStalePendingAsync(cutoff, cancellationToken);
        if (staleOrders.Count == 0)
        {
            return [];
        }

        foreach (var order in staleOrders)
        {
            await RestoreStockAsync(order, cancellationToken);

            order.Status = OrderStatus.Cancelled;
            order.UpdatedDate = DateTime.UtcNow;
            order.UpdatedBy = SystemUser;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return [.. staleOrders.Select(order => order.Id)];
    }

    private static decimal CalculateTotal(IEnumerable<OrderItem> items)
    {
        return items
            .Where(item => !item.IsDeleted)
            .Sum(item => item.UnitPrice * item.Quantity);
    }
}

