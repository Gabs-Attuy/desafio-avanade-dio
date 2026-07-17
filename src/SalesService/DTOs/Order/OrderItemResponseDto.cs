using System.ComponentModel.DataAnnotations;

namespace SalesService.DTOs.Order;

public class OrderItemResponseDto
{
    [Required]
    public int ProductId { get; set; }

    [Required]
    [MaxLength(150)]
    public string ProductName { get; set; } = string.Empty;

    [Required]
    public decimal UnitPrice { get; set; }
    
    [Required]
    public int Quantity { get; set; }
}