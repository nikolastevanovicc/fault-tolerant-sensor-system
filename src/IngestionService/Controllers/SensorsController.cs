using IngestionService.Services;
using Microsoft.AspNetCore.Mvc;
using Shared.Dtos;

namespace IngestionService.Controllers;

[ApiController]
[Route("api/sensors")]
public sealed class SensorsController : ControllerBase
{
    private static readonly TimeSpan ManualBlockDuration = TimeSpan.FromSeconds(30);
    private readonly ISensorStateStore _sensorStateStore;
    private readonly IReadingPersistence _readingPersistence;

    public SensorsController(
        ISensorStateStore sensorStateStore,
        IReadingPersistence readingPersistence)
    {
        _sensorStateStore = sensorStateStore;
        _readingPersistence = readingPersistence;
    }

    [HttpGet]
    public ActionResult<IReadOnlyCollection<SensorStateDto>> GetSensors()
    {
        var sensors = _sensorStateStore.GetAll(DateTimeOffset.UtcNow);
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
