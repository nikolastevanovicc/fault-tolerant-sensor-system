using Shared.Dtos;

namespace IngestionService.Services;

public interface ISensorStateStore
{
    SensorStateDto UpdateFromReading(SensorReadingDto reading, DateTimeOffset receivedAt);

    SensorStateDto? Get(string sensorId, DateTimeOffset now);

    IReadOnlyCollection<SensorStateDto> GetAll(DateTimeOffset now);

    SensorStateDto? Block(string sensorId, DateTimeOffset now, TimeSpan duration);
}
