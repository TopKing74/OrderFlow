namespace OrderFlow.Domain.Entities;

public sealed class OrderItem
{
    public Guid Id { get; private set; }

    public Guid OrderId { get; private set; }

    public string ProductName { get; private set; } = null!;

    public int Quantity { get; private set; }

    public decimal UnitPrice { get; private set; }

    public decimal LineTotal { get; private set; }

    private OrderItem()
    {
    }

    public static OrderItem Create(string productName, int quantity, decimal unitPrice)
    {
        if (string.IsNullOrWhiteSpace(productName))
        {
            throw new ArgumentException("Product name is required.", nameof(productName));
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "Quantity must be greater than zero.");
        }

        if (unitPrice <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(unitPrice), unitPrice, "Unit price must be greater than zero.");
        }

        return new OrderItem
        {
            Id = Guid.NewGuid(),
            ProductName = productName.Trim(),
            Quantity = quantity,
            UnitPrice = unitPrice,
            LineTotal = quantity * unitPrice
        };
    }
}
