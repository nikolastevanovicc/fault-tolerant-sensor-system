using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using NotificationService.Hubs;
using Shared.Dtos;

namespace NotificationService.Controllers
{
    [ApiController]
    [Route("api/notifications")]
    public sealed class AlarmNotificationsController : ControllerBase
    {
        private readonly IHubContext<AlarmHub> _hubContext;
        private readonly ILogger<AlarmNotificationsController> _logger;

        public AlarmNotificationsController(
            IHubContext<AlarmHub> hubContext,
            ILogger<AlarmNotificationsController> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        [HttpPost("alarm")]
        public async Task<IActionResult> PublishAlarm(
            [FromBody] AlarmNotificationDto alarm,
            CancellationToken cancellationToken)
        {
            _logger.LogWarning(
                "Alarm received. SensorId={SensorId}, Temperature={Temperature}, Priority={Priority}",
                alarm.SensorId,
                alarm.Temperature,
                alarm.AlarmPriority);

            await _hubContext.Clients.All.SendAsync(
                "AlarmReceived",
                alarm,
                cancellationToken);

            return Ok(new
            {
                Message = "Alarm notification sent.",
                alarm.SensorId,
                alarm.MessageId
            });
        }
    }
}
