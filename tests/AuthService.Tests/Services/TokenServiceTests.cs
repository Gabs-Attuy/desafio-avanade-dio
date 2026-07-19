using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AuthService.Enums;
using AuthService.Models;
using AuthService.Services;
using Microsoft.Extensions.Configuration;

namespace AuthService.Tests.Services;

[TestClass]
public class TokenServiceTests
{
    private const string JwtKey =
        "this-is-a-test-key-with-at-least-32-characters";

    private const string JwtIssuer =
        "AuthService.Tests";

    private const string JwtAudience =
        "Microservices.Tests";

    [TestMethod]
    public void GenerateToken_ShouldReturnValidJwt()
    {
        var service =
            CreateTokenService();

        var user = new User
        {
            Id = 10,
            Name = "Gabriel",
            Email = "gabriel@email.com",
            Role = Role.User
        };

        var token =
            service.GenerateToken(user);

        Assert.IsFalse(
            string.IsNullOrWhiteSpace(token));

        var handler =
            new JwtSecurityTokenHandler();

        Assert.IsTrue(
            handler.CanReadToken(token));
    }

    [TestMethod]
    public void GenerateToken_ShouldContainCorrectClaims()
    {
        var service =
            CreateTokenService();

        var user = new User
        {
            Id = 10,
            Name = "Gabriel",
            Email = "gabriel@email.com",
            Role = Role.Admin
        };

        var tokenString =
            service.GenerateToken(user);

        var handler =
            new JwtSecurityTokenHandler();

        var token =
            handler.ReadJwtToken(tokenString);

        Assert.AreEqual(
            "10",
            token.Claims
                .First(
                    claim =>
                        claim.Type ==
                        JwtRegisteredClaimNames.Sub)
                .Value);

        Assert.AreEqual(
            "gabriel@email.com",
            token.Claims
                .First(
                    claim =>
                        claim.Type ==
                        JwtRegisteredClaimNames.Email)
                .Value);

        Assert.AreEqual(
            "Gabriel",
            token.Claims
                .First(
                    claim =>
                        claim.Type ==
                        ClaimTypes.Name)
                .Value);

        Assert.AreEqual(
            Role.Admin.ToString(),
            token.Claims
                .First(
                    claim =>
                        claim.Type ==
                        ClaimTypes.Role)
                .Value);
    }

    [TestMethod]
    public void GenerateToken_ShouldUseConfiguredIssuerAndAudience()
    {
        var service =
            CreateTokenService();

        var user = new User
        {
            Id = 1,
            Name = "Gabriel",
            Email = "gabriel@email.com"
        };

        var tokenString =
            service.GenerateToken(user);

        var token =
            new JwtSecurityTokenHandler()
                .ReadJwtToken(tokenString);

        Assert.AreEqual(
            JwtIssuer,
            token.Issuer);

        Assert.IsTrue(
            token.Audiences.Contains(
                JwtAudience));
    }

    [TestMethod]
    public void GenerateToken_ShouldGenerateDifferentJti_ForEachToken()
    {
        var service =
            CreateTokenService();

        var user = new User
        {
            Id = 1,
            Name = "Gabriel",
            Email = "gabriel@email.com"
        };

        var handler =
            new JwtSecurityTokenHandler();

        var firstToken =
            handler.ReadJwtToken(
                service.GenerateToken(user));

        var secondToken =
            handler.ReadJwtToken(
                service.GenerateToken(user));

        var firstJti =
            firstToken.Claims
                .First(
                    claim =>
                        claim.Type ==
                        JwtRegisteredClaimNames.Jti)
                .Value;

        var secondJti =
            secondToken.Claims
                .First(
                    claim =>
                        claim.Type ==
                        JwtRegisteredClaimNames.Jti)
                .Value;

        Assert.AreNotEqual(
            firstJti,
            secondJti);
    }

    [TestMethod]
    public void GenerateToken_ShouldSetExpirationDate()
    {
        var service =
            CreateTokenService(
                expirationMinutes: "30");

        var user = new User
        {
            Id = 1,
            Name = "Gabriel",
            Email = "gabriel@email.com"
        };

        var before =
            DateTime.UtcNow.AddMinutes(29);

        var tokenString =
            service.GenerateToken(user);

        var token =
            new JwtSecurityTokenHandler()
                .ReadJwtToken(tokenString);

        var after =
            DateTime.UtcNow.AddMinutes(31);

        Assert.IsTrue(
            token.ValidTo >= before);

        Assert.IsTrue(
            token.ValidTo <= after);
    }

    [TestMethod]
    public void GenerateToken_ShouldUseDefaultExpiration_WhenExpirationIsNotConfigured()
    {
        var configurationData =
            new Dictionary<string, string?>
            {
                ["Jwt:Key"] = JwtKey,
                ["Jwt:Issuer"] = JwtIssuer,
                ["Jwt:Audience"] = JwtAudience
            };

        var configuration =
            new ConfigurationBuilder()
                .AddInMemoryCollection(
                    configurationData)
                .Build();

        var service =
            new TokenService(configuration);

        var user = new User
        {
            Id = 1,
            Name = "Gabriel",
            Email = "gabriel@email.com"
        };

        var before =
            DateTime.UtcNow.AddMinutes(59);

        var tokenString =
            service.GenerateToken(user);

        var token =
            new JwtSecurityTokenHandler()
                .ReadJwtToken(tokenString);

        var after =
            DateTime.UtcNow.AddMinutes(61);

        Assert.IsTrue(
            token.ValidTo >= before);

        Assert.IsTrue(
            token.ValidTo <= after);
    }

    [TestMethod]
    public void GenerateToken_ShouldThrowInvalidOperationException_WhenKeyIsNotConfigured()
    {
        var configurationData =
            new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = JwtIssuer,
                ["Jwt:Audience"] = JwtAudience
            };

        var service =
            CreateTokenService(
                configurationData);

        var user = new User();

        var exception =
            Assert.ThrowsException<InvalidOperationException>(
                () =>
                    service.GenerateToken(user));

        Assert.AreEqual(
            "A chave JWT não foi configurada.",
            exception.Message);
    }

    [TestMethod]
    public void GenerateToken_ShouldThrowInvalidOperationException_WhenIssuerIsNotConfigured()
    {
        var configurationData =
            new Dictionary<string, string?>
            {
                ["Jwt:Key"] = JwtKey,
                ["Jwt:Audience"] = JwtAudience
            };

        var service =
            CreateTokenService(
                configurationData);

        var exception =
            Assert.ThrowsException<InvalidOperationException>(
                () =>
                    service.GenerateToken(
                        new User()));

        Assert.AreEqual(
            "O issuer JWT não foi configurado.",
            exception.Message);
    }

    [TestMethod]
    public void GenerateToken_ShouldThrowInvalidOperationException_WhenAudienceIsNotConfigured()
    {
        var configurationData =
            new Dictionary<string, string?>
            {
                ["Jwt:Key"] = JwtKey,
                ["Jwt:Issuer"] = JwtIssuer
            };

        var service =
            CreateTokenService(
                configurationData);

        var exception =
            Assert.ThrowsException<InvalidOperationException>(
                () =>
                    service.GenerateToken(
                        new User()));

        Assert.AreEqual(
            "A audience JWT não foi configurada.",
            exception.Message);
    }

    private static TokenService CreateTokenService(
        string expirationMinutes = "60")
    {
        var configurationData =
            new Dictionary<string, string?>
            {
                ["Jwt:Key"] = JwtKey,
                ["Jwt:Issuer"] = JwtIssuer,
                ["Jwt:Audience"] = JwtAudience,
                ["Jwt:ExpirationMinutes"] =
                    expirationMinutes
            };

        return CreateTokenService(
            configurationData);
    }

    private static TokenService CreateTokenService(
        Dictionary<string, string?> configurationData)
    {
        var configuration =
            new ConfigurationBuilder()
                .AddInMemoryCollection(
                    configurationData)
                .Build();

        return new TokenService(
            configuration);
    }
}