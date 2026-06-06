using Shared.Dtos;

namespace IngestionService.Services;

public interface IReadingPersistence
{
    Task SaveAcceptedReadingAsync(
        SensorReadingDto reading,
        SensorStateDto sensorState,
        DateTimeOffset receivedAt,
        CancellationToken cancellationToken);

    Task SaveSensorStateAsync(
        SensorStateDto sensorState,
        CancellationToken cancellationToken);
}
