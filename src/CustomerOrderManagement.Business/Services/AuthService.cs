using CustomerOrderManagement.Business.Exceptions;
using CustomerOrderManagement.Business.Interfaces.Persistence;
using CustomerOrderManagement.Business.Interfaces.Services;
using CustomerOrderManagement.Domain.DTOs.Auth;
using CustomerOrderManagement.Domain.Entities;
using CustomerOrderManagement.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace CustomerOrderManagement.Business.Services;

public sealed class AuthService : IAuthService
{
    private const string InvalidCredentialsMessage = "Invalid username or password.";

    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public AuthService(
        IUnitOfWork unitOfWork,
        IPasswordHasher<User> passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<LoginResponseDto> RegisterAsync(
        RegisterRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        if (await _unitOfWork.Users.GetByUsernameAsync(dto.Username, cancellationToken) is not null)
        {
            throw new ConflictException($"Username '{dto.Username}' is already taken.");
        }

        if (await _unitOfWork.Customers.EmailExistsAsync(dto.Email, cancellationToken: cancellationToken))
        {
            throw new ConflictException($"A customer with email '{dto.Email}' already exists.");
        }

        var now = DateTime.UtcNow;

        var customer = new Customer
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            PhoneNumber = dto.PhoneNumber,
            Address = dto.Address,
            Age = dto.Age,
            Gender = dto.Gender,
            CreatedDate = now,
            CreatedBy = dto.Username,
        };

        var user = new User
        {
            Username = dto.Username,
            Email = dto.Email,
            Role = UserRole.Customer,
            CustomerId = customer.Id,
            CreatedDate = now,
            CreatedBy = dto.Username,
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);

        await _unitOfWork.Customers.AddAsync(customer, cancellationToken);
        await _unitOfWork.Users.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var token = _jwtTokenGenerator.GenerateToken(user);

        return new LoginResponseDto
        {
            AccessToken = token.Value,
            ExpiresAtUtc = token.ExpiresAtUtc,
        };
    }

    public async Task<LoginResponseDto> LoginAsync(
        LoginRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByUsernameAsync(dto.Username, cancellationToken);

        if (user is null)
        {
            throw new UnauthorizedException(InvalidCredentialsMessage);
        }

        var verificationResult = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            dto.Password);

        if (verificationResult == PasswordVerificationResult.Failed)
        {
            throw new UnauthorizedException(InvalidCredentialsMessage);
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedException("This account has been deactivated.");
        }

        var token = _jwtTokenGenerator.GenerateToken(user);

        return new LoginResponseDto
        {
            AccessToken = token.Value,
            ExpiresAtUtc = token.ExpiresAtUtc,
        };
    }
}
