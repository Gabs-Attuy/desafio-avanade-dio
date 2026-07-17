using SalesService.Enums;

namespace SalesService.DTOs.Order;

public class OrderResponseDto
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<OrderItemResponseDto> Items { get; set; } = new List<OrderItemResponseDto>();
    public decimal TotalAmount { get; set; }
    public OrderStatusEnum Status { get; set; }
}