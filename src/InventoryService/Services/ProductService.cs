using InventoryService.DTOs;
using InventoryService.Interfaces;
using InventoryService.Models;

namespace InventoryService.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;

    public ProductService(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<ProductResponseDto?> GetProductByIdAsync(int id)
    {
        var product = await _productRepository.GetByIdAsync(id);

        return product != null ? ToDto(product) : null;
    }

    public async Task<IEnumerable<ProductResponseDto>> GetAllProductsAsync()
    {
        var products = await _productRepository.GetAllAsync();

        return products.Select(ToDto);
    }

    public async Task<int?> GetStockByProductIdAsync(int id)
    {
        var product = await _productRepository.GetByIdAsync(id);
        return product?.Stock;
    }

    public async Task<ProductResponseDto> AddProductAsync(CreateProductDto dto)
    {
        ValidateProductPriceAndStock(dto.Price, dto.Stock);

        var product = new Product
        {
            Name = dto.Name.Trim(),
            Description = dto.Description.Trim(),
            Price = dto.Price,
            Stock = dto.Stock
        };

        await _productRepository.AddAsync(product);

        return ToDto(product);
    }

    public async Task<ProductResponseDto> UpdateProductAsync(int id, UpdateProductDto dto)
    {
        var product = await _productRepository.GetByIdAsync(id) ?? throw new KeyNotFoundException("Produto não encontrado.");

        ValidateProductPriceAndStock(dto.Price, dto.Stock);

        product.Name = dto.Name.Trim();
        product.Description = dto.Description.Trim();
        product.Price = dto.Price;
        product.Stock = dto.Stock;

        await _productRepository.UpdateAsync(product);

        return ToDto(product);
    }

    private static void ValidateProductPriceAndStock(decimal price, int stock)
    {
        if (price <= 0)
            throw new ArgumentException("O preço deve ser maior que zero.");

        if (stock < 0)
            throw new ArgumentException("O estoque não pode ser negativo.");
    }

    private static ProductResponseDto ToDto (Product product)
    {
        return new ProductResponseDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            Stock = product.Stock
        };
    }
}