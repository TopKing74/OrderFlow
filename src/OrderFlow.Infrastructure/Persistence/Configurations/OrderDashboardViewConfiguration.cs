using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderFlow.Infrastructure.Persistence.ReadModels;

namespace OrderFlow.Infrastructure.Persistence.Configurations;

public sealed class OrderDashboardViewConfiguration : IEntityTypeConfiguration<OrderDashboardView>
{
    public void Configure(EntityTypeBuilder<OrderDashboardView> builder)
    {
        builder.ToTable("OrdersDashboard");

        builder.HasKey(view => view.Id);

        builder.Property(view => view.Id)
            .ValueGeneratedNever();

        builder.Property(view => view.CustomerName)
            .HasColumnType("nvarchar(200)")
            .IsRequired();

        builder.Property(view => view.TotalAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(view => view.Status)
            .HasColumnType("nvarchar(20)")
            .IsRequired();

        builder.Property(view => view.LastUpdatedUtc)
            .HasColumnType("datetime2");

        builder.HasIndex(view => view.OrderId)
            .IsUnique();

        builder.HasIndex(view => view.LastUpdatedUtc);
    }
}
