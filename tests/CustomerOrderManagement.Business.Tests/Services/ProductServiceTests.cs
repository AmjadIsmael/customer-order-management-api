using CustomerOrderManagement.Business.Exceptions;
using CustomerOrderManagement.Business.Interfaces.Persistence;
using CustomerOrderManagement.Business.Services;
using CustomerOrderManagement.Domain.DTOs.Products;
using CustomerOrderManagement.Domain.Entities;
using Moq;

namespace CustomerOrderManagement.Business.Tests.Services;

public sealed class ProductServiceTests
{
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly ProductService _sut;

    public ProductServiceTests()
    {
        _unitOfWork.Setup(u => u.Products).Returns(_productRepository.Object);
        _sut = new ProductService(_unitOfWork.Object);
    }

    [Fact]
    public async Task CreateAsync_AddsProductAndReturnsDto()
    {
        // Arrange
        var dto = new ProductCreateDto { Name = "Widget", Price = 9.99m, StockQuantity = 100 };

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        Assert.Equal(dto.Name, result.Name);
        Assert.Equal(dto.Price, result.Price);
        Assert.Equal(dto.StockQuantity, result.StockQuantity);
        _productRepository.Verify(
            r => r.AddAsync(It.Is<Product>(p => p.Name == dto.Name), It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WhenProductExists_ReturnsDto()
    {
        // Arrange
        var product = new Product { Id = Guid.NewGuid(), Name = "Gadget", Price = 19.99m, StockQuantity = 5 };

        _productRepository
            .Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        // Act
        var result = await _sut.GetByIdAsync(product.Id);

        // Assert
        Assert.Equal(product.Id, result.Id);
        Assert.Equal(product.Name, result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_WhenProductMissing_ThrowsNotFoundException()
    {
        // Arrange
        var id = Guid.NewGuid();

        _productRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        // Act
        var act = () => _sut.GetByIdAsync(id);

        // Assert
        await Assert.ThrowsAsync<NotFoundException>(act);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllProductsMappedToDtos()
    {
        // Arrange
        var products = new List<Product>
        {
            new() { Id = Guid.NewGuid(), Name = "A", Price = 1m, StockQuantity = 1 },
            new() { Id = Guid.NewGuid(), Name = "B", Price = 2m, StockQuantity = 2 },
        };

        _productRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task UpdateAsync_WhenProductExists_UpdatesAndReturnsDto()
    {
        // Arrange
        var product = new Product { Id = Guid.NewGuid(), Name = "Old", Price = 1m, StockQuantity = 1 };
        var dto = new ProductUpdateDto { Name = "New", Price = 2m, StockQuantity = 10 };

        _productRepository
            .Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        // Act
        var result = await _sut.UpdateAsync(product.Id, dto);

        // Assert
        Assert.Equal("New", result.Name);
        Assert.Equal(2m, result.Price);
        Assert.Equal(10, result.StockQuantity);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenProductMissing_ThrowsNotFoundException()
    {
        // Arrange
        var id = Guid.NewGuid();

        _productRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        // Act
        var act = () => _sut.UpdateAsync(id, new ProductUpdateDto());

        // Assert
        await Assert.ThrowsAsync<NotFoundException>(act);
    }

    [Fact]
    public async Task DeleteAsync_WhenProductExists_SoftDeletesProduct()
    {
        // Arrange
        var product = new Product { Id = Guid.NewGuid(), IsActive = true, IsDeleted = false };

        _productRepository
            .Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        // Act
        await _sut.DeleteAsync(product.Id);

        // Assert
        Assert.True(product.IsDeleted);
        Assert.False(product.IsActive);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenProductMissing_ThrowsNotFoundException()
    {
        // Arrange
        var id = Guid.NewGuid();

        _productRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        // Act
        var act = () => _sut.DeleteAsync(id);

        // Assert
        await Assert.ThrowsAsync<NotFoundException>(act);
    }
}
