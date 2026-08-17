using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Shuttlez.Application.Admin.Services;
using Shuttlez.Application.Common.Behaviors;

namespace Shuttlez.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddScoped<ICorridorDemandService, CorridorDemandService>();
        services.AddScoped<IRouteDemandAnalysisService, RouteDemandAnalysisService>();
        services.AddScoped<IRoutePolylineService, RoutePolylineService>();

        return services;
    }
}
