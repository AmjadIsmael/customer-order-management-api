using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CustomerOrderManagement.Business.Configuration;
using CustomerOrderManagement.Business.Services;
using CustomerOrderManagement.Domain.Entities;
using CustomerOrderManagement.Domain.Enums;
using Microsoft.Extensions.Options;

namespace CustomerOrderManagement.Business.Tests.Services;

public sealed class JwtTokenGeneratorTests
{
    private static JwtTokenGenerator CreateGenerator(JwtSettings? settings = null)
    {
        settings ??= new JwtSettings
        {
            Issuer = "test-issuer",
            Audience = "test-audience",
            Key = "super-secret-signing-key-for-unit-tests-only",
            ExpiryMinutes = 60,
        };

        return new JwtTokenGenerator(Options.Create(settings));
    }

    [Fact]
    public void GenerateToken_ForStaffUser_IncludesStandardClaimsButNotCustomerId()
    {
        // Arrange
        var sut = CreateGenerator();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "admin",
            Email = "admin@example.com",
            Role = UserRole.Admin,
            CustomerId = null,
        };

        // Act
        var token = sut.GenerateToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token.Value);

        // Assert
        Assert.Equal(user.Id.ToString(), jwt.Claims.Single(c => c.Type == ClaimTypes.NameIdentifier).Value);
        Assert.Equal(user.Username, jwt.Claims.Single(c => c.Type == ClaimTypes.Name).Value);
        Assert.Equal(user.Role.ToString(), jwt.Claims.Single(c => c.Type == ClaimTypes.Role).Value);
        Assert.DoesNotContain(jwt.Claims, c => c.Type == CustomClaimTypes.CustomerId);
    }

    [Fact]
    public void GenerateToken_ForSelfRegisteredCustomer_IncludesCustomerIdClaim()
    {
        // Arrange
        var sut = CreateGenerator();
        var customerId = Guid.NewGuid();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "jane",
            Email = "jane@example.com",
            Role = UserRole.Customer,
            CustomerId = customerId,
        };

        // Act
        var token = sut.GenerateToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token.Value);

        // Assert
        Assert.Equal(customerId.ToString(), jwt.Claims.Single(c => c.Type == CustomClaimTypes.CustomerId).Value);
    }

    [Fact]
    public void GenerateToken_SetsExpiryAccordingToConfiguredMinutes()
    {
        // Arrange
        var settings = new JwtSettings
        {
            Issuer = "test-issuer",
            Audience = "test-audience",
            Key = "super-secret-signing-key-for-unit-tests-only",
            ExpiryMinutes = 30,
        };
        var sut = CreateGenerator(settings);
        var user = new User { Username = "admin" };
        var before = DateTime.UtcNow;

        // Act
        var token = sut.GenerateToken(user);

        // Assert
        Assert.InRange(token.ExpiresAtUtc, before.AddMinutes(30).AddSeconds(-5), before.AddMinutes(30).AddSeconds(5));
    }

    [Fact]
    public void GenerateToken_WhenSigningKeyMissing_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = new JwtSettings { Issuer = "test-issuer", Audience = "test-audience", Key = string.Empty };
        var sut = CreateGenerator(settings);
        var user = new User { Username = "admin" };

        // Act
        var act = () => sut.GenerateToken(user);

        // Assert
        Assert.Throws<InvalidOperationException>(act);
    }
}
