using Shared.Dtos;

namespace IngestionService.Services;

public interface ISensorStateStore
{
    SensorStateDto UpdateFromReading(SensorReadingDto reading, DateTimeOffset receivedAt);

    IReadOnlyCollection<SensorStateDto> GetAll(DateTimeOffset now);
}
