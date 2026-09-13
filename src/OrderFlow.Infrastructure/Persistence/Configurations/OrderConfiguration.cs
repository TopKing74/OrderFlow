using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderFlow.Domain.Entities;

namespace OrderFlow.Infrastructure.Persistence.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");

        builder.HasKey(order => order.Id);

        builder.Property(order => order.Id)
            .ValueGeneratedNever();

        builder.Property(order => order.CustomerName)
            .HasColumnType("nvarchar(200)")
            .IsRequired();

        builder.Property(order => order.Status)
            .HasConversion<string>()
            .HasColumnType("nvarchar(20)")
            .IsRequired();

        builder.Property(order => order.TotalAmount)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(order => order.CreatedAtUtc)
            .HasColumnType("datetime2");

        builder.HasMany(order => order.Items)
            .WithOne()
            .HasForeignKey(item => item.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(order => order.CustomerName);

        builder.HasIndex(order => order.CreatedAtUtc);
    }
}
