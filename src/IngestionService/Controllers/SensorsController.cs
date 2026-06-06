using IngestionService.Services;
using Microsoft.AspNetCore.Mvc;
using Shared.Dtos;

namespace IngestionService.Controllers;

[ApiController]
[Route("api/sensors")]
public sealed class SensorsController : ControllerBase
{
    private readonly ISensorStateStore _sensorStateStore;

    public SensorsController(ISensorStateStore sensorStateStore)
    {
        _sensorStateStore = sensorStateStore;
    }

    [HttpGet]
    public ActionResult<IReadOnlyCollection<SensorStateDto>> GetSensors()
    {
        var sensors = _sensorStateStore.GetAll(DateTimeOffset.UtcNow);
        return Ok(sensors);
    }
}
