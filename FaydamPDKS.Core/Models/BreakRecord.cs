using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FaydamPDKS.Core.Models;

[Table("break_records")]
public sealed class BreakRecord
{
    [Key, Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("user_id")]
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    [Column("started_at")]
    public DateTimeOffset StartedAt { get; set; }

    [Column("ended_at")]
    public DateTimeOffset? EndedAt { get; set; }

    [Column("start_device_event_id"), StringLength(100)]
    public string StartDeviceEventId { get; set; } = string.Empty;

    [Column("end_device_event_id"), StringLength(100)]
    public string? EndDeviceEventId { get; set; }

    [Column("auto_closed")]
    public bool AutoClosed { get; set; }
}
