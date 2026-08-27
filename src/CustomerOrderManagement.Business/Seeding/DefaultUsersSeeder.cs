using CustomerOrderManagement.Business.Interfaces.Persistence;
using CustomerOrderManagement.Domain.Entities;
using CustomerOrderManagement.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CustomerOrderManagement.Business.Seeding;

public static class DefaultUsersSeeder
{
    private const string SystemUser = "system";

    private static readonly IReadOnlyList<(string Username, string Email, string Password, UserRole Role)> DefaultUsers =
    [
        ("admin", "admin@customerordermanagement.local", "Admin@123", UserRole.Admin),
        ("user", "user@customerordermanagement.local", "User@123", UserRole.User),
    ];

    public static async Task SeedDefaultUsersAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();

        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(nameof(DefaultUsersSeeder));

        foreach (var (username, email, password, role) in DefaultUsers)
        {
            var existingUser = await unitOfWork.Users.GetByUsernameAsync(username);
            if (existingUser is not null)
            {
                continue;
            }

            var user = new User
            {
                Username = username,
                Email = email,
                Role = role,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = SystemUser,
            };
            user.PasswordHash = passwordHasher.HashPassword(user, password);

            await unitOfWork.Users.AddAsync(user);
            await unitOfWork.SaveChangesAsync();

            logger.LogWarning(
                "Seeded default {Role} user '{Username}' with password '{Password}'. " +
                "This account is for local development only — remove it before deploying.",
                role,
                username,
                password);
        }
    }
}
