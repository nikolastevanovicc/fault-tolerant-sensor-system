using Shared.Enums;

namespace Shared.Dtos;

public sealed record SensorStateDto
{
    public required string SensorId { get; init; }

    public required DateTimeOffset LastMessageTime { get; init; }

    public required bool IsActive { get; init; }

    public required DataQuality Quality { get; init; }

    public DateTimeOffset? BlockedUntil { get; init; }

    public required long LastMessageId { get; init; }
}
