using CustomerOrderManagement.Business.Exceptions;
using CustomerOrderManagement.Business.Interfaces.Persistence;
using CustomerOrderManagement.Business.Interfaces.Services;
using CustomerOrderManagement.Domain.DTOs.Auth;
using CustomerOrderManagement.Domain.Entities;
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

    public async Task<LoginResponseDto> LoginAsync(
        LoginRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByUsernameAsync(dto.Username, cancellationToken);

        if (user is null)
        {
            // Same error for "unknown username" and "wrong password" below —
            // don't let a client learn which usernames exist.
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
