namespace Persistence.Entities;

public sealed class SensorAnomalyStateEntity
{
    public int Id { get; set; }

    public required string SensorId { get; set; }

    public int ConsecutiveDeviationCount { get; set; }

    public double? LastDeviation { get; set; }

    public DateTime LastUpdatedAt { get; set; }
}
