using Microsoft.AspNetCore.Mvc;
using Shared.Dtos;

namespace IngestionService.Controllers;

[ApiController]
[Route("api/readings")]
public sealed class ReadingsController : ControllerBase
{
    private readonly ILogger<ReadingsController> _logger;

    public ReadingsController(ILogger<ReadingsController> logger)
    {
        _logger = logger;
    }

    [HttpPost]
    public ActionResult<IngestReadingResponseDto> PostReading([FromBody] SensorReadingDto reading)
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

        _logger.LogInformation(
            "Reading accepted. SensorId={SensorId}, MessageId={MessageId}, Temperature={Temperature}, Quality={Quality}, AlarmPriority={AlarmPriority}, Timestamp={Timestamp}",
            reading.SensorId,
            reading.MessageId,
            reading.Temperature,
            reading.Quality,
            reading.AlarmPriority,
            reading.Timestamp);

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
