using Microsoft.EntityFrameworkCore;
using SalesService.Data;
using SalesService.Enums;
using SalesService.Interfaces;
using SalesService.Models;

namespace SalesService.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly SalesContext _context;

    public OrderRepository(SalesContext context)
    {
        _context = context;
    }

    public async Task<Order?> GetByIdAsync(int id)
    {
        return await _context.Orders
        .Include(o => o.Items)
        .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<IEnumerable<Order>> GetAllAsync()
    {
        return await _context.Orders
        .Include(o => o.Items)
        .ToListAsync();
    }

    public async Task AddAsync(Order order)
    {
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Order order)
    {
        _context.Orders.Update(order);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Order>> GetByUserIdAsync(int userId)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .Where(o => o.UserId == userId)
            .ToListAsync();
    }
}