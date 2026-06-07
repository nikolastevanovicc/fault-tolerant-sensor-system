using Shared.Enums;

namespace Shared.Dtos;

public sealed record SensorReadingHistoryDto
{
    public required string SensorId { get; init; }

    public required double Temperature { get; init; }

    public required DateTimeOffset Timestamp { get; init; }

    public required DateTimeOffset ReceivedAt { get; init; }

    public required DataQuality Quality { get; init; }

    public required AlarmPriority AlarmPriority { get; init; }

    public required long MessageId { get; init; }

    public required bool IsConsensusValue { get; init; }
}
