using Microsoft.EntityFrameworkCore;
using Persistence;

namespace ConsensusService.Services;

public sealed class ConsensusProcessor : IConsensusProcessor
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<ConsensusProcessor> _logger;

    public ConsensusProcessor(
        AppDbContext dbContext,
        ILogger<ConsensusProcessor> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task ProcessPreviousMinuteAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var periodEnd = new DateTimeOffset(
            now.Year,
            now.Month,
            now.Day,
            now.Hour,
            now.Minute,
            0,
            TimeSpan.Zero);
        var periodStart = periodEnd.AddMinutes(-1);

        var rawReadingCount = await _dbContext.SensorReadings
            .AsNoTracking()
            .CountAsync(
                reading =>
                    !reading.IsConsensusValue
                    && reading.Timestamp >= periodStart
                    && reading.Timestamp < periodEnd,
                cancellationToken);

        _logger.LogInformation(
            "Previous minute readings queried. PeriodStart={PeriodStart}, PeriodEnd={PeriodEnd}, RawReadingCount={RawReadingCount}",
            periodStart,
            periodEnd,
            rawReadingCount);
    }
}
