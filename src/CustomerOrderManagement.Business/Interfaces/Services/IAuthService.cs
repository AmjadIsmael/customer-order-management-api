using CustomerOrderManagement.Domain.DTOs.Auth;

namespace CustomerOrderManagement.Business.Interfaces.Services;

public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(
        LoginRequestDto dto,
        CancellationToken cancellationToken = default);

    Task<LoginResponseDto> RegisterAsync(
        RegisterRequestDto dto,
        CancellationToken cancellationToken = default);
}
