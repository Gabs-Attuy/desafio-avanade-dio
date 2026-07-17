using System.Text;
using System.Text.Json;
using InventoryService.Interfaces;
using InventoryService.Messaging.Events;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace InventoryService.Messaging.Consumers;

public class OrderCreatedConsumer : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<OrderCreatedConsumer> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    private IConnection? _connection;
    private IChannel? _channel;

    public OrderCreatedConsumer(
        IConfiguration configuration,
        ILogger<OrderCreatedConsumer> logger,
        IServiceScopeFactory scopeFactory)
    {
        _configuration = configuration;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _configuration["RabbitMq:HostName"]!,
            Port = int.Parse(_configuration["RabbitMq:Port"]!),
            UserName = _configuration["RabbitMq:UserName"]!,
            Password = _configuration["RabbitMq:Password"]!
        };

        _connection = await factory.CreateConnectionAsync(stoppingToken);

        _channel = await _connection.CreateChannelAsync(
            cancellationToken: stoppingToken);

        var exchangeName =
            _configuration["RabbitMq:ExchangeName"]!;

        var queueName =
            _configuration["RabbitMq:OrderCreatedQueue"]!;

        var routingKey =
            _configuration["RabbitMq:OrderCreatedRoutingKey"]!;

        await _channel.ExchangeDeclareAsync(
            exchange: exchangeName,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await _channel.QueueDeclareAsync(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await _channel.QueueBindAsync(
            queue: queueName,
            exchange: exchangeName,
            routingKey: routingKey,
            cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (_, eventArgs) =>
        {
            try
            {
                var message = Encoding.UTF8.GetString(
                    eventArgs.Body.ToArray());

                var orderCreatedEvent =
                    JsonSerializer.Deserialize<OrderCreatedEvent>(
                        message);

                if (orderCreatedEvent is null)
                {
                    _logger.LogWarning(
                        "Não foi possível desserializar o evento recebido.");

                    await _channel.BasicNackAsync(
                        deliveryTag: eventArgs.DeliveryTag,
                        multiple: false,
                        requeue: false);

                    return;
                }

                _logger.LogInformation(
                    "Processando pedido {OrderId}.",
                    orderCreatedEvent.OrderId);

                using var scope = _scopeFactory.CreateScope();

                var productService =
                    scope.ServiceProvider
                        .GetRequiredService<IProductService>();

                var items = orderCreatedEvent.Items
                    .Select(item => (
                        item.ProductId,
                        item.Quantity
                    ));

                await productService.DecreaseStockAsync(items);

                await _channel.BasicAckAsync(
                    deliveryTag: eventArgs.DeliveryTag,
                    multiple: false);

                _logger.LogInformation(
                    "Pedido {OrderId} processado com sucesso.",
                    orderCreatedEvent.OrderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro ao processar mensagem do RabbitMQ.");

                await _channel.BasicNackAsync(
                    deliveryTag: eventArgs.DeliveryTag,
                    multiple: false,
                    requeue: false);
            }
        };

        await _channel.BasicConsumeAsync(
            queue: queueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        _logger.LogInformation(
            "Consumer iniciado e aguardando mensagens na fila {QueueName}.",
            queueName);

        await Task.Delay(
            Timeout.Infinite,
            stoppingToken);
    }
}