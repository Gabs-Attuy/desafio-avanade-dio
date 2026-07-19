using System.ComponentModel.DataAnnotations;

namespace SalesService.DTOs.InventoryMS;

public class ProductDto
{
    [Required]
    public int Id { get; set; }

    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public decimal Price { get; set; }
    
    [Required]
    public int Stock { get; set; }
    
    [Required]
    public bool IsActive { get; set; }
}