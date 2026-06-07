using Shared.Enums;

namespace IngestionService.Services;

public sealed record SensorStateSnapshot(
    string SensorId,
    DateTimeOffset LastMessageTime,
    DataQuality Quality,
    DateTimeOffset? BlockedUntil,
    long LastMessageId);
