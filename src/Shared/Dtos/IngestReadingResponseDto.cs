namespace Shared.Dtos;

public sealed record IngestReadingResponseDto
{
    public required string SensorId { get; init; }

    public required long MessageId { get; init; }

    public required bool Accepted { get; init; }

    public required string Message { get; init; }
}
