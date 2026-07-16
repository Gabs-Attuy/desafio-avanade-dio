using InventoryService.DTOs;

namespace InventoryService.Interfaces;

public interface IProductService
{
    Task<ProductResponseDto> AddProductAsync(CreateProductDto dto);

    Task<IEnumerable<ProductResponseDto>> GetAllProductsAsync();

    Task<ProductResponseDto?> GetProductByIdAsync(int id);

    Task<int?> GetStockByProductIdAsync(int id);

    Task<ProductResponseDto> UpdateProductAsync(int id, UpdateProductDto dto);
}