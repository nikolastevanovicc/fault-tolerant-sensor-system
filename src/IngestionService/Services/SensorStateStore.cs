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
            (_, current) =>
            {
                var blockedUntil = current.BlockedUntil > receivedAt
                    ? current.BlockedUntil
                    : null;

                return new SensorStateSnapshot(
                    reading.SensorId,
                    receivedAt,
                    reading.Quality,
                    blockedUntil,
                    reading.MessageId);
            });

        return ToDto(snapshot, receivedAt);
    }

    public SensorStateDto? Get(string sensorId, DateTimeOffset now)
    {
        return _states.TryGetValue(sensorId, out var snapshot)
            ? ToDto(snapshot, now)
            : null;
    }

    public IReadOnlyCollection<SensorStateDto> GetAll(DateTimeOffset now)
    {
        return _states.Values
            .OrderBy(state => state.SensorId, StringComparer.OrdinalIgnoreCase)
            .Select(state => ToDto(state, now))
            .ToArray();
    }

    public SensorStateDto? Block(string sensorId, DateTimeOffset now, TimeSpan duration)
    {
        if (!_states.ContainsKey(sensorId))
        {
            return null;
        }

        var blockedUntil = now.Add(duration);
        var snapshot = _states.AddOrUpdate(
            sensorId,
            _ => new SensorStateSnapshot(
                sensorId,
                now,
                Shared.Enums.DataQuality.Uncertain,
                blockedUntil,
                0),
            (_, current) => current with { BlockedUntil = blockedUntil });

        return ToDto(snapshot, now);
    }

    private static SensorStateDto ToDto(SensorStateSnapshot state, DateTimeOffset now)
    {
        return new SensorStateDto
        {
            SensorId = state.SensorId,
            LastMessageTime = state.LastMessageTime,
            IsActive = now - state.LastMessageTime <= InactiveAfter,
            Quality = state.Quality,
            BlockedUntil = state.BlockedUntil > now ? state.BlockedUntil : null,
            LastMessageId = state.LastMessageId
        };
    }
}
