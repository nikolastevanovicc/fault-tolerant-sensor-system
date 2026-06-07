using Microsoft.EntityFrameworkCore;
using Persistence;
using Persistence.Entities;
using Shared.Dtos;
using Shared.Enums;

namespace IngestionService.Services;

public sealed class PostgresReadingPersistence : IReadingPersistence
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<PostgresReadingPersistence> _logger;

    public PostgresReadingPersistence(
        AppDbContext dbContext,
        ILogger<PostgresReadingPersistence> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task SaveAcceptedReadingAsync(
        SensorReadingDto reading,
        SensorStateDto sensorState,
        DateTimeOffset receivedAt,
        CancellationToken cancellationToken)
    {
        var sensorStateEntity = await GetOrCreateSensorStateAsync(sensorState, cancellationToken);
        var preserveBadQuality = sensorStateEntity.Quality == DataQuality.Bad;
        var effectiveReadingQuality = preserveBadQuality
            ? DataQuality.Bad
            : reading.Quality;

        if (preserveBadQuality)
        {
            _logger.LogWarning(
                "Sensor BAD state preserved; incoming quality was ignored. SensorId={SensorId}, IncomingQuality={IncomingQuality}",
                reading.SensorId,
                reading.Quality);

            _logger.LogWarning(
                "Incoming reading stored as BAD because sensor is already marked BAD. SensorId={SensorId}, MessageId={MessageId}, IncomingQuality={IncomingQuality}",
                reading.SensorId,
                reading.MessageId,
                reading.Quality);
        }

        _dbContext.SensorReadings.Add(new SensorReadingEntity
        {
            SensorId = reading.SensorId,
            Temperature = reading.Temperature,
            Timestamp = reading.Timestamp.ToUniversalTime(),
            ReceivedAt = receivedAt.ToUniversalTime(),
            Quality = effectiveReadingQuality,
            AlarmPriority = reading.AlarmPriority,
            MessageId = reading.MessageId,
            IsConsensusValue = false
        });

        UpdateSensorState(sensorStateEntity, sensorState, preserveBadQuality);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveSensorStateAsync(
        SensorStateDto sensorState,
        CancellationToken cancellationToken)
    {
        var entity = await GetOrCreateSensorStateAsync(sensorState, cancellationToken);
        UpdateSensorState(entity, sensorState, entity.Quality == DataQuality.Bad);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<SensorStateEntity> GetOrCreateSensorStateAsync(
        SensorStateDto sensorState,
        CancellationToken cancellationToken)
    {
        var entity = await _dbContext.SensorStates
            .SingleOrDefaultAsync(state => state.SensorId == sensorState.SensorId, cancellationToken);

        if (entity is null)
        {
            entity = new SensorStateEntity
            {
                SensorId = sensorState.SensorId,
                LastMessageTime = sensorState.LastMessageTime.ToUniversalTime(),
                IsActive = sensorState.IsActive,
                Quality = sensorState.Quality,
                BlockedUntil = sensorState.BlockedUntil?.ToUniversalTime(),
                LastMessageId = sensorState.LastMessageId
            };
            _dbContext.SensorStates.Add(entity);
        }

        return entity;
    }

    private static void UpdateSensorState(
        SensorStateEntity entity,
        SensorStateDto sensorState,
        bool preserveBadQuality)
    {
        entity.LastMessageTime = sensorState.LastMessageTime.ToUniversalTime();
        entity.IsActive = sensorState.IsActive;
        entity.Quality = preserveBadQuality
            ? DataQuality.Bad
            : sensorState.Quality;
        entity.BlockedUntil = sensorState.BlockedUntil?.ToUniversalTime();
        entity.LastMessageId = sensorState.LastMessageId;
    }
}
