using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using SalesService.Interfaces;
using SalesService.Messaging.Events;

namespace SalesService.Messaging.Producers;

public class OrderCreatedProducer : IOrderCreatedProducer
{
    private readonly IConfiguration _configuration;

    public OrderCreatedProducer(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task PublishAsync(OrderCreatedEvent orderCreatedEvent)
    {
        var factory = new ConnectionFactory
        {
            HostName = _configuration["RabbitMq:HostName"]!,
            Port = int.Parse(_configuration["RabbitMq:Port"]!),
            UserName = _configuration["RabbitMq:UserName"]!,
            Password = _configuration["RabbitMq:Password"]!
        };

        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();

        var exchangeName =
            _configuration["RabbitMq:ExchangeName"]!;

        var routingKey =
            _configuration["RabbitMq:OrderCreatedRoutingKey"]!;

        await channel.ExchangeDeclareAsync(
            exchange: exchangeName,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false
        );

        var message = JsonSerializer.Serialize(orderCreatedEvent);

        var body = Encoding.UTF8.GetBytes(message);

        await channel.BasicPublishAsync(
            exchange: exchangeName,
            routingKey: routingKey,
            body: body
        );
    }
}