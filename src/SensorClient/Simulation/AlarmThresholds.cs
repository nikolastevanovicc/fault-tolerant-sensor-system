using Shared.Enums;

namespace SensorClient.Simulation;

public sealed class AlarmThresholds
{
    public AlarmThresholds(
        double lowPriorityLimit,
        double mediumPriorityLimit,
        double highPriorityLimit)
    {
        LowPriorityLimit = lowPriorityLimit;
        MediumPriorityLimit = mediumPriorityLimit;
        HighPriorityLimit = highPriorityLimit;
    }

    public double LowPriorityLimit { get; }

    public double MediumPriorityLimit { get; }

    public double HighPriorityLimit { get; }

    public AlarmPriority GetPriority(double temperature)
    {
        if (temperature >= HighPriorityLimit)
        {
            return AlarmPriority.High;
        }

        if (temperature >= MediumPriorityLimit)
        {
            return AlarmPriority.Medium;
        }

        if (temperature >= LowPriorityLimit)
        {
            return AlarmPriority.Low;
        }

        return AlarmPriority.None;
    }
}
