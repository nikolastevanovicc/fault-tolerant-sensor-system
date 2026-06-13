using Shared.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Dtos
{
    public sealed record AlarmNotificationDto
    {
        public required string SensorId { get; init; }

        public required double Temperature { get; init; }

        public required AlarmPriority AlarmPriority { get; init; }

        public required DateTimeOffset Timestamp { get; init; }

        public required DateTimeOffset ReceivedAt { get; init; }

        public required long MessageId { get; init; }
    }
}
