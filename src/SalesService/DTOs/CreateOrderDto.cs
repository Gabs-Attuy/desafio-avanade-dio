namespace SalesService.DTOs;
public class CreateOrderDto
{
    public decimal TotalAmount { get; set; }
    public List<CreateOrderItemDto> Items { get; set; } = [];
}