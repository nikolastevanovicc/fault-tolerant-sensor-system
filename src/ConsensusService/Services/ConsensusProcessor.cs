using Microsoft.EntityFrameworkCore;
using Persistence;
using Persistence.Entities;
using Shared.Enums;

namespace ConsensusService.Services;

public sealed class ConsensusProcessor : IConsensusProcessor
{
    private const string AlgorithmName = "TrimmedMeanBft";

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
        var now = DateTime.UtcNow;
        var periodEnd = new DateTime(
            now.Year,
            now.Month,
            now.Day,
            now.Hour,
            now.Minute,
            0,
            DateTimeKind.Utc);
        var periodStart = periodEnd.AddMinutes(-1);

        var alreadyProcessed = await _dbContext.ConsensusReadings
            .AsNoTracking()
            .AnyAsync(
                reading =>
                    reading.PeriodStart == periodStart
                    && reading.PeriodEnd == periodEnd,
                cancellationToken);

        if (alreadyProcessed)
        {
            _logger.LogInformation(
                "Consensus period was already processed. PeriodStart={PeriodStart}, PeriodEnd={PeriodEnd}",
                periodStart,
                periodEnd);
            return;
        }

        var readingPeriodStart = new DateTimeOffset(periodStart);
        var readingPeriodEnd = new DateTimeOffset(periodEnd);

        var rawReadings = await _dbContext.SensorReadings
            .AsNoTracking()
            .Where(reading =>
                !reading.IsConsensusValue
                && reading.Timestamp >= readingPeriodStart
                && reading.Timestamp < readingPeriodEnd)
            .Select(reading => new
            {
                reading.SensorId,
                reading.Temperature,
                reading.Quality
            })
            .ToListAsync(cancellationToken);

        var rawReadingCount = rawReadings.Count;
        var goodSensorValues = rawReadings
            .Where(reading => reading.Quality == DataQuality.Good)
            .GroupBy(reading => reading.SensorId)
            .Select(group => group.Average(reading => reading.Temperature))
            .ToList();

        _logger.LogInformation(
            "Consensus readings loaded. PeriodStart={PeriodStart}, PeriodEnd={PeriodEnd}, RawReadingCount={RawReadingCount}, GoodSensorCount={GoodSensorCount}",
            periodStart,
            periodEnd,
            rawReadingCount,
            goodSensorValues.Count);

        if (goodSensorValues.Count < 3)
        {
            _logger.LogWarning(
                "Not enough GOOD sensor values to calculate consensus. PeriodStart={PeriodStart}, PeriodEnd={PeriodEnd}, RawReadingCount={RawReadingCount}, GoodSensorCount={GoodSensorCount}",
                periodStart,
                periodEnd,
                rawReadingCount,
                goodSensorValues.Count);
            return;
        }

        var valuesUsed = GetValuesForConsensus(goodSensorValues);
        var consensusValue = valuesUsed.Average();

        _dbContext.ConsensusReadings.Add(new ConsensusReadingEntity
        {
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            Value = consensusValue,
            UsedSensorCount = valuesUsed.Count,
            RawReadingCount = rawReadingCount,
            Algorithm = AlgorithmName,
            CreatedAt = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Consensus calculated and stored. PeriodStart={PeriodStart}, PeriodEnd={PeriodEnd}, RawReadingCount={RawReadingCount}, GoodSensorCount={GoodSensorCount}, UsedSensorCount={UsedSensorCount}, ConsensusValue={ConsensusValue}",
            periodStart,
            periodEnd,
            rawReadingCount,
            goodSensorValues.Count,
            valuesUsed.Count,
            consensusValue);
    }

    private static List<double> GetValuesForConsensus(IEnumerable<double> sensorValues)
    {
        var sortedValues = sensorValues.Order().ToList();

        return sortedValues.Count >= 5
            ? sortedValues.GetRange(1, sortedValues.Count - 2)
            : sortedValues;
    }
}
