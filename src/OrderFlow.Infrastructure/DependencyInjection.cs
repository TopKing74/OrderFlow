using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Infrastructure.BackgroundJobs;
using OrderFlow.Infrastructure.Caching;
using OrderFlow.Infrastructure.Persistence;

namespace OrderFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' was not found.");

        var redisConnectionString = configuration.GetConnectionString("Redis")
            ?? throw new InvalidOperationException(
                "Connection string 'Redis' was not found.");

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sqlServerOptions =>
            {
                sqlServerOptions.EnableRetryOnFailure();
            });
        });

        services.AddScoped<IApplicationDbContext>(serviceProvider =>
            serviceProvider.GetRequiredService<ApplicationDbContext>());

        services.AddSingleton<ISqlConnectionFactory>(new SqlConnectionFactory(connectionString));

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = "orderflow:";
        });

        services.AddSingleton<ICacheService, RedisCacheService>();

        services.AddHostedService<PendingOrderProcessorJob>();
        services.AddHostedService<DashboardMaterializedViewRefresherJob>();

        return services;
    }
}
