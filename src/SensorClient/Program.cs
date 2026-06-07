using System.Net.Http.Json;
using SensorClient;
using SensorClient.Simulation;
using Shared.Dtos;
using Shared.Enums;

var sensors = new[]
{
    new SimulatedSensor("sensor-001", 35, 72, DataQuality.Good, new AlarmThresholds(50, 60, 68)),
    new SimulatedSensor("sensor-002", 36, 74, DataQuality.Good, new AlarmThresholds(52, 62, 70)),
    new SimulatedSensor("sensor-003", 34, 70, DataQuality.Good, new AlarmThresholds(49, 59, 66)),
    new SimulatedSensor("sensor-004", 37, 76, DataQuality.Good, new AlarmThresholds(53, 64, 72)),
    new SimulatedSensor("sensor-005", 33, 69, DataQuality.Good, new AlarmThresholds(48, 58, 65))
};

SensorClientOptions options;
try
{
    options = SensorClientOptions.Parse(args, sensors[^1].SensorId);
}
catch (Exception ex) when (ex is ArgumentException or UriFormatException)
{
    Console.Error.WriteLine($"Invalid SensorClient arguments: {ex.Message}");
    Console.Error.WriteLine(
        "Usage: SensorClient [baseAddress] [--malicious-demo] [--malicious-sensor <sensorId>] [--malicious-offset <double>]");
    return;
}

if (options.MaliciousDemoEnabled
    && !sensors.Any(sensor => string.Equals(sensor.SensorId, options.MaliciousSensorId, StringComparison.OrdinalIgnoreCase)))
{
    Console.Error.WriteLine($"Malicious sensor '{options.MaliciousSensorId}' is not configured.");
    return;
}

using var client = new HttpClient
{
    BaseAddress = options.BaseAddress
};

using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellation.Cancel();
};

var generator = new SensorReadingGenerator();
var consoleWriter = new ConsoleReadingWriter();
var tasks = sensors.Select(sensor => RunSensorAsync(sensor, client, generator, consoleWriter, options, cancellation.Token));

Console.WriteLine($"Sending readings to {client.BaseAddress}api/readings");
if (options.MaliciousDemoEnabled)
{
    Console.WriteLine("Malicious demo mode is active.");
    Console.WriteLine($"Malicious SensorId: {options.MaliciousSensorId}");
    Console.WriteLine($"Malicious offset: {options.MaliciousOffset:0.0###} C");
}

Console.WriteLine("Press Ctrl+C to stop.");

await Task.WhenAll(tasks);
Console.WriteLine("Sensor simulation stopped.");

static async Task RunSensorAsync(
    SimulatedSensor sensor,
    HttpClient client,
    SensorReadingGenerator generator,
    ConsoleReadingWriter consoleWriter,
    SensorClientOptions options,
    CancellationToken cancellationToken)
{
    long messageId = 0;

    while (!cancellationToken.IsCancellationRequested)
    {
        messageId++;
        var reading = generator.CreateReading(sensor, messageId);

        if (options.MaliciousDemoEnabled
            && string.Equals(sensor.SensorId, options.MaliciousSensorId, StringComparison.OrdinalIgnoreCase))
        {
            var originalTemperature = reading.Temperature;
            var maliciousTemperature = Math.Round(originalTemperature + options.MaliciousOffset, 2);
            reading = reading with
            {
                Temperature = maliciousTemperature,
                AlarmPriority = sensor.AlarmThresholds.GetPriority(maliciousTemperature)
            };

            Console.WriteLine(
                $"[MALICIOUS] {sensor.SensorId}: original {originalTemperature} C, malicious {maliciousTemperature} C");
        }

        await SendReadingAsync(client, reading, consoleWriter, cancellationToken);

        var delaySeconds = Random.Shared.Next(1, 11);
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            break;
        }
    }
}

static async Task SendReadingAsync(
    HttpClient client,
    SensorReadingDto reading,
    ConsoleReadingWriter consoleWriter,
    CancellationToken cancellationToken)
{
    try
    {
        using var response = await client.PostAsJsonAsync("api/readings", reading, cancellationToken);
        var result = await response.Content.ReadFromJsonAsync<IngestReadingResponseDto>(cancellationToken);

        consoleWriter.WriteSentReading(reading, (int)response.StatusCode);

        if (result is { Accepted: false })
        {
            consoleWriter.WriteRejectedReading(reading, result.Message);
        }
    }
    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
    {
    }
    catch (HttpRequestException ex)
    {
        consoleWriter.WriteRequestFailure(reading, ex.Message);
    }
}
