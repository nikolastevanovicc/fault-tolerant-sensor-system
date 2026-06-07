namespace Shared.Dtos;

public sealed record ConsensusReadingDto
{
    public required DateTime PeriodStart { get; init; }

    public required DateTime PeriodEnd { get; init; }

    public required double Value { get; init; }

    public required int UsedSensorCount { get; init; }

    public required int RawReadingCount { get; init; }

    public required string Algorithm { get; init; }

    public required DateTime CreatedAt { get; init; }
}
