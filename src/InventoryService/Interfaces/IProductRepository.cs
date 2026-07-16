using InventoryService.Models;

namespace InventoryService.Interfaces;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(int id);

    Task<IEnumerable<Product>> GetAllAsync();

    Task AddAsync(Product product);

    Task UpdateAsync(Product product);
}