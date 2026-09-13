namespace OrderFlow.Infrastructure.Persistence.ReadModels;

public sealed class OrderDashboardView
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public string CustomerName { get; set; } = null!;

    public int ItemCount { get; set; }

    public decimal TotalAmount { get; set; }

    public string Status { get; set; } = null!;

    public DateTime LastUpdatedUtc { get; set; }
}
