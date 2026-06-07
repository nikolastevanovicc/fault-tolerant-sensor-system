using Microsoft.EntityFrameworkCore;
using Persistence;
using Persistence.Entities;
using Shared.Dtos;

namespace IngestionService.Services;

public sealed class PostgresReadingPersistence : IReadingPersistence
{
    private readonly AppDbContext _dbContext;

    public PostgresReadingPersistence(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SaveAcceptedReadingAsync(
        SensorReadingDto reading,
        SensorStateDto sensorState,
        DateTimeOffset receivedAt,
        CancellationToken cancellationToken)
    {
        _dbContext.SensorReadings.Add(new SensorReadingEntity
        {
            SensorId = reading.SensorId,
            Temperature = reading.Temperature,
            Timestamp = reading.Timestamp.ToUniversalTime(),
            ReceivedAt = receivedAt.ToUniversalTime(),
            Quality = reading.Quality,
            AlarmPriority = reading.AlarmPriority,
            MessageId = reading.MessageId,
            IsConsensusValue = false
        });

        await UpsertSensorStateAsync(sensorState, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveSensorStateAsync(
        SensorStateDto sensorState,
        CancellationToken cancellationToken)
    {
        await UpsertSensorStateAsync(sensorState, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task UpsertSensorStateAsync(
        SensorStateDto sensorState,
        CancellationToken cancellationToken)
    {
        var entity = await _dbContext.SensorStates
            .SingleOrDefaultAsync(state => state.SensorId == sensorState.SensorId, cancellationToken);

        if (entity is null)
        {
            _dbContext.SensorStates.Add(new SensorStateEntity
            {
                SensorId = sensorState.SensorId,
                LastMessageTime = sensorState.LastMessageTime.ToUniversalTime(),
                IsActive = sensorState.IsActive,
                Quality = sensorState.Quality,
                BlockedUntil = sensorState.BlockedUntil?.ToUniversalTime(),
                LastMessageId = sensorState.LastMessageId
            });

            return;
        }

        entity.LastMessageTime = sensorState.LastMessageTime.ToUniversalTime();
        entity.IsActive = sensorState.IsActive;
        entity.Quality = sensorState.Quality;
        entity.BlockedUntil = sensorState.BlockedUntil?.ToUniversalTime();
        entity.LastMessageId = sensorState.LastMessageId;
    }
}
