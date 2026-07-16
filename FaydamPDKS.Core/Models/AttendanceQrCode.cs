using FaydamPDKS.Core.Attendance;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FaydamPDKS.Core.Models;

[Table("attendance_qr_codes")]
public sealed class AttendanceQrCode
{
    [Key, Column("id")] public Guid Id { get; set; }
    [Column("workplace_id")] public Guid WorkplaceId { get; set; }
    public Workplace Workplace { get; set; } = null!;
    [Column("zone_id")] public int ZoneId { get; set; }
    public Zone Zone { get; set; } = null!;
    [Required, StringLength(100), Column("name")] public string Name { get; set; } = string.Empty;
    [Column("event_type")] public AttendanceEventType EventType { get; set; }
    [Required, StringLength(64), Column("token_hash")] public string TokenHash { get; set; } = string.Empty;
    [Column("is_legacy")] public bool IsLegacy { get; set; }
    [Column("is_active")] public bool IsActive { get; set; } = true;
    [Column("created_at")] public DateTimeOffset CreatedAt { get; set; }
    [Column("revoked_at")] public DateTimeOffset? RevokedAt { get; set; }
    [Column("replaced_by_id")] public Guid? ReplacedById { get; set; }
}
