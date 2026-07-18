using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesService.DTOs.Order;
using SalesService.Enums;
using SalesService.Interfaces;

namespace SalesService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [Authorize]
    [HttpPost]
    [ProducesResponseType(typeof(OrderResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto createOrderDto)
    {
        var orderResponse = await _orderService.AddOrderAsync(createOrderDto);
        return CreatedAtAction(nameof(GetOrderById), new { id = orderResponse.Id }, orderResponse);
    }

    [Authorize]
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(OrderResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrderById(int id)
    {
        var orderResponse = await _orderService.GetOrderByIdAsync(id);
        if (orderResponse == null)
        {
            return NotFound();
        }
        return Ok(orderResponse);
    }

    [Authorize]
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<OrderResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllOrders()
    {
        var orders = await _orderService.GetAllOrdersAsync();
        return Ok(orders);
    }

    [Authorize(Roles = "Admin")]
    [HttpPatch("{id}/status")]
    [ProducesResponseType(typeof(OrderResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateOrderStatus(int id, [FromQuery] OrderStatusEnum status)
    {
        var orderResponse = await _orderService.UpdateOrderStatusAsync(id, status);
        return Ok(orderResponse);   
    }
}