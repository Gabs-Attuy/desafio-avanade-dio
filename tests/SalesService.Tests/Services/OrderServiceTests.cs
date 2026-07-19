using Moq;
using SalesService.DTOs.InventoryMS;
using SalesService.DTOs.Order;
using SalesService.Enums;
using SalesService.Interfaces;
using SalesService.Messaging.Events;
using SalesService.Models;
using SalesService.Services;

namespace SalesService.Tests.Services;

[TestClass]
public class OrderServiceTests
{
    private Mock<IOrderRepository> _orderRepositoryMock = null!;
    private Mock<IInventoryClient> _inventoryClientMock = null!;
    private Mock<IOrderCreatedProducer> _orderCreatedProducerMock = null!;
    private OrderService _orderService = null!;

    [TestInitialize]
    public void Setup()
    {
        _orderRepositoryMock = new Mock<IOrderRepository>();
        _inventoryClientMock = new Mock<IInventoryClient>();
        _orderCreatedProducerMock = new Mock<IOrderCreatedProducer>();

        _orderService = new OrderService(
            _orderRepositoryMock.Object,
            _inventoryClientMock.Object,
            _orderCreatedProducerMock.Object);
    }

    [TestMethod]
    public async Task AddOrderAsync_ShouldCreateOrder_WhenDataIsValid()
    {
        var dto = new CreateOrderDto
        {
            Items =
            [
                new CreateOrderItemDto
                {
                    ProductId = 1,
                    Quantity = 2
                }
            ]
        };

        var product = new ProductDto
        {
            Id = 1,
            Name = "Café Pilão",
            Price = 29.99m,
            Stock = 10,
            IsActive = true
        };

        _inventoryClientMock
            .Setup(client => client.GetProductByIdAsync(1))
            .ReturnsAsync(product);

        _orderRepositoryMock
            .Setup(repository => repository.AddAsync(It.IsAny<Order>()))
            .Callback<Order>(order => order.Id = 1)
            .Returns(Task.CompletedTask);

        _orderCreatedProducerMock
            .Setup(producer => producer.PublishAsync(It.IsAny<OrderCreatedEvent>()))
            .Returns(Task.CompletedTask);

        var result = await _orderService.AddOrderAsync(dto, 5);

        Assert.AreEqual(1, result.Id);
        Assert.AreEqual(5, result.UserId);
        Assert.AreEqual(59.98m, result.TotalAmount);
        Assert.AreEqual(1, result.Items.Count);

        Assert.AreEqual(1, result.Items[0].ProductId);
        Assert.AreEqual("Café Pilão", result.Items[0].ProductName);
        Assert.AreEqual(29.99m, result.Items[0].UnitPrice);
        Assert.AreEqual(2, result.Items[0].Quantity);

        _orderRepositoryMock.Verify(
            repository => repository.AddAsync(It.Is<Order>(
                order =>
                    order.UserId == 5 &&
                    order.TotalAmount == 59.98m &&
                    order.Items.Count == 1)),
            Times.Once);

        _orderCreatedProducerMock.Verify(
            producer => producer.PublishAsync(It.IsAny<OrderCreatedEvent>()),
            Times.Once);
    }

    [TestMethod]
    public async Task AddOrderAsync_ShouldCalculateTotalAmountCorrectly_WhenOrderHasMultipleItems()
    {
        var dto = new CreateOrderDto
        {
            Items =
            [
                new CreateOrderItemDto
                {
                    ProductId = 1,
                    Quantity = 2
                },
                new CreateOrderItemDto
                {
                    ProductId = 2,
                    Quantity = 3
                }
            ]
        };

        _inventoryClientMock
            .Setup(client => client.GetProductByIdAsync(1))
            .ReturnsAsync(new ProductDto
            {
                Id = 1,
                Name = "Produto 1",
                Price = 10m,
                Stock = 10,
                IsActive = true
            });

        _inventoryClientMock
            .Setup(client => client.GetProductByIdAsync(2))
            .ReturnsAsync(new ProductDto
            {
                Id = 2,
                Name = "Produto 2",
                Price = 5m,
                Stock = 10,
                IsActive = true
            });

        _orderRepositoryMock
            .Setup(repository => repository.AddAsync(It.IsAny<Order>()))
            .Returns(Task.CompletedTask);

        _orderCreatedProducerMock
            .Setup(producer => producer.PublishAsync(It.IsAny<OrderCreatedEvent>()))
            .Returns(Task.CompletedTask);

        var result = await _orderService.AddOrderAsync(dto, 1);

        Assert.AreEqual(35m, result.TotalAmount);
        Assert.AreEqual(2, result.Items.Count);
    }

    [TestMethod]
    public async Task AddOrderAsync_ShouldThrowArgumentException_WhenItemsAreNull()
    {
        var dto = new CreateOrderDto
        {
            Items = null!
        };

        var exception = await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => _orderService.AddOrderAsync(dto, 1));

        Assert.AreEqual(
            "O pedido deve conter pelo menos um item.",
            exception.Message);

        _orderRepositoryMock.Verify(
            repository => repository.AddAsync(It.IsAny<Order>()),
            Times.Never);

        _orderCreatedProducerMock.Verify(
            producer => producer.PublishAsync(It.IsAny<OrderCreatedEvent>()),
            Times.Never);
    }

    [TestMethod]
    public async Task AddOrderAsync_ShouldThrowArgumentException_WhenItemsAreEmpty()
    {
        var dto = new CreateOrderDto
        {
            Items = []
        };

        await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => _orderService.AddOrderAsync(dto, 1));

        _orderRepositoryMock.Verify(
            repository => repository.AddAsync(It.IsAny<Order>()),
            Times.Never);
    }

    [TestMethod]
    public async Task AddOrderAsync_ShouldThrowKeyNotFoundException_WhenProductDoesNotExist()
    {
        var dto = new CreateOrderDto
        {
            Items =
            [
                new CreateOrderItemDto
                {
                    ProductId = 99,
                    Quantity = 1
                }
            ]
        };

        _inventoryClientMock
            .Setup(client => client.GetProductByIdAsync(99))
            .ReturnsAsync((ProductDto?)null);

        var exception =
            await Assert.ThrowsExceptionAsync<KeyNotFoundException>(
                () => _orderService.AddOrderAsync(dto, 1));

        Assert.AreEqual(
            "Produto com ID 99 não encontrado.",
            exception.Message);

        _orderRepositoryMock.Verify(
            repository => repository.AddAsync(It.IsAny<Order>()),
            Times.Never);

        _orderCreatedProducerMock.Verify(
            producer => producer.PublishAsync(It.IsAny<OrderCreatedEvent>()),
            Times.Never);
    }

    [TestMethod]
    public async Task AddOrderAsync_ShouldThrowInvalidOperationException_WhenStockIsInsufficient()
    {
        var dto = new CreateOrderDto
        {
            Items =
            [
                new CreateOrderItemDto
                {
                    ProductId = 1,
                    Quantity = 10
                }
            ]
        };

        var product = new ProductDto
        {
            Id = 1,
            Name = "Café",
            Price = 20m,
            Stock = 5,
            IsActive = true
        };

        _inventoryClientMock
            .Setup(client => client.GetProductByIdAsync(1))
            .ReturnsAsync(product);

        var exception =
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                () => _orderService.AddOrderAsync(dto, 1));

        Assert.AreEqual(
            "Estoque insuficiente para o produto Café. Requisitado: 10, Disponível: 5",
            exception.Message);

        _orderRepositoryMock.Verify(
            repository => repository.AddAsync(It.IsAny<Order>()),
            Times.Never);

        _orderCreatedProducerMock.Verify(
            producer => producer.PublishAsync(It.IsAny<OrderCreatedEvent>()),
            Times.Never);
    }

    [TestMethod]
    public async Task AddOrderAsync_ShouldThrowInvalidOperationException_WhenProductIsInactive()
    {
        var dto = new CreateOrderDto
        {
            Items =
            [
                new CreateOrderItemDto
                {
                    ProductId = 1,
                    Quantity = 1
                }
            ]
        };

        var product = new ProductDto
        {
            Id = 1,
            Name = "Café",
            Price = 20m,
            Stock = 10,
            IsActive = false
        };

        _inventoryClientMock
            .Setup(client => client.GetProductByIdAsync(1))
            .ReturnsAsync(product);

        var exception =
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                () => _orderService.AddOrderAsync(dto, 1));

        Assert.AreEqual(
            "O produto Café está desativado no momento.",
            exception.Message);

        _orderRepositoryMock.Verify(
            repository => repository.AddAsync(It.IsAny<Order>()),
            Times.Never);

        _orderCreatedProducerMock.Verify(
            producer => producer.PublishAsync(It.IsAny<OrderCreatedEvent>()),
            Times.Never);
    }

    [TestMethod]
    public async Task AddOrderAsync_ShouldPublishCorrectEvent_AfterOrderIsCreated()
    {
        var dto = new CreateOrderDto
        {
            Items =
            [
                new CreateOrderItemDto
                {
                    ProductId = 1,
                    Quantity = 2
                },
                new CreateOrderItemDto
                {
                    ProductId = 2,
                    Quantity = 3
                }
            ]
        };

        _inventoryClientMock
            .Setup(client => client.GetProductByIdAsync(1))
            .ReturnsAsync(new ProductDto
            {
                Id = 1,
                Name = "Produto 1",
                Price = 10m,
                Stock = 10,
                IsActive = true
            });

        _inventoryClientMock
            .Setup(client => client.GetProductByIdAsync(2))
            .ReturnsAsync(new ProductDto
            {
                Id = 2,
                Name = "Produto 2",
                Price = 5m,
                Stock = 10,
                IsActive = true
            });

        _orderRepositoryMock
            .Setup(repository => repository.AddAsync(It.IsAny<Order>()))
            .Callback<Order>(order => order.Id = 10)
            .Returns(Task.CompletedTask);

        OrderCreatedEvent? publishedEvent = null;

        _orderCreatedProducerMock
            .Setup(producer => producer.PublishAsync(It.IsAny<OrderCreatedEvent>()))
            .Callback<OrderCreatedEvent>(orderEvent => publishedEvent = orderEvent)
            .Returns(Task.CompletedTask);

        await _orderService.AddOrderAsync(dto, 5);

        Assert.IsNotNull(publishedEvent);

        Assert.AreEqual(10, publishedEvent.OrderId);
        Assert.AreEqual(2, publishedEvent.Items.Count);

        Assert.AreEqual(1, publishedEvent.Items[0].ProductId);
        Assert.AreEqual(2, publishedEvent.Items[0].Quantity);

        Assert.AreEqual(2, publishedEvent.Items[1].ProductId);
        Assert.AreEqual(3, publishedEvent.Items[1].Quantity);
    }

    [TestMethod]
    public async Task GetOrderByIdAsync_ShouldReturnOrder_WhenOrderExists()
    {
        var order = CreateOrder();

        _orderRepositoryMock
            .Setup(repository => repository.GetByIdAsync(1))
            .ReturnsAsync(order);

        var result = await _orderService.GetOrderByIdAsync(1);

        Assert.IsNotNull(result);
        Assert.AreEqual(order.Id, result.Id);
        Assert.AreEqual(order.UserId, result.UserId);
        Assert.AreEqual(order.TotalAmount, result.TotalAmount);
        Assert.AreEqual(order.Status, result.Status);
        Assert.AreEqual(order.Items.Count, result.Items.Count);
    }

    [TestMethod]
    public async Task GetOrderByIdAsync_ShouldReturnNull_WhenOrderDoesNotExist()
    {
        _orderRepositoryMock
            .Setup(repository => repository.GetByIdAsync(1))
            .ReturnsAsync((Order?)null);

        var result = await _orderService.GetOrderByIdAsync(1);

        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetAllOrdersAsync_ShouldReturnAllOrders()
    {
        var orders = new List<Order>
        {
            CreateOrder(1, 10),
            CreateOrder(2, 20)
        };

        _orderRepositoryMock
            .Setup(repository => repository.GetAllAsync())
            .ReturnsAsync(orders);

        var result = (await _orderService.GetAllOrdersAsync()).ToList();

        Assert.AreEqual(2, result.Count);
        Assert.AreEqual(1, result[0].Id);
        Assert.AreEqual(2, result[1].Id);
    }

    [TestMethod]
    public async Task GetAllOrdersAsync_ShouldReturnEmptyCollection_WhenNoOrdersExist()
    {
        _orderRepositoryMock
            .Setup(repository => repository.GetAllAsync())
            .ReturnsAsync([]);

        var result = await _orderService.GetAllOrdersAsync();

        Assert.IsNotNull(result);
        Assert.IsFalse(result.Any());
    }

    [TestMethod]
    public async Task GetByUserIdAsync_ShouldReturnOnlyUserOrders()
    {
        var orders = new List<Order>
        {
            CreateOrder(1, 5),
            CreateOrder(2, 5)
        };

        _orderRepositoryMock
            .Setup(repository => repository.GetByUserIdAsync(5))
            .ReturnsAsync(orders);

        var result =
            (await _orderService.GetByUserIdAsync(5)).ToList();

        Assert.AreEqual(2, result.Count);

        Assert.IsTrue(
            result.All(order => order.UserId == 5));

        _orderRepositoryMock.Verify(
            repository => repository.GetByUserIdAsync(5),
            Times.Once);
    }

    [TestMethod]
    public async Task GetByUserIdAsync_ShouldReturnEmptyCollection_WhenUserHasNoOrders()
    {
        _orderRepositoryMock
            .Setup(repository => repository.GetByUserIdAsync(5))
            .ReturnsAsync([]);

        var result =
            await _orderService.GetByUserIdAsync(5);

        Assert.IsNotNull(result);
        Assert.IsFalse(result.Any());
    }

    [TestMethod]
    public async Task UpdateOrderStatusAsync_ShouldUpdateStatus_WhenOrderExists()
    {
        var order = CreateOrder();

        order.Status = OrderStatusEnum.Pending;

        _orderRepositoryMock
            .Setup(repository => repository.GetByIdAsync(1))
            .ReturnsAsync(order);

        _orderRepositoryMock
            .Setup(repository => repository.UpdateAsync(order))
            .Returns(Task.CompletedTask);

        var result =
            await _orderService.UpdateOrderStatusAsync(
                1,
                OrderStatusEnum.Completed);

        Assert.AreEqual(
            OrderStatusEnum.Completed,
            order.Status);

        Assert.AreEqual(
            OrderStatusEnum.Completed,
            result.Status);

        _orderRepositoryMock.Verify(
            repository => repository.UpdateAsync(order),
            Times.Once);
    }

    [TestMethod]
    public async Task UpdateOrderStatusAsync_ShouldThrowKeyNotFoundException_WhenOrderDoesNotExist()
    {
        _orderRepositoryMock
            .Setup(repository => repository.GetByIdAsync(99))
            .ReturnsAsync((Order?)null);

        var exception =
            await Assert.ThrowsExceptionAsync<KeyNotFoundException>(
                () => _orderService.UpdateOrderStatusAsync(
                    99,
                    OrderStatusEnum.Completed));

        Assert.AreEqual(
            "Pedido com ID 99 não encontrado.",
            exception.Message);

        _orderRepositoryMock.Verify(
            repository => repository.UpdateAsync(It.IsAny<Order>()),
            Times.Never);
    }

    private static Order CreateOrder(
        int id = 1,
        int userId = 5)
    {
        return new Order
        {
            Id = id,
            UserId = userId,
            TotalAmount = 20m,
            Status = OrderStatusEnum.Pending,
            Items =
            [
                new OrderItem
                {
                    ProductId = 1,
                    ProductName = "Café",
                    UnitPrice = 10m,
                    Quantity = 2
                }
            ]
        };
    }
}