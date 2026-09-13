namespace OrderFlow.Application.Common.Caching;

public static class CacheKeys
{
    public const string OrdersList = "orders:list";

    public const string OrdersDashboard = "orders:dashboard";

    public static string OrderDetails(Guid orderId) => $"orders:details:{orderId}";
}
