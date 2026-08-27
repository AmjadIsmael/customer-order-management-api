using CustomerOrderManagement.Business.Exceptions;
using CustomerOrderManagement.Business.Interfaces.Persistence;
using CustomerOrderManagement.Business.Services;
using CustomerOrderManagement.Domain.DTOs.Orders;
using CustomerOrderManagement.Domain.Entities;
using CustomerOrderManagement.Domain.Enums;
using Moq;

namespace CustomerOrderManagement.Business.Tests.Services;

public sealed class OrderServiceTests
{
    private readonly Mock<ICustomerRepository> _customerRepository = new();
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<IOrderRepository> _orderRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly OrderService _sut;

    public OrderServiceTests()
    {
        _unitOfWork.Setup(u => u.Customers).Returns(_customerRepository.Object);
        _unitOfWork.Setup(u => u.Products).Returns(_productRepository.Object);
        _unitOfWork.Setup(u => u.Orders).Returns(_orderRepository.Object);
        _sut = new OrderService(_unitOfWork.Object);
    }

    private static Product MakeProduct(int stock = 10, decimal price = 5m) => new()
    {
        Id = Guid.NewGuid(),
        Name = "Widget",
        Price = price,
        StockQuantity = stock,
    };

    // ---- CreateAsync ----

    [Fact]
    public async Task CreateAsync_WithValidCustomerAndSufficientStock_CreatesOrderAndCommitsTransaction()
    {
        // Arrange
        var customer = new Customer { Id = Guid.NewGuid() };
        var product = MakeProduct(stock: 10, price: 5m);
        var dto = new OrderCreateDto
        {
            CustomerId = customer.Id,
            Items = [new OrderItemCreateDto { ProductId = product.Id, Quantity = 3 }],
        };

        _customerRepository.Setup(r => r.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>())).ReturnsAsync(customer);
        _productRepository.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        Assert.Equal(customer.Id, result.CustomerId);
        Assert.Equal(15m, result.TotalAmount);
        Assert.Equal(7, product.StockQuantity);
        _unitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _orderRepository.Verify(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenCustomerMissing_ThrowsNotFoundExceptionWithoutStartingTransaction()
    {
        // Arrange
        var dto = new OrderCreateDto { CustomerId = Guid.NewGuid(), Items = [] };

        _customerRepository
            .Setup(r => r.GetByIdAsync(dto.CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        // Act
        var act = () => _sut.CreateAsync(dto);

        // Assert
        await Assert.ThrowsAsync<NotFoundException>(act);
        _unitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenProductMissing_ThrowsNotFoundExceptionAndRollsBackTransaction()
    {
        // Arrange
        var customer = new Customer { Id = Guid.NewGuid() };
        var missingProductId = Guid.NewGuid();
        var dto = new OrderCreateDto
        {
            CustomerId = customer.Id,
            Items = [new OrderItemCreateDto { ProductId = missingProductId, Quantity = 1 }],
        };

        _customerRepository.Setup(r => r.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>())).ReturnsAsync(customer);
        _productRepository
            .Setup(r => r.GetByIdAsync(missingProductId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        // Act
        var act = () => _sut.CreateAsync(dto);

        // Assert
        await Assert.ThrowsAsync<NotFoundException>(act);
        _unitOfWork.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenInsufficientStock_ThrowsConflictExceptionAndRollsBackTransaction()
    {
        // Arrange
        var customer = new Customer { Id = Guid.NewGuid() };
        var product = MakeProduct(stock: 1);
        var dto = new OrderCreateDto
        {
            CustomerId = customer.Id,
            Items = [new OrderItemCreateDto { ProductId = product.Id, Quantity = 5 }],
        };

        _customerRepository.Setup(r => r.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>())).ReturnsAsync(customer);
        _productRepository.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        // Act
        var act = () => _sut.CreateAsync(dto);

        // Assert
        await Assert.ThrowsAsync<ConflictException>(act);
        _unitOfWork.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(1, product.StockQuantity); // untouched
    }

    // ---- GetByIdAsync / GetAllAsync ----

    [Fact]
    public async Task GetByIdAsync_WhenOrderExists_ReturnsDto()
    {
        // Arrange
        var order = new Order { Id = Guid.NewGuid(), CustomerId = Guid.NewGuid(), Status = OrderStatus.Pending };

        _orderRepository.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        // Act
        var result = await _sut.GetByIdAsync(order.Id);

        // Assert
        Assert.Equal(order.Id, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WhenOrderMissing_ThrowsNotFoundException()
    {
        // Arrange
        var id = Guid.NewGuid();

        _orderRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Order?)null);

        // Act
        var act = () => _sut.GetByIdAsync(id);

        // Assert
        await Assert.ThrowsAsync<NotFoundException>(act);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllOrdersMappedToDtos()
    {
        // Arrange
        var orders = new List<Order>
        {
            new() { Id = Guid.NewGuid(), CustomerId = Guid.NewGuid() },
            new() { Id = Guid.NewGuid(), CustomerId = Guid.NewGuid() },
        };

        _orderRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(orders);

        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        Assert.Equal(2, result.Count);
    }

    // ---- UpdateAsync ----

    [Fact]
    public async Task UpdateAsync_WhenCancellingAPendingOrder_RestoresStockForEveryItem()
    {
        // Arrange
        var product = MakeProduct(stock: 2);
        var order = new Order
        {
            Id = Guid.NewGuid(),
            Status = OrderStatus.Pending,
            Items = [new OrderItem { Id = Guid.NewGuid(), Product = product, ProductId = product.Id, Quantity = 3, IsDeleted = false }],
        };
        var dto = new OrderUpdateDto { Status = OrderStatus.Cancelled };

        _orderRepository.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _productRepository.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        // Act
        var result = await _sut.UpdateAsync(order.Id, dto);

        // Assert
        Assert.Equal(OrderStatus.Cancelled, result.Status);
        Assert.Equal(5, product.StockQuantity); // 2 + 3 restored
    }

    [Fact]
    public async Task UpdateAsync_WhenNotChangingToCancelled_DoesNotTouchStock()
    {
        // Arrange
        var product = MakeProduct(stock: 2);
        var order = new Order
        {
            Id = Guid.NewGuid(),
            Status = OrderStatus.Pending,
            Items = [new OrderItem { Id = Guid.NewGuid(), Product = product, ProductId = product.Id, Quantity = 3 }],
        };
        var dto = new OrderUpdateDto { Status = OrderStatus.Shipped };

        _orderRepository.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        // Act
        var result = await _sut.UpdateAsync(order.Id, dto);

        // Assert
        Assert.Equal(OrderStatus.Shipped, result.Status);
        Assert.Equal(2, product.StockQuantity);
        _productRepository.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenOrderMissing_ThrowsNotFoundException()
    {
        // Arrange
        var id = Guid.NewGuid();

        _orderRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Order?)null);

        // Act
        var act = () => _sut.UpdateAsync(id, new OrderUpdateDto());

        // Assert
        await Assert.ThrowsAsync<NotFoundException>(act);
    }

    // ---- DeleteAsync ----

    [Fact]
    public async Task DeleteAsync_WhenOrderNotAlreadyCancelled_RestoresStockAndCancelsAndSoftDeletes()
    {
        // Arrange
        var product = MakeProduct(stock: 0);
        var order = new Order
        {
            Id = Guid.NewGuid(),
            Status = OrderStatus.Pending,
            Items = [new OrderItem { Id = Guid.NewGuid(), Product = product, ProductId = product.Id, Quantity = 4 }],
        };

        _orderRepository.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _productRepository.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        // Act
        await _sut.DeleteAsync(order.Id);

        // Assert
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.True(order.IsDeleted);
        Assert.Equal(4, product.StockQuantity);
    }

    [Fact]
    public async Task DeleteAsync_WhenOrderAlreadyCancelled_DoesNotDoubleRestoreStock()
    {
        // Arrange
        var product = MakeProduct(stock: 4);
        var order = new Order
        {
            Id = Guid.NewGuid(),
            Status = OrderStatus.Cancelled,
            Items = [new OrderItem { Id = Guid.NewGuid(), Product = product, ProductId = product.Id, Quantity = 4 }],
        };

        _orderRepository.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        // Act
        await _sut.DeleteAsync(order.Id);

        // Assert
        Assert.Equal(4, product.StockQuantity); // unchanged, already cancelled once
        _productRepository.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenOrderMissing_ThrowsNotFoundException()
    {
        // Arrange
        var id = Guid.NewGuid();

        _orderRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Order?)null);

        // Act
        var act = () => _sut.DeleteAsync(id);

        // Assert
        await Assert.ThrowsAsync<NotFoundException>(act);
    }

    // ---- AddItemAsync ----

    [Fact]
    public async Task AddItemAsync_WithSufficientStock_AddsItemAndRecalculatesTotal()
    {
        // Arrange
        var existingProduct = MakeProduct(stock: 10, price: 2m);
        var newProduct = MakeProduct(stock: 10, price: 3m);
        var order = new Order
        {
            Id = Guid.NewGuid(),
            Status = OrderStatus.Pending,
            Items = [new OrderItem { Id = Guid.NewGuid(), Product = existingProduct, ProductId = existingProduct.Id, Quantity = 1, UnitPrice = 2m }],
            TotalAmount = 2m,
        };
        var dto = new OrderItemCreateDto { ProductId = newProduct.Id, Quantity = 2 };

        _orderRepository.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _productRepository.Setup(r => r.GetByIdAsync(newProduct.Id, It.IsAny<CancellationToken>())).ReturnsAsync(newProduct);

        // Act
        var result = await _sut.AddItemAsync(order.Id, dto);

        // Assert
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(8m, result.TotalAmount); // 2 (existing) + 2*3 (new)
        Assert.Equal(8, newProduct.StockQuantity);
    }

    [Fact]
    public async Task AddItemAsync_WhenOrderIsCancelled_ThrowsConflictException()
    {
        // Arrange
        var order = new Order { Id = Guid.NewGuid(), Status = OrderStatus.Cancelled, Items = [] };

        _orderRepository.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        // Act
        var act = () => _sut.AddItemAsync(order.Id, new OrderItemCreateDto { ProductId = Guid.NewGuid(), Quantity = 1 });

        // Assert
        await Assert.ThrowsAsync<ConflictException>(act);
    }

    [Fact]
    public async Task AddItemAsync_WhenOrderMissing_ThrowsNotFoundException()
    {
        // Arrange
        var id = Guid.NewGuid();

        _orderRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Order?)null);

        // Act
        var act = () => _sut.AddItemAsync(id, new OrderItemCreateDto { ProductId = Guid.NewGuid(), Quantity = 1 });

        // Assert
        await Assert.ThrowsAsync<NotFoundException>(act);
    }

    [Fact]
    public async Task AddItemAsync_WhenProductMissing_ThrowsNotFoundException()
    {
        // Arrange
        var order = new Order { Id = Guid.NewGuid(), Status = OrderStatus.Pending, Items = [] };
        var missingProductId = Guid.NewGuid();

        _orderRepository.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _productRepository
            .Setup(r => r.GetByIdAsync(missingProductId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        // Act
        var act = () => _sut.AddItemAsync(order.Id, new OrderItemCreateDto { ProductId = missingProductId, Quantity = 1 });

        // Assert
        await Assert.ThrowsAsync<NotFoundException>(act);
    }

    [Fact]
    public async Task AddItemAsync_WhenInsufficientStock_ThrowsConflictException()
    {
        // Arrange
        var product = MakeProduct(stock: 1);
        var order = new Order { Id = Guid.NewGuid(), Status = OrderStatus.Pending, Items = [] };

        _orderRepository.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _productRepository.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        // Act
        var act = () => _sut.AddItemAsync(order.Id, new OrderItemCreateDto { ProductId = product.Id, Quantity = 5 });

        // Assert
        await Assert.ThrowsAsync<ConflictException>(act);
    }

    // ---- RemoveItemAsync ----

    [Fact]
    public async Task RemoveItemAsync_WhenItemExists_RestoresStockAndRecalculatesTotal()
    {
        // Arrange
        var product = MakeProduct(stock: 3, price: 2m);
        var item = new OrderItem { Id = Guid.NewGuid(), Product = product, ProductId = product.Id, Quantity = 2, UnitPrice = 2m };
        var order = new Order
        {
            Id = Guid.NewGuid(),
            Status = OrderStatus.Pending,
            Items = [item],
            TotalAmount = 4m,
        };

        _orderRepository.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _productRepository.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        // Act
        var result = await _sut.RemoveItemAsync(order.Id, item.Id);

        // Assert
        Assert.True(item.IsDeleted);
        Assert.Equal(0m, result.TotalAmount);
        Assert.Equal(5, product.StockQuantity); // 3 + 2 restored
    }

    [Fact]
    public async Task RemoveItemAsync_WhenItemMissing_ThrowsNotFoundException()
    {
        // Arrange
        var order = new Order { Id = Guid.NewGuid(), Status = OrderStatus.Pending, Items = [] };

        _orderRepository.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        // Act
        var act = () => _sut.RemoveItemAsync(order.Id, Guid.NewGuid());

        // Assert
        await Assert.ThrowsAsync<NotFoundException>(act);
    }

    [Fact]
    public async Task RemoveItemAsync_WhenOrderIsCancelled_ThrowsConflictException()
    {
        // Arrange
        var item = new OrderItem { Id = Guid.NewGuid() };
        var order = new Order { Id = Guid.NewGuid(), Status = OrderStatus.Cancelled, Items = [item] };

        _orderRepository.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        // Act
        var act = () => _sut.RemoveItemAsync(order.Id, item.Id);

        // Assert
        await Assert.ThrowsAsync<ConflictException>(act);
    }

    // ---- GetCustomerOrderSummaryAsync ----

    [Fact]
    public async Task GetCustomerOrderSummaryAsync_WhenCustomerExists_ReturnsSummary()
    {
        // Arrange
        var customer = new Customer { Id = Guid.NewGuid() };
        var summary = new CustomerOrderSummaryDto { CustomerId = customer.Id, TotalOrders = 3, TotalSpent = 100m };

        _customerRepository.Setup(r => r.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>())).ReturnsAsync(customer);
        _orderRepository
            .Setup(r => r.GetCustomerOrderSummaryAsync(customer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);

        // Act
        var result = await _sut.GetCustomerOrderSummaryAsync(customer.Id);

        // Assert
        Assert.Equal(3, result.TotalOrders);
        Assert.Equal(100m, result.TotalSpent);
    }

    [Fact]
    public async Task GetCustomerOrderSummaryAsync_WhenCustomerMissing_ThrowsNotFoundException()
    {
        // Arrange
        var id = Guid.NewGuid();

        _customerRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Customer?)null);

        // Act
        var act = () => _sut.GetCustomerOrderSummaryAsync(id);

        // Assert
        await Assert.ThrowsAsync<NotFoundException>(act);
        _orderRepository.Verify(
            r => r.GetCustomerOrderSummaryAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ---- SearchOrdersAsync ----

    [Fact]
    public async Task SearchOrdersAsync_DelegatesFilterToRepository()
    {
        // Arrange
        var filter = new OrderSearchDto { Status = OrderStatus.Pending };
        var expected = new List<OrderSearchResultDto> { new() { Id = Guid.NewGuid() } };

        _orderRepository
            .Setup(r => r.SearchOrdersAsync(filter, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _sut.SearchOrdersAsync(filter);

        // Assert
        Assert.Same(expected, result);
    }

    // ---- ExpireStalePendingOrdersAsync ----

    [Fact]
    public async Task ExpireStalePendingOrdersAsync_WithNoStaleOrders_ReturnsEmptyListWithoutSaving()
    {
        // Arrange
        _orderRepository
            .Setup(r => r.GetStalePendingAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        var result = await _sut.ExpireStalePendingOrdersAsync(TimeSpan.FromHours(24));

        // Assert
        Assert.Empty(result);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExpireStalePendingOrdersAsync_WithStaleOrders_CancelsThemAndRestoresStock()
    {
        // Arrange
        var product = MakeProduct(stock: 0);
        var staleOrder = new Order
        {
            Id = Guid.NewGuid(),
            Status = OrderStatus.Pending,
            Items = [new OrderItem { Id = Guid.NewGuid(), Product = product, ProductId = product.Id, Quantity = 2 }],
        };

        _orderRepository
            .Setup(r => r.GetStalePendingAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([staleOrder]);
        _productRepository.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        // Act
        var result = await _sut.ExpireStalePendingOrdersAsync(TimeSpan.FromHours(24));

        // Assert
        Assert.Single(result);
        Assert.Equal(staleOrder.Id, result[0]);
        Assert.Equal(OrderStatus.Cancelled, staleOrder.Status);
        Assert.Equal(2, product.StockQuantity);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
