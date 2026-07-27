using FaydamPDKS.Core.Interfaces;

namespace FaydamPDKS.Web;

public sealed class PublicHolidayCalendarWorker(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<PublicHolidayCalendarWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await SyncCurrentYearsAsync(stoppingToken);
            try
            {
                await Task.Delay(TimeSpan.FromHours(24), timeProvider, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task SyncCurrentYearsAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var sync = scope.ServiceProvider.GetRequiredService<IPublicHolidaySyncService>();
            var currentYear = timeProvider.GetLocalNow().Year;
            await sync.SyncYearAsync(currentYear, cancellationToken);
            await sync.SyncYearAsync(currentYear + 1, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Resmî tatil takvimi otomatik güncellenemedi; son başarılı kayıtlar korunuyor.");
        }
    }
}
