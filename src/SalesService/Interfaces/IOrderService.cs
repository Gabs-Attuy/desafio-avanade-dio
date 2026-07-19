using SalesService.DTOs.Order;
using SalesService.Enums;

namespace SalesService.Interfaces;

public interface IOrderService
{
    Task<OrderResponseDto?> GetOrderByIdAsync(int id);
    Task<IEnumerable<OrderResponseDto>> GetAllOrdersAsync();
    Task<IEnumerable<OrderResponseDto>> GetByUserIdAsync(int userId);
    Task<OrderResponseDto> AddOrderAsync(CreateOrderDto createOrderDto, int userId);
    Task<OrderResponseDto> UpdateOrderStatusAsync(int orderId, OrderStatusEnum status);
}