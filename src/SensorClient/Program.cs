using System.Net.Http.Json;
using SensorClient.Simulation;
using Shared.Dtos;
using Shared.Enums;

var baseAddress = args.Length > 0
    ? args[0]
    : "http://localhost:5095";

using var client = new HttpClient
{
    BaseAddress = new Uri(baseAddress)
};

using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellation.Cancel();
};

var sensors = new[]
{
    new SimulatedSensor("sensor-001", 35, 72, DataQuality.Good, new AlarmThresholds(50, 60, 68)),
    new SimulatedSensor("sensor-002", 36, 74, DataQuality.Good, new AlarmThresholds(52, 62, 70)),
    new SimulatedSensor("sensor-003", 34, 70, DataQuality.Good, new AlarmThresholds(49, 59, 66)),
    new SimulatedSensor("sensor-004", 37, 76, DataQuality.Good, new AlarmThresholds(53, 64, 72)),
    new SimulatedSensor("sensor-005", 33, 69, DataQuality.Good, new AlarmThresholds(48, 58, 65))
};

var generator = new SensorReadingGenerator();
var consoleWriter = new ConsoleReadingWriter();
var tasks = sensors.Select(sensor => RunSensorAsync(sensor, client, generator, consoleWriter, cancellation.Token));

Console.WriteLine($"Sending readings to {client.BaseAddress}api/readings");
Console.WriteLine("Press Ctrl+C to stop.");

await Task.WhenAll(tasks);
Console.WriteLine("Sensor simulation stopped.");

static async Task RunSensorAsync(
    SimulatedSensor sensor,
    HttpClient client,
    SensorReadingGenerator generator,
    ConsoleReadingWriter consoleWriter,
    CancellationToken cancellationToken)
{
    long messageId = 0;

    while (!cancellationToken.IsCancellationRequested)
    {
        messageId++;
        var reading = generator.CreateReading(sensor, messageId);

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
