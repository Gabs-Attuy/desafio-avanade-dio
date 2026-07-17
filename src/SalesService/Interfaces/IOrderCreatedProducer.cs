using SalesService.Messaging.Events;

namespace SalesService.Interfaces;

public interface IOrderCreatedProducer
{
    Task PublishAsync(OrderCreatedEvent orderCreatedEvent);
}