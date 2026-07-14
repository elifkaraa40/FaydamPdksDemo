using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FaydamPDKS.Core.Models;

[Table("attendance_terminals")]
public sealed class AttendanceTerminal
{
    [Key, Column("id")] public Guid Id { get; set; }
    [Column("workplace_id")] public Guid WorkplaceId { get; set; }
    public Workplace Workplace { get; set; } = null!;
    [Required, StringLength(100), Column("name")] public string Name { get; set; } = string.Empty;
    [Required, StringLength(100), Column("serial_number")] public string SerialNumber { get; set; } = string.Empty;
    [Required, StringLength(64), Column("api_key_hash")] public string ApiKeyHash { get; set; } = string.Empty;
    [Column("last_seen_at")] public DateTimeOffset? LastSeenAt { get; set; }
    [StringLength(80), Column("firmware_version")] public string? FirmwareVersion { get; set; }
    [Column("pending_event_count")] public int PendingEventCount { get; set; }
    [StringLength(500), Column("last_error")] public string? LastError { get; set; }
    [Column("is_active")] public bool IsActive { get; set; } = true;
    [Column("created_at")] public DateTimeOffset CreatedAt { get; set; }
}
