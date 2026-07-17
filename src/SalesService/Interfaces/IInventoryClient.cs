using SalesService.DTOs.InventoryMS;

namespace SalesService.Interfaces;

public interface IInventoryClient
{
    Task<ProductDto?> GetProductByIdAsync(int id);
}