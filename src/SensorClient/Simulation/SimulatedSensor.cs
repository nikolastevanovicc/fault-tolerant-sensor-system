using Shared.Enums;

namespace SensorClient.Simulation;

public sealed class SimulatedSensor
{
    public SimulatedSensor(
        string sensorId,
        double minTemperature,
        double maxTemperature,
        DataQuality quality,
        AlarmThresholds alarmThresholds)
    {
        SensorId = sensorId;
        MinTemperature = minTemperature;
        MaxTemperature = maxTemperature;
        Quality = quality;
        AlarmThresholds = alarmThresholds;
    }

    public string SensorId { get; }

    public double MinTemperature { get; }

    public double MaxTemperature { get; }

    public DataQuality Quality { get; }

    public AlarmThresholds AlarmThresholds { get; }
}
