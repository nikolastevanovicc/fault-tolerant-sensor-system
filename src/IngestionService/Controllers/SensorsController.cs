using IngestionService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Persistence;
using Shared.Dtos;

namespace IngestionService.Controllers;

[ApiController]
[Route("api/sensors")]
public sealed class SensorsController : ControllerBase
{
    private static readonly TimeSpan ManualBlockDuration = TimeSpan.FromSeconds(30);
    private readonly ISensorStateStore _sensorStateStore;
    private readonly IReadingPersistence _readingPersistence;
    private readonly AppDbContext _dbContext;

    public SensorsController(
        ISensorStateStore sensorStateStore,
        IReadingPersistence readingPersistence,
        AppDbContext dbContext)
    {
        _sensorStateStore = sensorStateStore;
        _readingPersistence = readingPersistence;
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<SensorStateDto>>> GetSensors(
        CancellationToken cancellationToken)
    {
        var cutoff = DateTimeOffset.UtcNow.AddSeconds(-10);
        var sensors = await _dbContext.SensorStates
            .AsNoTracking()
            .OrderBy(sensor => sensor.SensorId)
            .Select(sensor => new SensorStateDto
            {
                SensorId = sensor.SensorId,
                LastMessageTime = sensor.LastMessageTime,
                IsActive = sensor.LastMessageTime >= cutoff,
                Quality = sensor.Quality,
                BlockedUntil = sensor.BlockedUntil,
                LastMessageId = sensor.LastMessageId
            })
            .ToListAsync(cancellationToken);

        return Ok(sensors);
    }

    [HttpGet("active")]
    public async Task<ActionResult<IReadOnlyCollection<SensorStateDto>>> GetActiveSensors(
        CancellationToken cancellationToken)
    {
        var cutoff = DateTimeOffset.UtcNow.AddSeconds(-10);
        var sensors = await _dbContext.SensorStates
            .AsNoTracking()
            .Where(sensor => sensor.LastMessageTime >= cutoff)
            .OrderBy(sensor => sensor.SensorId)
            .Select(sensor => new SensorStateDto
            {
                SensorId = sensor.SensorId,
                LastMessageTime = sensor.LastMessageTime,
                IsActive = true,
                Quality = sensor.Quality,
                BlockedUntil = sensor.BlockedUntil,
                LastMessageId = sensor.LastMessageId
            })
            .ToListAsync(cancellationToken);

        return Ok(sensors);
    }

    [HttpPost("{sensorId}/block")]
    public async Task<ActionResult<SensorStateDto>> BlockSensor(
        string sensorId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sensorId))
        {
            return BadRequest("SensorId is required.");
        }

        var now = DateTimeOffset.UtcNow;
        var state = _sensorStateStore.Block(sensorId, now, ManualBlockDuration);
        if (state is null)
        {
            return NotFound($"Sensor '{sensorId}' is not known.");
        }

        await _readingPersistence.SaveSensorStateAsync(state, cancellationToken);

        return Ok(state);
    }
}
