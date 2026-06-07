using ConsensusService.Services;

namespace ConsensusService;

public sealed class Worker : BackgroundService
{
    private const int DefaultIntervalSeconds = 60;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<Worker> _logger;
    private readonly TimeSpan _interval;

    public Worker(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<Worker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

        var intervalSeconds = configuration.GetValue(
            "Consensus:IntervalSeconds",
            DefaultIntervalSeconds);

        _interval = TimeSpan.FromSeconds(
            intervalSeconds > 0 ? intervalSeconds : DefaultIntervalSeconds);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Consensus worker started. Processing interval: {IntervalSeconds} seconds.",
            _interval.TotalSeconds);

        await ProcessAsync(stoppingToken);

        using var timer = new PeriodicTimer(_interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await ProcessAsync(stoppingToken);
        }
    }

    private async Task ProcessAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting consensus processing attempt.");

            using var scope = _scopeFactory.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<IConsensusProcessor>();

            await processor.ProcessPreviousMinuteAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Consensus processing attempt canceled.");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Consensus processing attempt failed.");
        }
    }
}
