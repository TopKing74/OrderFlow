using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderFlow.Domain.Entities;

namespace OrderFlow.Infrastructure.Persistence.Configurations;

public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems");

        builder.HasKey(item => item.Id);

        builder.Property(item => item.Id)
            .ValueGeneratedNever();

        builder.Property(item => item.ProductName)
            .HasColumnType("nvarchar(200)")
            .IsRequired();

        builder.Property(item => item.UnitPrice)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(item => item.LineTotal)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.HasIndex(item => item.OrderId);
    }
}
