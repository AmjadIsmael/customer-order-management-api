using CustomerOrderManagement.Business.Exceptions;
using CustomerOrderManagement.Business.Interfaces.Persistence;
using CustomerOrderManagement.Business.Services;
using CustomerOrderManagement.Domain.DTOs.Customers;
using CustomerOrderManagement.Domain.Entities;
using Moq;

namespace CustomerOrderManagement.Business.Tests.Services;

public sealed class CustomerServiceTests
{
    private readonly Mock<ICustomerRepository> _customerRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly CustomerService _sut;

    public CustomerServiceTests()
    {
        _unitOfWork.Setup(u => u.Customers).Returns(_customerRepository.Object);
        _sut = new CustomerService(_unitOfWork.Object);
    }

    [Fact]
    public async Task CreateAsync_WithUniqueEmail_AddsCustomerAndReturnsDto()
    {
        // Arrange
        var dto = new CustomerCreateDto
        {
            FirstName = "Ada",
            LastName = "Lovelace",
            Email = "ada@example.com",
        };

        _customerRepository
            .Setup(r => r.EmailExistsAsync(dto.Email, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        Assert.Equal(dto.Email, result.Email);
        Assert.Equal(dto.FirstName, result.FirstName);
        _customerRepository.Verify(
            r => r.AddAsync(It.Is<Customer>(c => c.Email == dto.Email), It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateEmail_ThrowsConflictExceptionAndDoesNotAdd()
    {
        // Arrange
        var dto = new CustomerCreateDto { Email = "taken@example.com" };

        _customerRepository
            .Setup(r => r.EmailExistsAsync(dto.Email, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var act = () => _sut.CreateAsync(dto);

        // Assert
        await Assert.ThrowsAsync<ConflictException>(act);
        _customerRepository.Verify(
            r => r.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCustomerExists_ReturnsDto()
    {
        // Arrange
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            FirstName = "Grace",
            LastName = "Hopper",
            Email = "grace@example.com",
        };

        _customerRepository
            .Setup(r => r.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        // Act
        var result = await _sut.GetByIdAsync(customer.Id);

        // Assert
        Assert.Equal(customer.Id, result.Id);
        Assert.Equal(customer.Email, result.Email);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCustomerMissing_ThrowsNotFoundException()
    {
        // Arrange
        var id = Guid.NewGuid();

        _customerRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        // Act
        var act = () => _sut.GetByIdAsync(id);

        // Assert
        await Assert.ThrowsAsync<NotFoundException>(act);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllCustomersMappedToDtos()
    {
        // Arrange
        var customers = new List<Customer>
        {
            new() { Id = Guid.NewGuid(), FirstName = "A", LastName = "One", Email = "a@example.com" },
            new() { Id = Guid.NewGuid(), FirstName = "B", LastName = "Two", Email = "b@example.com" },
        };

        _customerRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(customers);

        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, c => c.Email == "a@example.com");
        Assert.Contains(result, c => c.Email == "b@example.com");
    }

    [Fact]
    public async Task UpdateAsync_WhenCustomerExistsAndEmailIsFree_UpdatesAndReturnsDto()
    {
        // Arrange
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            FirstName = "Old",
            LastName = "Name",
            Email = "old@example.com",
        };
        var dto = new CustomerUpdateDto { FirstName = "New", LastName = "Name", Email = "new@example.com" };

        _customerRepository
            .Setup(r => r.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);
        _customerRepository
            .Setup(r => r.EmailExistsAsync(dto.Email, customer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.UpdateAsync(customer.Id, dto);

        // Assert
        Assert.Equal("New", result.FirstName);
        Assert.Equal("new@example.com", result.Email);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenCustomerMissing_ThrowsNotFoundException()
    {
        // Arrange
        var id = Guid.NewGuid();

        _customerRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        // Act
        var act = () => _sut.UpdateAsync(id, new CustomerUpdateDto());

        // Assert
        await Assert.ThrowsAsync<NotFoundException>(act);
    }

    [Fact]
    public async Task UpdateAsync_WhenEmailTakenByAnotherCustomer_ThrowsConflictException()
    {
        // Arrange
        var customer = new Customer { Id = Guid.NewGuid(), Email = "old@example.com" };
        var dto = new CustomerUpdateDto { Email = "taken@example.com" };

        _customerRepository
            .Setup(r => r.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);
        _customerRepository
            .Setup(r => r.EmailExistsAsync(dto.Email, customer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var act = () => _sut.UpdateAsync(customer.Id, dto);

        // Assert
        await Assert.ThrowsAsync<ConflictException>(act);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenCustomerExists_SoftDeletesCustomer()
    {
        // Arrange
        var customer = new Customer { Id = Guid.NewGuid(), IsActive = true, IsDeleted = false };

        _customerRepository
            .Setup(r => r.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        // Act
        await _sut.DeleteAsync(customer.Id);

        // Assert
        Assert.True(customer.IsDeleted);
        Assert.False(customer.IsActive);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenCustomerMissing_ThrowsNotFoundException()
    {
        // Arrange
        var id = Guid.NewGuid();

        _customerRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        // Act
        var act = () => _sut.DeleteAsync(id);

        // Assert
        await Assert.ThrowsAsync<NotFoundException>(act);
    }
}
