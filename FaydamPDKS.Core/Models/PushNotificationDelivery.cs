using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FaydamPDKS.Core.Models;

[Table("push_notification_deliveries")]
public sealed class PushNotificationDelivery
{
    [Key, Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("notification_id")]
    public Guid NotificationId { get; set; }
    public Notification Notification { get; set; } = null!;

    [Column("device_session_id")]
    public Guid DeviceSessionId { get; set; }
    public DeviceSession DeviceSession { get; set; } = null!;

    [Column("attempt_count")]
    public int AttemptCount { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [Column("last_attempt_at")]
    public DateTimeOffset? LastAttemptAt { get; set; }

    [Column("next_attempt_at")]
    public DateTimeOffset? NextAttemptAt { get; set; }

    [Column("sent_at")]
    public DateTimeOffset? SentAt { get; set; }

    [MaxLength(100), Column("error_code")]
    public string? ErrorCode { get; set; }
}
