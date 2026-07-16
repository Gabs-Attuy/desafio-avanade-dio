using InventoryService.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryService.Data;

public class InventoryContext : DbContext
{
    public InventoryContext(DbContextOptions<InventoryContext> options) : base(options)
    {
    }

    public DbSet<Product> Products { get; set; }
}