using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NymrSignIn.Application.Register;

namespace NymrSignIn.Infrastructure.BackgroundJobs;

public sealed class DailyEmailBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeProvider _timeProvider;
    private readonly SiteOptions _siteOptions;
    private readonly ILogger<DailyEmailBackgroundService> _logger;

    private const int TargetHour = 23;
    private const int TargetMinute = 0;

    public DailyEmailBackgroundService(
        IServiceScopeFactory scopeFactory,
        TimeProvider timeProvider,
        IOptions<SiteOptions> siteOptions,
        ILogger<DailyEmailBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _timeProvider = timeProvider;
        _siteOptions = siteOptions.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Daily email background service started. Target time: {Hour}:{Minute:D2} UK",
            TargetHour, TargetMinute);

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = CalculateDelayUntilNextRun();
            _logger.LogInformation("Next daily email scheduled in {Delay}", delay);

            await Task.Delay(delay, stoppingToken);

            await SendDailyEmailAsync(stoppingToken);
        }
    }

    private async Task SendDailyEmailAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var registerService = scope.ServiceProvider.GetRequiredService<RegisterService>();
            await registerService.SendDailyEmailAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Graceful shutdown, do not log as error
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send daily register email");
        }
    }

    private TimeSpan CalculateDelayUntilNextRun()
    {
        var ukZone = TimeZoneInfo.FindSystemTimeZoneById(_siteOptions.TimeZone);
        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        var ukNow = TimeZoneInfo.ConvertTimeFromUtc(utcNow, ukZone);

        var targetToday = new DateTime(ukNow.Year, ukNow.Month, ukNow.Day, TargetHour, TargetMinute, 0);

        var target = ukNow < targetToday
            ? targetToday
            : targetToday.AddDays(1);

        var targetUtc = TimeZoneInfo.ConvertTimeToUtc(target, ukZone);
        var delay = targetUtc - utcNow;

        return delay > TimeSpan.Zero ? delay : TimeSpan.FromMinutes(1);
    }
}
