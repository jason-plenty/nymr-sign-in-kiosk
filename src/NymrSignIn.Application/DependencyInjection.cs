using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using NymrSignIn.Application.Register;
using NymrSignIn.Application.Register.Admin;

namespace NymrSignIn.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<RegisterService>();
        services.AddScoped<AdminRegisterService>();
        services.AddValidatorsFromAssemblyContaining<RegisterService>();
        return services;
    }
}
