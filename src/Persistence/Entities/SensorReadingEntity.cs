using Shared.Enums;

namespace Persistence.Entities;

public sealed class SensorReadingEntity
{
    public long Id { get; set; }

    public required string SensorId { get; set; }

    public double Temperature { get; set; }

    public DateTimeOffset Timestamp { get; set; }

    public DateTimeOffset ReceivedAt { get; set; }

    public DataQuality Quality { get; set; }

    public AlarmPriority AlarmPriority { get; set; }

    public long MessageId { get; set; }

    public bool IsConsensusValue { get; set; }
}
