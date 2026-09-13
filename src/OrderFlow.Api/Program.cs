using Microsoft.EntityFrameworkCore;
using OrderFlow.Api.Middleware;
using OrderFlow.Application;
using OrderFlow.Infrastructure;
using OrderFlow.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "OrderFlow API",
        Version = "v1",
        Description = "Order management API with CQRS, Dapper read models, Redis caching and materialized dashboards."
    });
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
builder.Services.AddExceptionHandler<NotFoundExceptionHandler>();
builder.Services.AddExceptionHandler<UnhandledExceptionHandler>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "OrderFlow API v1");
    });
}

app.UseExceptionHandler();

app.MapControllers();

await ApplyMigrationsAsync(app);
await app.RunAsync();

static async Task ApplyMigrationsAsync(WebApplication application)
{
    using var scope = application.Services.CreateScope();

    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
        .CreateLogger("Migrations");

    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        if ((await dbContext.Database.GetPendingMigrationsAsync()).Any())
        {
            logger.LogInformation("Applying pending database migrations...");
            await dbContext.Database.MigrateAsync();
            logger.LogInformation("Database migrations applied.");
        }
        else
        {
            logger.LogInformation("Database is up to date. No pending migrations.");
        }
    }
    catch (Exception exception)
    {
        logger.LogCritical(exception, "Database migration failed on startup.");
        throw;
    }
}
