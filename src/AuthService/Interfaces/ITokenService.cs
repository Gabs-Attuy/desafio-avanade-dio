using AuthService.Models;

namespace AuthService.Interfaces;

public interface ITokenService
{
    string GenerateToken(User user);
}