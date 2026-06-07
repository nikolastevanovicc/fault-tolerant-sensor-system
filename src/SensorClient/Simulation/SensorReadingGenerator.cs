using Shared.Dtos;
using Shared.Enums;

namespace SensorClient.Simulation;

public sealed class SensorReadingGenerator
{
    public SensorReadingDto CreateReading(SimulatedSensor sensor, long messageId)
    {
        var temperature = GenerateTemperature(sensor);

        return new SensorReadingDto
        {
            SensorId = sensor.SensorId,
            Temperature = temperature,
            Timestamp = DateTimeOffset.UtcNow,
            Quality = sensor.Quality,
            AlarmPriority = sensor.AlarmThresholds.GetPriority(temperature),
            MessageId = messageId
        };
    }

    private static double GenerateTemperature(SimulatedSensor sensor)
    {
        var value = Random.Shared.NextDouble() * (sensor.MaxTemperature - sensor.MinTemperature) + sensor.MinTemperature;
        return Math.Round(value, 2);
    }
}
