using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FaydamPDKS.Core.Enums;

namespace FaydamPDKS.Core.Models;

[Table("field_work_requests")]
public sealed class FieldWorkRequest
{
    [Key, Column("id")] public Guid Id { get; set; } = Guid.NewGuid();
    [Column("user_id")] public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    [Column("location_type")] public WorkLocationType LocationType { get; set; } = WorkLocationType.Field;
    [Column("start_date")] public DateOnly StartDate { get; set; }
    [Column("end_date")] public DateOnly EndDate { get; set; }
    [Column("recurrence_type")] public WorkLocationRecurrenceType RecurrenceType { get; set; }
    [StringLength(150), Column("project_name")] public string? ProjectName { get; set; }
    [StringLength(150), Column("customer_name")] public string? CustomerName { get; set; }
    [StringLength(300), Column("field_address")] public string? FieldAddress { get; set; }
    [Required, StringLength(500), Column("reason")] public string Reason { get; set; } = string.Empty;
    [Column("status")] public WorkLocationRequestStatus Status { get; set; } = WorkLocationRequestStatus.Pending;
    [Column("created_at")] public DateTimeOffset CreatedAt { get; set; }
    [Column("reviewed_at")] public DateTimeOffset? ReviewedAt { get; set; }
    [Column("reviewed_by_user_id")] public Guid? ReviewedByUserId { get; set; }
    [StringLength(500), Column("review_note")] public string? ReviewNote { get; set; }
    public ICollection<FieldWorkRequestDay> Days { get; set; } = [];
}

[Table("field_work_request_days")]
public sealed class FieldWorkRequestDay
{
    [Key, Column("id")] public Guid Id { get; set; } = Guid.NewGuid();
    [Column("request_id")] public Guid RequestId { get; set; }
    public FieldWorkRequest Request { get; set; } = null!;
    [Column("day_of_week")] public DayOfWeek DayOfWeek { get; set; }
}
