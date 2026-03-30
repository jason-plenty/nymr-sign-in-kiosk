using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using NymrSignIn.Application.Register;

namespace NymrSignIn.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<RegisterService>();
        services.AddValidatorsFromAssemblyContaining<RegisterService>();
        return services;
    }
}
