using CustomerOrderManagement.Business.Exceptions;
using CustomerOrderManagement.Business.Interfaces.Persistence;
using CustomerOrderManagement.Business.Interfaces.Services;
using CustomerOrderManagement.Business.Models;
using CustomerOrderManagement.Business.Services;
using CustomerOrderManagement.Domain.DTOs.Auth;
using CustomerOrderManagement.Domain.Entities;
using CustomerOrderManagement.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace CustomerOrderManagement.Business.Tests.Services;

public sealed class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<ICustomerRepository> _customerRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IPasswordHasher<User>> _passwordHasher = new();
    private readonly Mock<IJwtTokenGenerator> _jwtTokenGenerator = new();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _unitOfWork.Setup(u => u.Users).Returns(_userRepository.Object);
        _unitOfWork.Setup(u => u.Customers).Returns(_customerRepository.Object);
        _sut = new AuthService(_unitOfWork.Object, _passwordHasher.Object, _jwtTokenGenerator.Object);
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsAccessToken()
    {
        // Arrange
        var dto = new LoginRequestDto { Username = "admin", Password = "Admin@123" };
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "admin",
            PasswordHash = "hashed",
            Role = UserRole.Admin,
            IsActive = true,
        };
        var token = new JwtToken("signed-jwt", DateTime.UtcNow.AddHours(1));

        _userRepository
            .Setup(r => r.GetByUsernameAsync(dto.Username, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasher
            .Setup(h => h.VerifyHashedPassword(user, user.PasswordHash, dto.Password))
            .Returns(PasswordVerificationResult.Success);
        _jwtTokenGenerator.Setup(g => g.GenerateToken(user)).Returns(token);

        // Act
        var result = await _sut.LoginAsync(dto);

        // Assert
        Assert.Equal(token.Value, result.AccessToken);
        Assert.Equal(token.ExpiresAtUtc, result.ExpiresAtUtc);
    }

    [Fact]
    public async Task LoginAsync_WhenUsernameDoesNotExist_ThrowsUnauthorizedException()
    {
        // Arrange
        var dto = new LoginRequestDto { Username = "ghost", Password = "whatever" };

        _userRepository
            .Setup(r => r.GetByUsernameAsync(dto.Username, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var act = () => _sut.LoginAsync(dto);

        // Assert
        await Assert.ThrowsAsync<UnauthorizedException>(act);
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ThrowsUnauthorizedException()
    {
        // Arrange
        var dto = new LoginRequestDto { Username = "admin", Password = "wrong" };
        var user = new User { Username = "admin", PasswordHash = "hashed", IsActive = true };

        _userRepository
            .Setup(r => r.GetByUsernameAsync(dto.Username, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasher
            .Setup(h => h.VerifyHashedPassword(user, user.PasswordHash, dto.Password))
            .Returns(PasswordVerificationResult.Failed);

        // Act
        var act = () => _sut.LoginAsync(dto);

        // Assert
        await Assert.ThrowsAsync<UnauthorizedException>(act);
        _jwtTokenGenerator.Verify(g => g.GenerateToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WhenAccountDeactivated_ThrowsUnauthorizedException()
    {
        // Arrange  
        var dto = new LoginRequestDto { Username = "admin", Password = "Admin@123" };
        var user = new User { Username = "admin", PasswordHash = "hashed", IsActive = false };

        _userRepository
            .Setup(r => r.GetByUsernameAsync(dto.Username, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasher
            .Setup(h => h.VerifyHashedPassword(user, user.PasswordHash, dto.Password))
            .Returns(PasswordVerificationResult.Success);

        // Act
        var act = () => _sut.LoginAsync(dto);

        // Assert
        await Assert.ThrowsAsync<UnauthorizedException>(act);
        _jwtTokenGenerator.Verify(g => g.GenerateToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_WithNewUsernameAndEmail_CreatesCustomerAndUserAndReturnsToken()
    {
        // Arrange
        var dto = new RegisterRequestDto
        {
            Username = "newcustomer",
            Password = "Password1",
            FirstName = "Jane",
            LastName = "Doe",
            Email = "jane@example.com",
        };
        var token = new JwtToken("signed-jwt", DateTime.UtcNow.AddHours(1));

        _userRepository
            .Setup(r => r.GetByUsernameAsync(dto.Username, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _customerRepository
            .Setup(r => r.EmailExistsAsync(dto.Email, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _passwordHasher
            .Setup(h => h.HashPassword(It.IsAny<User>(), dto.Password))
            .Returns("hashed-password");
        _jwtTokenGenerator
            .Setup(g => g.GenerateToken(It.IsAny<User>()))
            .Returns(token);

        // Act
        var result = await _sut.RegisterAsync(dto);

        // Assert
        Assert.Equal(token.Value, result.AccessToken);
        _customerRepository.Verify(
            r => r.AddAsync(
                It.Is<Customer>(c => c.Email == dto.Email && c.FirstName == dto.FirstName),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _userRepository.Verify(
            r => r.AddAsync(
                It.Is<User>(u =>
                    u.Username == dto.Username &&
                    u.Role == UserRole.Customer &&
                    u.PasswordHash == "hashed-password" &&
                    u.CustomerId != null),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_WithTakenUsername_ThrowsConflictExceptionAndDoesNotSave()
    {
        // Arrange
        var dto = new RegisterRequestDto { Username = "admin", Email = "new@example.com" };

        _userRepository
            .Setup(r => r.GetByUsernameAsync(dto.Username, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Username = "admin" });

        // Act
        var act = () => _sut.RegisterAsync(dto);

        // Assert
        await Assert.ThrowsAsync<ConflictException>(act);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_WithTakenEmail_ThrowsConflictExceptionAndDoesNotSave()
    {
        // Arrange
        var dto = new RegisterRequestDto { Username = "newuser", Email = "taken@example.com" };

        _userRepository
            .Setup(r => r.GetByUsernameAsync(dto.Username, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _customerRepository
            .Setup(r => r.EmailExistsAsync(dto.Email, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var act = () => _sut.RegisterAsync(dto);
        // Assert
        await Assert.ThrowsAsync<ConflictException>(act);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
