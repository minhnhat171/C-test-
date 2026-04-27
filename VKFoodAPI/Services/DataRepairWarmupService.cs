namespace VKFoodAPI.Services;

public sealed class DataRepairWarmupService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;

    public DataRepairWarmupService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        _ = scope.ServiceProvider.GetRequiredService<PoiRepository>();
        _ = scope.ServiceProvider.GetRequiredService<AudioGuideRepository>();
        _ = scope.ServiceProvider.GetRequiredService<ListeningHistoryRepository>();
        _ = scope.ServiceProvider.GetRequiredService<QrCodeRepository>();
        _ = scope.ServiceProvider.GetRequiredService<UserManagementRepository>();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
