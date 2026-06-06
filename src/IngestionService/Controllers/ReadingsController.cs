using Microsoft.AspNetCore.Mvc;
using IngestionService.Services;
using Shared.Dtos;

namespace IngestionService.Controllers;

[ApiController]
[Route("api/readings")]
public sealed class ReadingsController : ControllerBase
{
    private readonly ILogger<ReadingsController> _logger;
    private readonly ISensorStateStore _sensorStateStore;
    private readonly IReadingPersistence _readingPersistence;
    private readonly AlarmConsoleWriter _alarmConsoleWriter;

    public ReadingsController(
        ILogger<ReadingsController> logger,
        ISensorStateStore sensorStateStore,
        IReadingPersistence readingPersistence,
        AlarmConsoleWriter alarmConsoleWriter)
    {
        _logger = logger;
        _sensorStateStore = sensorStateStore;
        _readingPersistence = readingPersistence;
        _alarmConsoleWriter = alarmConsoleWriter;
    }

    [HttpPost]
    public async Task<ActionResult<IngestReadingResponseDto>> PostReading(
        [FromBody] SensorReadingDto reading,
        CancellationToken cancellationToken)
    {
        var validationError = Validate(reading);
        if (validationError is not null)
        {
            return BadRequest(new IngestReadingResponseDto
            {
                SensorId = reading?.SensorId ?? string.Empty,
                MessageId = reading?.MessageId ?? 0,
                Accepted = false,
                Message = validationError
            });
        }

        var receivedAt = DateTimeOffset.UtcNow;
        var existingState = _sensorStateStore.Get(reading.SensorId, receivedAt);
        if (existingState?.BlockedUntil is not null)
        {
            return StatusCode(StatusCodes.Status423Locked, new IngestReadingResponseDto
            {
                SensorId = reading.SensorId,
                MessageId = reading.MessageId,
                Accepted = false,
                Message = $"Sensor is blocked until {existingState.BlockedUntil:O}."
            });
        }

        var sensorState = _sensorStateStore.UpdateFromReading(reading, receivedAt);

        _logger.LogInformation(
            "Reading accepted. SensorId={SensorId}, MessageId={MessageId}, Temperature={Temperature}, Quality={Quality}, AlarmPriority={AlarmPriority}, Timestamp={Timestamp}",
            reading.SensorId,
            reading.MessageId,
            reading.Temperature,
            reading.Quality,
            reading.AlarmPriority,
            reading.Timestamp);

        _logger.LogInformation(
            "Sensor state updated. SensorId={SensorId}, LastMessageTime={LastMessageTime}, IsActive={IsActive}, LastMessageId={LastMessageId}",
            sensorState.SensorId,
            sensorState.LastMessageTime,
            sensorState.IsActive,
            sensorState.LastMessageId);

        _alarmConsoleWriter.WriteAlarm(reading);
        await _readingPersistence.SaveAcceptedReadingAsync(
            reading,
            sensorState,
            receivedAt,
            cancellationToken);

        return Ok(new IngestReadingResponseDto
        {
            SensorId = reading.SensorId,
            MessageId = reading.MessageId,
            Accepted = true,
            Message = "Reading accepted."
        });
    }

    private static string? Validate(SensorReadingDto? reading)
    {
        if (reading is null)
        {
            return "Request body is required.";
        }

        if (string.IsNullOrWhiteSpace(reading.SensorId))
        {
            return "SensorId is required.";
        }

        if (reading.MessageId <= 0)
        {
            return "MessageId must be positive.";
        }

        if (reading.Timestamp == default)
        {
            return "Timestamp is required.";
        }

        if (double.IsNaN(reading.Temperature) || double.IsInfinity(reading.Temperature))
        {
            return "Temperature must be a finite number.";
        }

        return null;
    }
}
