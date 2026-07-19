using InventoryService.Data;
using InventoryService.DTOs;
using InventoryService.Interfaces;
using InventoryService.Models;
using InventoryService.Services;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace InventoryService.Tests.Services;

[TestClass]
public class ProductServiceTests
{
    private Mock<IProductRepository> _productRepositoryMock = null!;
    private InventoryContext _context = null!;
    private ProductService _productService = null!;

    [TestInitialize]
    public void Setup()
    {
        _productRepositoryMock = new Mock<IProductRepository>();

        var options = new DbContextOptionsBuilder<InventoryContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new InventoryContext(options);

        _productService = new ProductService(
            _productRepositoryMock.Object,
            _context);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _context.Dispose();
    }

    [TestMethod]
    public async Task GetProductByIdAsync_ShouldReturnProduct_WhenProductExists()
    {
        var product = new Product
        {
            Id = 1,
            Name = "Café",
            Description = "Café tradicional",
            Price = 29.99m,
            Stock = 10,
            IsActive = true
        };

        _productRepositoryMock
            .Setup(repository => repository.GetByIdAsync(1))
            .ReturnsAsync(product);

        var result = await _productService.GetProductByIdAsync(1);

        Assert.IsNotNull(result);
        Assert.AreEqual(product.Id, result.Id);
        Assert.AreEqual(product.Name, result.Name);
        Assert.AreEqual(product.Description, result.Description);
        Assert.AreEqual(product.Price, result.Price);
        Assert.AreEqual(product.Stock, result.Stock);
        Assert.AreEqual(product.IsActive, result.IsActive);
    }

    [TestMethod]
    public async Task GetProductByIdAsync_ShouldReturnNull_WhenProductDoesNotExist()
    {
        _productRepositoryMock
            .Setup(repository => repository.GetByIdAsync(1))
            .ReturnsAsync((Product?)null);

        var result = await _productService.GetProductByIdAsync(1);

        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetAllProductsAsync_ShouldReturnAllProducts()
    {
        var products = new List<Product>
        {
            new()
            {
                Id = 1,
                Name = "Café",
                Description = "Café tradicional",
                Price = 29.99m,
                Stock = 10,
                IsActive = true
            },
            new()
            {
                Id = 2,
                Name = "Leite",
                Description = "Leite integral",
                Price = 7.99m,
                Stock = 20,
                IsActive = false
            }
        };

        _productRepositoryMock
            .Setup(repository => repository.GetAllAsync())
            .ReturnsAsync(products);

        var result = (await _productService.GetAllProductsAsync()).ToList();

        Assert.AreEqual(2, result.Count);

        Assert.AreEqual(1, result[0].Id);
        Assert.AreEqual("Café", result[0].Name);

        Assert.AreEqual(2, result[1].Id);
        Assert.AreEqual("Leite", result[1].Name);
        Assert.IsFalse(result[1].IsActive);
    }

    [TestMethod]
    public async Task GetAllProductsAsync_ShouldReturnEmptyCollection_WhenNoProductsExist()
    {
        _productRepositoryMock
            .Setup(repository => repository.GetAllAsync())
            .ReturnsAsync([]);

        var result = await _productService.GetAllProductsAsync();

        Assert.IsNotNull(result);
        Assert.IsFalse(result.Any());
    }

    [TestMethod]
    public async Task GetStockByProductIdAsync_ShouldReturnStock_WhenProductExists()
    {
        var product = new Product
        {
            Id = 1,
            Stock = 15
        };

        _productRepositoryMock
            .Setup(repository => repository.GetByIdAsync(1))
            .ReturnsAsync(product);

        var result = await _productService.GetStockByProductIdAsync(1);

        Assert.AreEqual(15, result);
    }

    [TestMethod]
    public async Task GetStockByProductIdAsync_ShouldReturnNull_WhenProductDoesNotExist()
    {
        _productRepositoryMock
            .Setup(repository => repository.GetByIdAsync(1))
            .ReturnsAsync((Product?)null);

        var result = await _productService.GetStockByProductIdAsync(1);

        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task AddProductAsync_ShouldAddProduct_WhenDataIsValid()
    {
        var dto = new CreateProductDto
        {
            Name = "  Café Pilão  ",
            Description = "  Café tradicional  ",
            Price = 29.99m,
            Stock = 10
        };

        Product? capturedProduct = null;

        _productRepositoryMock
            .Setup(repository => repository.AddAsync(It.IsAny<Product>()))
            .Callback<Product>(product => capturedProduct = product)
            .Returns(Task.CompletedTask);

        var result = await _productService.AddProductAsync(dto);

        Assert.IsNotNull(capturedProduct);

        Assert.AreEqual("Café Pilão", capturedProduct.Name);
        Assert.AreEqual("Café tradicional", capturedProduct.Description);
        Assert.AreEqual(29.99m, capturedProduct.Price);
        Assert.AreEqual(10, capturedProduct.Stock);

        Assert.AreEqual("Café Pilão", result.Name);
        Assert.AreEqual("Café tradicional", result.Description);
        Assert.AreEqual(29.99m, result.Price);
        Assert.AreEqual(10, result.Stock);

        _productRepositoryMock.Verify(
            repository => repository.AddAsync(It.IsAny<Product>()),
            Times.Once);
    }

    [TestMethod]
    public async Task AddProductAsync_ShouldThrowArgumentException_WhenPriceIsZero()
    {
        var dto = new CreateProductDto
        {
            Name = "Café",
            Description = "Café tradicional",
            Price = 0,
            Stock = 10
        };

        var exception = await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => _productService.AddProductAsync(dto));

        Assert.AreEqual(
            "O preço deve ser maior que zero.",
            exception.Message);

        _productRepositoryMock.Verify(
            repository => repository.AddAsync(It.IsAny<Product>()),
            Times.Never);
    }

    [TestMethod]
    public async Task AddProductAsync_ShouldThrowArgumentException_WhenPriceIsNegative()
    {
        var dto = new CreateProductDto
        {
            Name = "Café",
            Description = "Café tradicional",
            Price = -10,
            Stock = 10
        };

        await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => _productService.AddProductAsync(dto));

        _productRepositoryMock.Verify(
            repository => repository.AddAsync(It.IsAny<Product>()),
            Times.Never);
    }

    [TestMethod]
    public async Task AddProductAsync_ShouldThrowArgumentException_WhenStockIsNegative()
    {
        var dto = new CreateProductDto
        {
            Name = "Café",
            Description = "Café tradicional",
            Price = 29.99m,
            Stock = -1
        };

        var exception = await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => _productService.AddProductAsync(dto));

        Assert.AreEqual(
            "O estoque não pode ser negativo.",
            exception.Message);

        _productRepositoryMock.Verify(
            repository => repository.AddAsync(It.IsAny<Product>()),
            Times.Never);
    }

    [TestMethod]
    public async Task UpdateProductAsync_ShouldUpdateProduct_WhenProductExists()
    {
        var product = new Product
        {
            Id = 1,
            Name = "Produto antigo",
            Description = "Descrição antiga",
            Price = 10,
            Stock = 5,
            IsActive = true
        };

        var dto = new UpdateProductDto
        {
            Name = "  Produto atualizado  ",
            Description = "  Nova descrição  ",
            Price = 25.50m,
            Stock = 15
        };

        _productRepositoryMock
            .Setup(repository => repository.GetByIdAsync(1))
            .ReturnsAsync(product);

        _productRepositoryMock
            .Setup(repository => repository.UpdateAsync(product))
            .Returns(Task.CompletedTask);

        var result = await _productService.UpdateProductAsync(1, dto);

        Assert.AreEqual("Produto atualizado", product.Name);
        Assert.AreEqual("Nova descrição", product.Description);
        Assert.AreEqual(25.50m, product.Price);
        Assert.AreEqual(15, product.Stock);

        Assert.AreEqual(product.Id, result.Id);
        Assert.AreEqual("Produto atualizado", result.Name);
        Assert.AreEqual("Nova descrição", result.Description);
        Assert.AreEqual(25.50m, result.Price);
        Assert.AreEqual(15, result.Stock);

        _productRepositoryMock.Verify(
            repository => repository.UpdateAsync(product),
            Times.Once);
    }

    [TestMethod]
    public async Task UpdateProductAsync_ShouldThrowKeyNotFoundException_WhenProductDoesNotExist()
    {
        var dto = new UpdateProductDto
        {
            Name = "Produto",
            Description = "Descrição",
            Price = 20,
            Stock = 5
        };

        _productRepositoryMock
            .Setup(repository => repository.GetByIdAsync(1))
            .ReturnsAsync((Product?)null);

        var exception =
            await Assert.ThrowsExceptionAsync<KeyNotFoundException>(
                () => _productService.UpdateProductAsync(1, dto));

        Assert.AreEqual(
            "Produto não encontrado.",
            exception.Message);

        _productRepositoryMock.Verify(
            repository => repository.UpdateAsync(It.IsAny<Product>()),
            Times.Never);
    }

    [TestMethod]
    public async Task UpdateProductAsync_ShouldThrowArgumentException_WhenPriceIsInvalid()
    {
        var product = new Product
        {
            Id = 1
        };

        var dto = new UpdateProductDto
        {
            Name = "Produto",
            Description = "Descrição",
            Price = 0,
            Stock = 5
        };

        _productRepositoryMock
            .Setup(repository => repository.GetByIdAsync(1))
            .ReturnsAsync(product);

        await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => _productService.UpdateProductAsync(1, dto));

        _productRepositoryMock.Verify(
            repository => repository.UpdateAsync(It.IsAny<Product>()),
            Times.Never);
    }

    [TestMethod]
    public async Task UpdateProductAsync_ShouldThrowArgumentException_WhenStockIsNegative()
    {
        var product = new Product
        {
            Id = 1
        };

        var dto = new UpdateProductDto
        {
            Name = "Produto",
            Description = "Descrição",
            Price = 20,
            Stock = -1
        };

        _productRepositoryMock
            .Setup(repository => repository.GetByIdAsync(1))
            .ReturnsAsync(product);

        await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => _productService.UpdateProductAsync(1, dto));

        _productRepositoryMock.Verify(
            repository => repository.UpdateAsync(It.IsAny<Product>()),
            Times.Never);
    }

    [TestMethod]
    public async Task UpdateProductStatusAsync_ShouldDeactivateProduct_WhenProductIsActive()
    {
        var product = new Product
        {
            Id = 1,
            Name = "Café",
            IsActive = true
        };

        _productRepositoryMock
            .Setup(repository => repository.GetByIdAsync(1))
            .ReturnsAsync(product);

        _productRepositoryMock
            .Setup(repository => repository.UpdateAsync(product))
            .Returns(Task.CompletedTask);

        var result =
            await _productService.UpdateProductStatusAsync(1);

        Assert.IsFalse(product.IsActive);
        Assert.IsFalse(result.IsActive);

        _productRepositoryMock.Verify(
            repository => repository.UpdateAsync(product),
            Times.Once);
    }

    [TestMethod]
    public async Task UpdateProductStatusAsync_ShouldActivateProduct_WhenProductIsInactive()
    {
        var product = new Product
        {
            Id = 1,
            Name = "Café",
            IsActive = false
        };

        _productRepositoryMock
            .Setup(repository => repository.GetByIdAsync(1))
            .ReturnsAsync(product);

        var result =
            await _productService.UpdateProductStatusAsync(1);

        Assert.IsTrue(product.IsActive);
        Assert.IsTrue(result.IsActive);
    }

    [TestMethod]
    public async Task UpdateProductStatusAsync_ShouldThrowKeyNotFoundException_WhenProductDoesNotExist()
    {
        _productRepositoryMock
            .Setup(repository => repository.GetByIdAsync(1))
            .ReturnsAsync((Product?)null);

        var exception =
            await Assert.ThrowsExceptionAsync<KeyNotFoundException>(
                () => _productService.UpdateProductStatusAsync(1));

        Assert.AreEqual(
            "Produto não encontrado.",
            exception.Message);

        _productRepositoryMock.Verify(
            repository => repository.UpdateAsync(It.IsAny<Product>()),
            Times.Never);
    }
}