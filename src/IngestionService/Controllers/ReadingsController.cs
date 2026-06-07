using Microsoft.AspNetCore.Mvc;
using IngestionService.Services;
using Microsoft.EntityFrameworkCore;
using Persistence;
using Shared.Dtos;

namespace IngestionService.Controllers;

[ApiController]
[Route("api/readings")]
public sealed class ReadingsController : ControllerBase
{
    private const int DefaultHistoryMaxResults = 500;
    private const int MaximumHistoryMaxResults = 5000;
    private readonly ILogger<ReadingsController> _logger;
    private readonly ISensorStateStore _sensorStateStore;
    private readonly IReadingPersistence _readingPersistence;
    private readonly AlarmConsoleWriter _alarmConsoleWriter;
    private readonly AppDbContext _dbContext;

    public ReadingsController(
        ILogger<ReadingsController> logger,
        ISensorStateStore sensorStateStore,
        IReadingPersistence readingPersistence,
        AlarmConsoleWriter alarmConsoleWriter,
        AppDbContext dbContext)
    {
        _logger = logger;
        _sensorStateStore = sensorStateStore;
        _readingPersistence = readingPersistence;
        _alarmConsoleWriter = alarmConsoleWriter;
        _dbContext = dbContext;
    }

    [HttpGet("history")]
    public async Task<ActionResult<IReadOnlyCollection<SensorReadingHistoryDto>>> GetHistory(
        [FromQuery] string? sensorId,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] bool includeConsensus = false,
        [FromQuery] int maxResults = DefaultHistoryMaxResults,
        CancellationToken cancellationToken = default)
    {
        var fromUtc = from?.ToUniversalTime();
        var toUtc = to?.ToUniversalTime();

        if (maxResults < 1)
        {
            return BadRequest("maxResults must be at least 1.");
        }

        if (fromUtc > toUtc)
        {
            return BadRequest("'from' must be earlier than or equal to 'to'.");
        }

        maxResults = Math.Min(maxResults, MaximumHistoryMaxResults);

        var query = _dbContext.SensorReadings.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(sensorId))
        {
            query = query.Where(reading => reading.SensorId == sensorId);
        }

        if (fromUtc.HasValue)
        {
            query = query.Where(reading => reading.Timestamp >= fromUtc.Value);
        }

        if (toUtc.HasValue)
        {
            query = query.Where(reading => reading.Timestamp <= toUtc.Value);
        }

        if (!includeConsensus)
        {
            query = query.Where(reading => !reading.IsConsensusValue);
        }

        var readings = await query
            .OrderByDescending(reading => reading.Timestamp)
            .Take(maxResults)
            .Select(reading => new SensorReadingHistoryDto
            {
                SensorId = reading.SensorId,
                Temperature = reading.Temperature,
                Timestamp = reading.Timestamp,
                ReceivedAt = reading.ReceivedAt,
                Quality = reading.Quality,
                AlarmPriority = reading.AlarmPriority,
                MessageId = reading.MessageId,
                IsConsensusValue = reading.IsConsensusValue
            })
            .ToListAsync(cancellationToken);

        return Ok(readings);
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
