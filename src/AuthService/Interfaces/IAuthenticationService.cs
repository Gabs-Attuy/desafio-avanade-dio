using AuthService.DTOs;

namespace AuthService.Interfaces;

public interface IAuthenticationService
{
    Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
    Task<AuthResponseDto> RegisterAsync(RegisterUserDto registerUserDto);
    Task<AuthResponseDto> RegisterAdminAsync(RegisterUserDto dto);
}