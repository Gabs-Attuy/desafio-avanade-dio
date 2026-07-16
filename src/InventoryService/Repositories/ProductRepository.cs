using InventoryService.Data;
using InventoryService.Interfaces;
using InventoryService.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryService.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly InventoryContext _context;

    public ProductRepository(InventoryContext context)
    {
        _context = context;
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        return await _context.Products.FindAsync(id);
    }

    public async Task<IEnumerable<Product>> GetAllAsync()
    {
        return await _context.Products.ToListAsync();
    }

    public async Task AddAsync(Product product)
    {
        await _context.Products.AddAsync(product);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Product product)
    {
        _context.Products.Update(product);
        await _context.SaveChangesAsync();
    }
}