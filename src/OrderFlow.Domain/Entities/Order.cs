using OrderFlow.Domain.Enums;

namespace OrderFlow.Domain.Entities;

public sealed class Order
{
    private readonly List<OrderItem> _items = [];

    public Guid Id { get; private set; }

    public string CustomerName { get; private set; } = null!;

    public OrderStatus Status { get; private set; }

    public decimal TotalAmount { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    private Order()
    {
    }

    public static Order Create(string customerName, IEnumerable<OrderItem> items)
    {
        if (string.IsNullOrWhiteSpace(customerName))
        {
            throw new ArgumentException("Customer name is required.", nameof(customerName));
        }

        var orderItems = items?.ToList();

        if (orderItems is null || orderItems.Count == 0)
        {
            throw new ArgumentException("An order must contain at least one item.", nameof(items));
        }

        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerName = customerName.Trim(),
            Status = OrderStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };

        order._items.AddRange(orderItems);
        order.TotalAmount = orderItems.Sum(item => item.LineTotal);

        return order;
    }

    public void MarkCompleted()
    {
        if (Status != OrderStatus.Pending)
        {
            throw new InvalidOperationException(
                $"Only pending orders can be marked as completed. Order '{Id}' is currently '{Status}'.");
        }

        Status = OrderStatus.Completed;
    }
}
