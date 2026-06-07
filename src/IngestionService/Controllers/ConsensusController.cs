using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Persistence;
using Shared.Dtos;

namespace IngestionService.Controllers;

[ApiController]
[Route("api/consensus")]
public sealed class ConsensusController : ControllerBase
{
    private const int DefaultHistoryMaxResults = 200;
    private const int MaximumHistoryMaxResults = 2000;
    private readonly AppDbContext _dbContext;

    public ConsensusController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("latest")]
    public async Task<ActionResult<ConsensusReadingDto>> GetLatest(
        CancellationToken cancellationToken)
    {
        var consensus = await _dbContext.ConsensusReadings
            .AsNoTracking()
            .OrderByDescending(reading => reading.PeriodEnd)
            .ThenByDescending(reading => reading.CreatedAt)
            .Select(reading => new ConsensusReadingDto
            {
                PeriodStart = reading.PeriodStart,
                PeriodEnd = reading.PeriodEnd,
                Value = reading.Value,
                UsedSensorCount = reading.UsedSensorCount,
                RawReadingCount = reading.RawReadingCount,
                Algorithm = reading.Algorithm,
                CreatedAt = reading.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        return consensus is null ? NotFound() : Ok(consensus);
    }

    [HttpGet("history")]
    public async Task<ActionResult<IReadOnlyCollection<ConsensusReadingDto>>> GetHistory(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int maxResults = DefaultHistoryMaxResults,
        CancellationToken cancellationToken = default)
    {
        var fromUtc = from.HasValue ? NormalizeUtc(from.Value) : (DateTime?)null;
        var toUtc = to.HasValue ? NormalizeUtc(to.Value) : (DateTime?)null;

        if (maxResults < 1)
        {
            return BadRequest("maxResults must be at least 1.");
        }

        if (fromUtc > toUtc)
        {
            return BadRequest("'from' must be earlier than or equal to 'to'.");
        }

        maxResults = Math.Min(maxResults, MaximumHistoryMaxResults);

        var query = _dbContext.ConsensusReadings.AsNoTracking();

        if (fromUtc.HasValue)
        {
            query = query.Where(reading => reading.PeriodStart >= fromUtc.Value);
        }

        if (toUtc.HasValue)
        {
            query = query.Where(reading => reading.PeriodEnd <= toUtc.Value);
        }

        var consensusReadings = await query
            .OrderByDescending(reading => reading.PeriodEnd)
            .Take(maxResults)
            .Select(reading => new ConsensusReadingDto
            {
                PeriodStart = reading.PeriodStart,
                PeriodEnd = reading.PeriodEnd,
                Value = reading.Value,
                UsedSensorCount = reading.UsedSensorCount,
                RawReadingCount = reading.RawReadingCount,
                Algorithm = reading.Algorithm,
                CreatedAt = reading.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(consensusReadings);
    }

    private static DateTime NormalizeUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }
}
