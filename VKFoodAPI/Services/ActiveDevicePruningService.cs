namespace VKFoodAPI.Services;

public class ActiveDevicePruningService : BackgroundService
{
    private static readonly TimeSpan PruneInterval = TimeSpan.FromSeconds(5);

    private readonly ActiveDeviceRepository _repository;
    private readonly ILogger<ActiveDevicePruningService> _logger;

    public ActiveDevicePruningService(
        ActiveDeviceRepository repository,
        ILogger<ActiveDevicePruningService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PruneInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await timer.WaitForNextTickAsync(stoppingToken);
                _repository.PruneInactive();
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not prune inactive app devices.");
            }
        }
    }
}
