using System.Net.Http.Json;
using Shared.Dtos;
using Shared.Enums;

var baseAddress = args.Length > 0
    ? args[0]
    : "http://localhost:5095";

using var client = new HttpClient
{
    BaseAddress = new Uri(baseAddress)
};

var reading = new SensorReadingDto
{
    SensorId = "sensor-001",
    Temperature = 42.5,
    Timestamp = DateTimeOffset.UtcNow,
    Quality = DataQuality.Good,
    AlarmPriority = AlarmPriority.None,
    MessageId = 1
};

Console.WriteLine($"Sending reading to {client.BaseAddress}api/readings");
Console.WriteLine($"SensorId={reading.SensorId}, Temperature={reading.Temperature}, MessageId={reading.MessageId}");

try
{
    using var response = await client.PostAsJsonAsync("api/readings", reading);
    var result = await response.Content.ReadFromJsonAsync<IngestReadingResponseDto>();

    Console.WriteLine($"HTTP {(int)response.StatusCode} {response.ReasonPhrase}");

    if (result is not null)
    {
        Console.WriteLine($"Accepted={result.Accepted}, SensorId={result.SensorId}, MessageId={result.MessageId}, Message={result.Message}");
    }
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"Request failed: {ex.Message}");
    Console.WriteLine("Start IngestionService first, then run SensorClient again.");
}
