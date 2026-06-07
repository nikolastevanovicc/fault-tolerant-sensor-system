using Shared.Enums;

namespace IngestionService.Data.Entities;

public sealed class SensorStateEntity
{
    public required string SensorId { get; set; }

    public DateTimeOffset LastMessageTime { get; set; }

    public bool IsActive { get; set; }

    public DataQuality Quality { get; set; }

    public DateTimeOffset? BlockedUntil { get; set; }

    public long LastMessageId { get; set; }
}
