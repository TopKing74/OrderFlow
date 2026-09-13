using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using OrderFlow.Application.Common.Behaviors;

namespace OrderFlow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(mediatorConfiguration =>
        {
            mediatorConfiguration.RegisterServicesFromAssembly(assembly);
            mediatorConfiguration.AddOpenBehavior(typeof(LoggingBehavior<,>));
            mediatorConfiguration.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}
