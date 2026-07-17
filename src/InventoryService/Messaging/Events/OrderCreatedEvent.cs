namespace InventoryService.Messaging.Events;

public class OrderCreatedEvent
{
    public int OrderId { get; set; }

    public List<OrderCreatedItemEvent> Items { get; set; } = [];
}

public class OrderCreatedItemEvent
{
    public int ProductId { get; set; }

    public int Quantity { get; set; }
}