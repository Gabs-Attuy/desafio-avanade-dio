using SalesService.DTOs.Order;
using SalesService.Enums;

namespace SalesService.Interfaces;

public interface IOrderService
{
    Task<OrderResponseDto?> GetOrderByIdAsync(int id);
    Task<IEnumerable<OrderResponseDto>> GetAllOrdersAsync();
    Task<OrderResponseDto> AddOrderAsync(CreateOrderDto createOrderDto);
    Task<OrderResponseDto> UpdateOrderStatusAsync(int orderId, OrderStatusEnum status);
}