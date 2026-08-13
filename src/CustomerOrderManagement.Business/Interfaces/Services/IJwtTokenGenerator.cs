using CustomerOrderManagement.Business.Models;
using CustomerOrderManagement.Domain.Entities;

namespace CustomerOrderManagement.Business.Interfaces.Services;

public interface IJwtTokenGenerator
{
    JwtToken GenerateToken(User user);
}
