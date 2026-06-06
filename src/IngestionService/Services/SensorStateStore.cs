using System.Collections.Concurrent;
using Shared.Dtos;

namespace IngestionService.Services;

public sealed class SensorStateStore : ISensorStateStore
{
    private static readonly TimeSpan InactiveAfter = TimeSpan.FromSeconds(10);
    private readonly ConcurrentDictionary<string, SensorStateSnapshot> _states = new(StringComparer.OrdinalIgnoreCase);

    public SensorStateDto UpdateFromReading(SensorReadingDto reading, DateTimeOffset receivedAt)
    {
        var snapshot = _states.AddOrUpdate(
            reading.SensorId,
            _ => new SensorStateSnapshot(
                reading.SensorId,
                receivedAt,
                reading.Quality,
                null,
                reading.MessageId),
            (_, _) => new SensorStateSnapshot(
                reading.SensorId,
                receivedAt,
                reading.Quality,
                null,
                reading.MessageId));

        return ToDto(snapshot, receivedAt);
    }

    public IReadOnlyCollection<SensorStateDto> GetAll(DateTimeOffset now)
    {
        return _states.Values
            .OrderBy(state => state.SensorId, StringComparer.OrdinalIgnoreCase)
            .Select(state => ToDto(state, now))
            .ToArray();
    }

    private static SensorStateDto ToDto(SensorStateSnapshot state, DateTimeOffset now)
    {
        return new SensorStateDto
        {
            SensorId = state.SensorId,
            LastMessageTime = state.LastMessageTime,
            IsActive = now - state.LastMessageTime <= InactiveAfter,
            Quality = state.Quality,
            BlockedUntil = state.BlockedUntil,
            LastMessageId = state.LastMessageId
        };
    }
}
