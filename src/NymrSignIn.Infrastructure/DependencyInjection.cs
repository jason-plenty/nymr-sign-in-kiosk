using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NymrSignIn.Application.Register;
using NymrSignIn.Infrastructure.BackgroundJobs;
using NymrSignIn.Infrastructure.BlobStorage;
using NymrSignIn.Infrastructure.Email;
using NymrSignIn.Infrastructure.Persistence;
using NymrSignIn.Infrastructure.Persistence.Register;
using NymrSignIn.Infrastructure.Register;

namespace NymrSignIn.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sqlOptions => sqlOptions.EnableRetryOnFailure()));

        services.AddScoped<IRegisterRepository, RegisterRepository>();

        var blobStorageSection = configuration.GetSection(BlobStorageOptions.SectionName);
        services.Configure<BlobStorageOptions>(blobStorageSection);

        var blobConnectionString = blobStorageSection.GetValue<string>("ConnectionString");
        var blobClientOptions = BuildBlobClientOptions(blobConnectionString);
        if (!string.IsNullOrWhiteSpace(blobConnectionString))
        {
            services.AddSingleton(new BlobServiceClient(blobConnectionString, blobClientOptions));
        }
        else
        {
            var blobServiceUri = blobStorageSection.GetValue<string>("ServiceUri");
            services.AddSingleton(new BlobServiceClient(
                new Uri(blobServiceUri ?? "https://localhost"),
                new DefaultAzureCredential(),
                blobClientOptions));
        }

        services.AddScoped<ISignatureStorage, SignatureStorageService>();

        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        services.AddScoped<IRegisterEmailService, GraphEmailService>();

        services.Configure<SiteOptions>(configuration.GetSection(SiteOptions.SectionName));
        services.AddSingleton<ISiteCodeGenerator, SiteCodeGenerator>();
        services.AddHostedService<DailyEmailBackgroundService>();

        return services;
    }

    private static BlobClientOptions BuildBlobClientOptions(string? connectionString)
    {
        var options = new BlobClientOptions();
        var isDevelopmentEmulator = !string.IsNullOrWhiteSpace(connectionString)
            && connectionString.Contains("UseDevelopmentStorage=true", StringComparison.OrdinalIgnoreCase);

        if (isDevelopmentEmulator)
        {
            options.Retry.MaxRetries = 0;
            options.Retry.NetworkTimeout = TimeSpan.FromSeconds(2);
        }

        return options;
    }
}
