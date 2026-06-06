using Shared.Dtos;
using Shared.Enums;

namespace SensorClient.Simulation;

public sealed class ConsoleReadingWriter
{
    private readonly object _lock = new();

    public void WriteSentReading(SensorReadingDto reading, int statusCode)
    {
        lock (_lock)
        {
            var previousColor = Console.ForegroundColor;
            Console.ForegroundColor = GetColor(reading.AlarmPriority);

            Console.WriteLine(
                $"[{DateTimeOffset.Now:HH:mm:ss}] {reading.SensorId} message {reading.MessageId}: {reading.Temperature} C, alarm {(int)reading.AlarmPriority} -> HTTP {statusCode}");

            Console.ForegroundColor = previousColor;
        }
    }

    public void WriteRejectedReading(SensorReadingDto reading, string message)
    {
        lock (_lock)
        {
            var previousColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"{reading.SensorId} rejected: {message}");
            Console.ForegroundColor = previousColor;
        }
    }

    public void WriteRequestFailure(SensorReadingDto reading, string message)
    {
        lock (_lock)
        {
            Console.WriteLine($"{reading.SensorId} request failed: {message}");
        }
    }

    private static ConsoleColor GetColor(AlarmPriority priority)
    {
        return priority switch
        {
            AlarmPriority.Low => ConsoleColor.Yellow,
            AlarmPriority.Medium => ConsoleColor.DarkYellow,
            AlarmPriority.High => ConsoleColor.Red,
            _ => Console.ForegroundColor
        };
    }
}
