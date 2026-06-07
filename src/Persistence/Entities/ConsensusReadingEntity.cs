namespace Persistence.Entities;

public sealed class ConsensusReadingEntity
{
    public int Id { get; set; }

    public DateTime PeriodStart { get; set; }

    public DateTime PeriodEnd { get; set; }

    public double Value { get; set; }

    public int UsedSensorCount { get; set; }

    public int RawReadingCount { get; set; }

    public string Algorithm { get; set; } = "TrimmedMeanBft";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
