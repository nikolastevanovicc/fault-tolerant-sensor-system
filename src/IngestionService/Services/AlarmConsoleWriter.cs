using Shared.Dtos;
using Shared.Enums;

namespace IngestionService.Services;

public sealed class AlarmConsoleWriter
{
    private readonly object _lock = new();

    public void WriteAlarm(SensorReadingDto reading)
    {
        if (reading.AlarmPriority == AlarmPriority.None)
        {
            return;
        }

        lock (_lock)
        {
            var previousColor = Console.ForegroundColor;
            Console.ForegroundColor = GetColor(reading.AlarmPriority);

            Console.WriteLine(
                "[ALARM] SensorId={0}, Temperature={1} C, Priority={2}",
                reading.SensorId,
                reading.Temperature,
                (int)reading.AlarmPriority);

            Console.ForegroundColor = previousColor;
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
