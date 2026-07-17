using FaydamPDKS.Core.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FaydamPDKS.Core.Models;

[Table("attendance_correction_requests")]
public sealed class AttendanceCorrectionRequest
{
    [Key, Column("id")] public Guid Id { get; set; }
    [Column("user_id")] public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    [Column("work_date")] public DateOnly WorkDate { get; set; }
    [Column("correction_type")] public AttendanceCorrectionType CorrectionType { get; set; } = AttendanceCorrectionType.TimeCorrection;
    [Column("requested_entry")] public TimeOnly RequestedEntry { get; set; }
    [Column("requested_exit")] public TimeOnly RequestedExit { get; set; }
    [Required, StringLength(500), Column("reason")] public string Reason { get; set; } = string.Empty;
    [Column("status")] public AttendanceCorrectionStatus Status { get; set; } = AttendanceCorrectionStatus.Pending;
    [Column("created_at")] public DateTimeOffset CreatedAt { get; set; }
    [Column("reviewed_at")] public DateTimeOffset? ReviewedAt { get; set; }
    [Column("reviewed_by_user_id")] public Guid? ReviewedByUserId { get; set; }
    [StringLength(500), Column("review_note")] public string? ReviewNote { get; set; }
    [StringLength(150), Column("project_name")] public string? ProjectName { get; set; }
    [StringLength(150), Column("customer_name")] public string? CustomerName { get; set; }
    [StringLength(300), Column("field_address")] public string? FieldAddress { get; set; }
}
