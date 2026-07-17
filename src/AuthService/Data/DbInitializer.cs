using AuthService.Enums;
using AuthService.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(
        AuthContext context,
        IConfiguration configuration)
    {
        var adminExists = await context.Users
            .AnyAsync(user => user.Email == "admin@ecommerce.com");

        if (adminExists)
            return;

        var admin = new User
        {
            Name = "Administrator",
            Email = "admin@ecommerce.com",
            Role = Role.Admin
        };

        var passwordHasher = new PasswordHasher<User>();

        admin.Password = passwordHasher.HashPassword(
            admin,
            configuration["SeedAdmin:Password"]!
        );

        context.Users.Add(admin);

        await context.SaveChangesAsync();
    }
}