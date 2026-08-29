using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Shuttlez.Application.Admin.Services;
using Shuttlez.Application.Bookings;
using Shuttlez.Application.Common.Behaviors;
using Shuttlez.Application.Groups;
using Shuttlez.Application.Pricing;
using Shuttlez.Application.Rides;

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
        services.AddScoped<IRouteDemandReadinessEnricher, RouteDemandReadinessEnricher>();
        services.AddScoped<IRouteDemandLaunchService, RouteDemandLaunchService>();
        services.AddScoped<IRoutePolylineService, RoutePolylineService>();
        services.AddScoped<IShuttleCommissionResolver, ShuttleCommissionResolver>();
        services.AddScoped<IPricingRuleResolver, PricingRuleResolver>();
        services.AddScoped<IRideFareResolver, RideFareResolver>();
        services.AddScoped<IGroupFareResolver, GroupFareResolver>();
        services.AddScoped<ITripDistanceService, TripDistanceService>();

        return services;
    }
}
