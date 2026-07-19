using AuthService.DTOs;
using AuthService.Enums;
using AuthService.Exceptions;
using AuthService.Interfaces;
using AuthService.Models;
using AuthService.Services;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace AuthService.Tests.Services;

[TestClass]
public class AuthenticationServiceTests
{
    private Mock<IUserRepository> _userRepositoryMock = null!;
    private Mock<ITokenService> _tokenServiceMock = null!;
    private Mock<IPasswordHasher<User>> _passwordHasherMock = null!;
    private AuthenticationService _authenticationService = null!;

    [TestInitialize]
    public void Setup()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _tokenServiceMock = new Mock<ITokenService>();
        _passwordHasherMock = new Mock<IPasswordHasher<User>>();

        _authenticationService = new AuthenticationService(
            _userRepositoryMock.Object,
            _tokenServiceMock.Object,
            _passwordHasherMock.Object);
    }

    [TestMethod]
    public async Task RegisterAsync_ShouldRegisterUser_WhenEmailIsNotRegistered()
    {
        var dto = new RegisterUserDto
        {
            Name = "  Gabriel Attuy  ",
            Email = "Gabriel@Email.com",
            Password = "123456"
        };

        _userRepositoryMock
            .Setup(repository =>
                repository.GetUserByEmailAsync(dto.Email))
            .ReturnsAsync((User?)null);

        _passwordHasherMock
            .Setup(hasher =>
                hasher.HashPassword(
                    It.IsAny<User>(),
                    dto.Password))
            .Returns("hashed-password");

        _tokenServiceMock
            .Setup(service =>
                service.GenerateToken(It.IsAny<User>()))
            .Returns("generated-token");

        User? capturedUser = null;

        _userRepositoryMock
            .Setup(repository =>
                repository.AddAsync(It.IsAny<User>()))
            .Callback<User>(user => capturedUser = user)
            .Returns(Task.CompletedTask);

        var result =
            await _authenticationService.RegisterAsync(dto);

        Assert.IsNotNull(capturedUser);

        Assert.AreEqual(
            "Gabriel Attuy",
            capturedUser.Name);

        Assert.AreEqual(
            "gabriel@email.com",
            capturedUser.Email);

        Assert.AreEqual(
            "hashed-password",
            capturedUser.Password);

        Assert.AreEqual(
            Role.User,
            capturedUser.Role);

        Assert.AreEqual(
            "generated-token",
            result.Token);

        _userRepositoryMock.Verify(
            repository =>
                repository.AddAsync(It.IsAny<User>()),
            Times.Once);

        _tokenServiceMock.Verify(
            service =>
                service.GenerateToken(It.IsAny<User>()),
            Times.Once);
    }

    [TestMethod]
    public async Task RegisterAsync_ShouldThrowConflictException_WhenEmailAlreadyExists()
    {
        var dto = new RegisterUserDto
        {
            Name = "Gabriel",
            Email = "gabriel@email.com",
            Password = "123456"
        };

        _userRepositoryMock
            .Setup(repository =>
                repository.GetUserByEmailAsync(dto.Email))
            .ReturnsAsync(new User());

        var exception =
            await Assert.ThrowsExceptionAsync<ConflictException>(
                () => _authenticationService.RegisterAsync(dto));

        Assert.AreEqual(
            "Já existe um usuário cadastrado com este e-mail.",
            exception.Message);

        _userRepositoryMock.Verify(
            repository =>
                repository.AddAsync(It.IsAny<User>()),
            Times.Never);

        _tokenServiceMock.Verify(
            service =>
                service.GenerateToken(It.IsAny<User>()),
            Times.Never);
    }

    [TestMethod]
    public async Task RegisterAdminAsync_ShouldRegisterAdmin_WhenEmailIsNotRegistered()
    {
        var dto = new RegisterUserDto
        {
            Name = "  Admin  ",
            Email = "  ADMIN@EMAIL.COM  ",
            Password = "123456"
        };

        _userRepositoryMock
            .Setup(repository =>
                repository.GetUserByEmailAsync(
                    "admin@email.com"))
            .ReturnsAsync((User?)null);

        _passwordHasherMock
            .Setup(hasher =>
                hasher.HashPassword(
                    It.IsAny<User>(),
                    dto.Password))
            .Returns("hashed-password");

        _tokenServiceMock
            .Setup(service =>
                service.GenerateToken(It.IsAny<User>()))
            .Returns("admin-token");

        User? capturedUser = null;

        _userRepositoryMock
            .Setup(repository =>
                repository.AddAsync(It.IsAny<User>()))
            .Callback<User>(user => capturedUser = user)
            .Returns(Task.CompletedTask);

        var result =
            await _authenticationService.RegisterAdminAsync(dto);

        Assert.IsNotNull(capturedUser);

        Assert.AreEqual(
            "Admin",
            capturedUser.Name);

        Assert.AreEqual(
            "admin@email.com",
            capturedUser.Email);

        Assert.AreEqual(
            Role.Admin,
            capturedUser.Role);

        Assert.AreEqual(
            "hashed-password",
            capturedUser.Password);

        Assert.AreEqual(
            "admin-token",
            result.Token);
    }

    [TestMethod]
    public async Task RegisterAdminAsync_ShouldThrowConflictException_WhenEmailAlreadyExists()
    {
        var dto = new RegisterUserDto
        {
            Name = "Admin",
            Email = "ADMIN@EMAIL.COM",
            Password = "123456"
        };

        _userRepositoryMock
            .Setup(repository =>
                repository.GetUserByEmailAsync(
                    "admin@email.com"))
            .ReturnsAsync(new User());

        await Assert.ThrowsExceptionAsync<ConflictException>(
            () =>
                _authenticationService.RegisterAdminAsync(dto));

        _userRepositoryMock.Verify(
            repository =>
                repository.AddAsync(It.IsAny<User>()),
            Times.Never);
    }

    [TestMethod]
    public async Task LoginAsync_ShouldReturnToken_WhenCredentialsAreValid()
    {
        var dto = new LoginDto
        {
            Email = "  USER@EMAIL.COM  ",
            Password = "123456"
        };

        var user = new User
        {
            Id = 1,
            Name = "Gabriel",
            Email = "user@email.com",
            Password = "hashed-password",
            Role = Role.User
        };

        _userRepositoryMock
            .Setup(repository =>
                repository.GetUserByEmailAsync(
                    "user@email.com"))
            .ReturnsAsync(user);

        _passwordHasherMock
            .Setup(hasher =>
                hasher.VerifyHashedPassword(
                    user,
                    "hashed-password",
                    dto.Password))
            .Returns(
                PasswordVerificationResult.Success);

        _tokenServiceMock
            .Setup(service =>
                service.GenerateToken(user))
            .Returns("valid-token");

        var result =
            await _authenticationService.LoginAsync(dto);

        Assert.AreEqual(
            "valid-token",
            result.Token);

        _tokenServiceMock.Verify(
            service =>
                service.GenerateToken(user),
            Times.Once);
    }

    [TestMethod]
    public async Task LoginAsync_ShouldThrowUnauthorizedAccessException_WhenUserDoesNotExist()
    {
        var dto = new LoginDto
        {
            Email = "notfound@email.com",
            Password = "123456"
        };

        _userRepositoryMock
            .Setup(repository =>
                repository.GetUserByEmailAsync(
                    "notfound@email.com"))
            .ReturnsAsync((User?)null);

        var exception =
            await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(
                () => _authenticationService.LoginAsync(dto));

        Assert.AreEqual(
            "E-mail ou senha inválidos.",
            exception.Message);

        _passwordHasherMock.Verify(
            hasher =>
                hasher.VerifyHashedPassword(
                    It.IsAny<User>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()),
            Times.Never);

        _tokenServiceMock.Verify(
            service =>
                service.GenerateToken(It.IsAny<User>()),
            Times.Never);
    }

    [TestMethod]
    public async Task LoginAsync_ShouldThrowUnauthorizedAccessException_WhenPasswordIsInvalid()
    {
        var dto = new LoginDto
        {
            Email = "user@email.com",
            Password = "wrong-password"
        };

        var user = new User
        {
            Id = 1,
            Email = "user@email.com",
            Password = "hashed-password"
        };

        _userRepositoryMock
            .Setup(repository =>
                repository.GetUserByEmailAsync(
                    "user@email.com"))
            .ReturnsAsync(user);

        _passwordHasherMock
            .Setup(hasher =>
                hasher.VerifyHashedPassword(
                    user,
                    user.Password,
                    dto.Password))
            .Returns(
                PasswordVerificationResult.Failed);

        var exception =
            await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(
                () => _authenticationService.LoginAsync(dto));

        Assert.AreEqual(
            "E-mail ou senha inválidos.",
            exception.Message);

        _tokenServiceMock.Verify(
            service =>
                service.GenerateToken(It.IsAny<User>()),
            Times.Never);
    }

    [TestMethod]
    public async Task LoginAsync_ShouldAcceptSuccessRehashNeeded_AsValidPassword()
    {
        var dto = new LoginDto
        {
            Email = "user@email.com",
            Password = "123456"
        };

        var user = new User
        {
            Id = 1,
            Email = "user@email.com",
            Password = "hashed-password"
        };

        _userRepositoryMock
            .Setup(repository =>
                repository.GetUserByEmailAsync(
                    "user@email.com"))
            .ReturnsAsync(user);

        _passwordHasherMock
            .Setup(hasher =>
                hasher.VerifyHashedPassword(
                    user,
                    user.Password,
                    dto.Password))
            .Returns(
                PasswordVerificationResult.SuccessRehashNeeded);

        _tokenServiceMock
            .Setup(service =>
                service.GenerateToken(user))
            .Returns("valid-token");

        var result =
            await _authenticationService.LoginAsync(dto);

        Assert.AreEqual(
            "valid-token",
            result.Token);
    }
}