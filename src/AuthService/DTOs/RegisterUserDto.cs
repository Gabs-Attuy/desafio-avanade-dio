using System.ComponentModel.DataAnnotations;
using AuthService.Enums;

namespace AuthService.DTOs;

public class RegisterUserDto
{
    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Confirmação de senha não confere.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}