using AuthService.DTOs;
using AuthService.Enums;
using AuthService.Exceptions;
using AuthService.Interfaces;
using AuthService.Models;
using Microsoft.AspNetCore.Identity;

namespace AuthService.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private readonly IPasswordHasher<User> _passwordHasher;

    public AuthenticationService(
        IUserRepository userRepository,
        ITokenService tokenService,
        IPasswordHasher<User> passwordHasher)
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
        _passwordHasher = passwordHasher;
    }

    public async Task<AuthResponseDto> RegisterAsync(
        RegisterUserDto dto)
    {
        var existingUser =
            await _userRepository.GetUserByEmailAsync(dto.Email);

        if (existingUser is not null)
        {
            throw new ConflictException(
                "Já existe um usuário cadastrado com este e-mail.");
        }

        var user = new User
        {
            Name = dto.Name.Trim(),
            Email = dto.Email.Trim().ToLowerInvariant(),
            Role = Role.User
        };

        user.Password =
            _passwordHasher.HashPassword(
                user,
                dto.Password);

        await _userRepository.AddAsync(user);

        var token = _tokenService.GenerateToken(user);

        return new AuthResponseDto
        {
            Token = token
        };
    }

    public async Task<AuthResponseDto> RegisterAdminAsync(
    RegisterUserDto dto)
    {
        var email = dto.Email
            .Trim()
            .ToLowerInvariant();

        var existingUser =
            await _userRepository.GetUserByEmailAsync(email);

        if (existingUser is not null)
        {
            throw new ConflictException(
                "Já existe um usuário cadastrado com este e-mail.");
        }

        var user = new User
        {
            Name = dto.Name.Trim(),
            Email = email,
            Role = Role.Admin
        };

        user.Password =
            _passwordHasher.HashPassword(
                user,
                dto.Password);

        await _userRepository.AddAsync(user);

        var token =
            _tokenService.GenerateToken(user);

        return new AuthResponseDto
        {
            Token = token
        };
    }
    public async Task<AuthResponseDto> LoginAsync(
        LoginDto dto)
    {
        var email = dto.Email
            .Trim()
            .ToLowerInvariant();

        var user =
            await _userRepository.GetUserByEmailAsync(email)
            ?? throw new UnauthorizedAccessException(
                "E-mail ou senha inválidos.");

        var verificationResult =
            _passwordHasher.VerifyHashedPassword(
                user,
                user.Password,
                dto.Password);

        if (verificationResult ==
            PasswordVerificationResult.Failed)
        {
            throw new UnauthorizedAccessException(
                "E-mail ou senha inválidos.");
        }

        var token =
            _tokenService.GenerateToken(user);

        return new AuthResponseDto
        {
            Token = token
        };
    }
}