using SalesService.DTOs.Order;
using SalesService.Enums;
using SalesService.Interfaces;
using SalesService.Messaging.Events;
using SalesService.Models;

namespace SalesService.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IInventoryClient _inventoryClient;
    private readonly IOrderCreatedProducer _orderCreatedProducer;

    public OrderService(
        IOrderRepository orderRepository,
        IInventoryClient inventoryClient,
        IOrderCreatedProducer orderCreatedProducer)
    {
        _orderRepository = orderRepository;
        _inventoryClient = inventoryClient;
        _orderCreatedProducer = orderCreatedProducer;
    }

    public async Task<OrderResponseDto> AddOrderAsync(CreateOrderDto createOrderDto)
    {
        if (createOrderDto.Items == null || createOrderDto.Items.Count == 0)
        {
            throw new ArgumentException("O pedido deve conter pelo menos um item.");
        }

        var order = new Order();
        
        foreach (var itemDto in createOrderDto.Items)
        {
            var product = await _inventoryClient.GetProductByIdAsync(itemDto.ProductId) ??
             throw new KeyNotFoundException($"Produto com ID {itemDto.ProductId} não encontrado.");

            if (product.Stock < itemDto.Quantity)
            {
                throw new InvalidOperationException($"Estoque insuficiente para o produto {product.Name}. Requisitado: {itemDto.Quantity}, Disponível: {product.Stock}");
            }
            
            var orderItem = new OrderItem
            {
                ProductId = itemDto.ProductId,
                ProductName = product.Name,
                UnitPrice = product.Price,
                Quantity = itemDto.Quantity
            };

            order.Items.Add(orderItem);
        }

        order.TotalAmount = order.Items.Sum(i => i.UnitPrice * i.Quantity);

        await _orderRepository.AddAsync(order);

        var orderCreatedEvent = new OrderCreatedEvent
        {
            OrderId = order.Id,

            Items = [.. order.Items.Select(item => new OrderCreatedItemEvent
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity
            })]
        };

        await _orderCreatedProducer.PublishAsync(orderCreatedEvent);

        return ToDto(order);
    }

    public async Task<OrderResponseDto?> GetOrderByIdAsync(int id)
    {
        var order = await _orderRepository.GetByIdAsync(id);
        if (order == null) return null;

        return ToDto(order);
    }

    public async Task<IEnumerable<OrderResponseDto>> GetAllOrdersAsync()
    {
        var orders = await _orderRepository.GetAllAsync();
        return orders.Select(ToDto);
    }

    public async Task<OrderResponseDto> UpdateOrderStatusAsync(int orderId, OrderStatusEnum status)
    {
        var order = await _orderRepository.GetByIdAsync(orderId) ?? throw new KeyNotFoundException($"Pedido com ID {orderId} não encontrado.");

        order.Status = status;
        
        await _orderRepository.UpdateAsync(order);

        return ToDto(order);
    }

    private OrderResponseDto ToDto(Order order)
    {
        return new OrderResponseDto
        {
            Id = order.Id,
            TotalAmount = order.TotalAmount,
            Status = order.Status,
            CreatedAt = order.OrderDate,
            Items = [.. order.Items.Select(i => new OrderItemResponseDto
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                UnitPrice = i.UnitPrice,
                Quantity = i.Quantity
            })]
        };
    }
}