using AuthService.Models;

namespace AuthService.Interfaces;

public interface IUserRepository
{
    Task<bool> IsEmailRegisteredAsync(string email);
    Task AddAsync(User user);
    Task<User?> GetUserByEmailAsync(string email);
}