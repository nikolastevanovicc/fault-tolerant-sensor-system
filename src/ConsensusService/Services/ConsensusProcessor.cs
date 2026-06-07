using Microsoft.EntityFrameworkCore;
using Persistence;
using Persistence.Entities;
using Shared.Enums;

namespace ConsensusService.Services;

public sealed class ConsensusProcessor : IConsensusProcessor
{
    private const string AlgorithmName = "TrimmedMeanBft";
    private const double DefaultDeviationThreshold = 10.0;
    private const int DefaultMaxConsecutiveDeviations = 3;

    private readonly AppDbContext _dbContext;
    private readonly ILogger<ConsensusProcessor> _logger;
    private readonly double _deviationThreshold;
    private readonly int _maxConsecutiveDeviations;

    public ConsensusProcessor(
        AppDbContext dbContext,
        IConfiguration configuration,
        ILogger<ConsensusProcessor> logger)
    {
        _dbContext = dbContext;
        _logger = logger;

        var configuredDeviationThreshold = configuration.GetValue(
            "MaliciousDetection:DeviationThreshold",
            DefaultDeviationThreshold);
        var configuredMaxConsecutiveDeviations = configuration.GetValue(
            "MaliciousDetection:MaxConsecutiveDeviations",
            DefaultMaxConsecutiveDeviations);

        _deviationThreshold = configuredDeviationThreshold > 0
            ? configuredDeviationThreshold
            : DefaultDeviationThreshold;
        _maxConsecutiveDeviations = configuredMaxConsecutiveDeviations > 0
            ? configuredMaxConsecutiveDeviations
            : DefaultMaxConsecutiveDeviations;

        _logger.LogInformation(
            "Malicious sensor detection configured. DeviationThreshold={DeviationThreshold}, MaxConsecutiveDeviations={MaxConsecutiveDeviations}",
            _deviationThreshold,
            _maxConsecutiveDeviations);
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
        var goodSensorAverages = rawReadings
            .Where(reading => reading.Quality == DataQuality.Good)
            .GroupBy(reading => reading.SensorId)
            .Select(group => new SensorAverage(
                group.Key,
                group.Average(reading => reading.Temperature)))
            .ToList();

        _logger.LogInformation(
            "Consensus readings loaded. PeriodStart={PeriodStart}, PeriodEnd={PeriodEnd}, RawReadingCount={RawReadingCount}, GoodSensorCount={GoodSensorCount}",
            periodStart,
            periodEnd,
            rawReadingCount,
            goodSensorAverages.Count);

        if (goodSensorAverages.Count < 3)
        {
            _logger.LogWarning(
                "Not enough GOOD sensor values to calculate consensus. PeriodStart={PeriodStart}, PeriodEnd={PeriodEnd}, RawReadingCount={RawReadingCount}, GoodSensorCount={GoodSensorCount}",
                periodStart,
                periodEnd,
                rawReadingCount,
                goodSensorAverages.Count);
            return;
        }

        var valuesUsed = GetValuesForConsensus(goodSensorAverages.Select(sensor => sensor.Average));
        var consensusValue = valuesUsed.Average();
        var updatedAt = DateTime.UtcNow;

        _dbContext.ConsensusReadings.Add(new ConsensusReadingEntity
        {
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            Value = consensusValue,
            UsedSensorCount = valuesUsed.Count,
            RawReadingCount = rawReadingCount,
            Algorithm = AlgorithmName,
            CreatedAt = updatedAt
        });

        await DetectMaliciousSensorsAsync(
            goodSensorAverages,
            consensusValue,
            updatedAt,
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Consensus calculated and stored. PeriodStart={PeriodStart}, PeriodEnd={PeriodEnd}, RawReadingCount={RawReadingCount}, GoodSensorCount={GoodSensorCount}, UsedSensorCount={UsedSensorCount}, ConsensusValue={ConsensusValue}",
            periodStart,
            periodEnd,
            rawReadingCount,
            goodSensorAverages.Count,
            valuesUsed.Count,
            consensusValue);
    }

    private async Task DetectMaliciousSensorsAsync(
        IReadOnlyCollection<SensorAverage> sensorAverages,
        double consensusValue,
        DateTime updatedAt,
        CancellationToken cancellationToken)
    {
        var sensorIds = sensorAverages.Select(sensor => sensor.SensorId).ToList();
        var anomalyStates = await _dbContext.SensorAnomalyStates
            .Where(state => sensorIds.Contains(state.SensorId))
            .ToDictionaryAsync(state => state.SensorId, cancellationToken);
        var sensorStates = await _dbContext.SensorStates
            .Where(state => sensorIds.Contains(state.SensorId))
            .ToDictionaryAsync(state => state.SensorId, cancellationToken);

        foreach (var sensorAverage in sensorAverages)
        {
            var deviation = Math.Abs(sensorAverage.Average - consensusValue);

            if (!anomalyStates.TryGetValue(sensorAverage.SensorId, out var anomalyState))
            {
                anomalyState = new SensorAnomalyStateEntity
                {
                    SensorId = sensorAverage.SensorId
                };
                anomalyStates.Add(sensorAverage.SensorId, anomalyState);
                _dbContext.SensorAnomalyStates.Add(anomalyState);
            }

            anomalyState.LastDeviation = deviation;
            anomalyState.LastUpdatedAt = updatedAt;

            if (deviation > _deviationThreshold)
            {
                anomalyState.ConsecutiveDeviationCount++;

                _logger.LogWarning(
                    "Suspicious sensor deviation detected. SensorId={SensorId}, Deviation={Deviation}, ConsecutiveDeviationCount={ConsecutiveDeviationCount}, DeviationThreshold={DeviationThreshold}",
                    sensorAverage.SensorId,
                    deviation,
                    anomalyState.ConsecutiveDeviationCount,
                    _deviationThreshold);

                if (!sensorStates.TryGetValue(sensorAverage.SensorId, out var sensorState))
                {
                    _logger.LogWarning(
                        "Sensor state was not found for suspicious sensor. SensorId={SensorId}",
                        sensorAverage.SensorId);
                    continue;
                }

                if (anomalyState.ConsecutiveDeviationCount >= _maxConsecutiveDeviations
                    && sensorState.Quality != DataQuality.Bad)
                {
                    sensorState.Quality = DataQuality.Bad;

                    _logger.LogWarning(
                        "Sensor marked as BAD after consecutive suspicious deviations. SensorId={SensorId}, ConsecutiveDeviationCount={ConsecutiveDeviationCount}, MaxConsecutiveDeviations={MaxConsecutiveDeviations}",
                        sensorAverage.SensorId,
                        anomalyState.ConsecutiveDeviationCount,
                        _maxConsecutiveDeviations);
                }

                continue;
            }

            anomalyState.ConsecutiveDeviationCount = 0;

            _logger.LogInformation(
                "Sensor consecutive deviation count reset. SensorId={SensorId}, Deviation={Deviation}, DeviationThreshold={DeviationThreshold}",
                sensorAverage.SensorId,
                deviation,
                _deviationThreshold);
        }
    }

    private static List<double> GetValuesForConsensus(IEnumerable<double> sensorValues)
    {
        var sortedValues = sensorValues.Order().ToList();

        return sortedValues.Count >= 5
            ? sortedValues.GetRange(1, sortedValues.Count - 2)
            : sortedValues;
    }

    private sealed record SensorAverage(string SensorId, double Average);
}
