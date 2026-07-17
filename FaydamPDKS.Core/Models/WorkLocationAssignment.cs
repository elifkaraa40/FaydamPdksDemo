using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FaydamPDKS.Core.Enums;

namespace FaydamPDKS.Core.Models;

[Table("work_location_assignments")]
public sealed class WorkLocationAssignment
{
    [Key, Column("id")] public Guid Id { get; set; } = Guid.NewGuid();
    [Column("user_id")] public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    [Column("location_type")] public WorkLocationType LocationType { get; set; }
    [Column("start_date")] public DateOnly StartDate { get; set; }
    [Column("end_date")] public DateOnly? EndDate { get; set; }
    [Column("recurrence_type")] public WorkLocationRecurrenceType RecurrenceType { get; set; }
    [StringLength(500), Column("reason")] public string? Reason { get; set; }
    [StringLength(150), Column("project_name")] public string? ProjectName { get; set; }
    [StringLength(150), Column("customer_name")] public string? CustomerName { get; set; }
    [StringLength(300), Column("field_address")] public string? FieldAddress { get; set; }
    [Column("created_by_user_id")] public Guid CreatedByUserId { get; set; }
    [Column("created_at")] public DateTimeOffset CreatedAt { get; set; }
    [Column("ended_at")] public DateTimeOffset? EndedAt { get; set; }
    [Column("ended_by_user_id")] public Guid? EndedByUserId { get; set; }
    [Column("is_active")] public bool IsActive { get; set; } = true;
    public ICollection<WorkLocationAssignmentDay> Days { get; set; } = [];
}

[Table("work_location_assignment_days")]
public sealed class WorkLocationAssignmentDay
{
    [Key, Column("id")] public Guid Id { get; set; } = Guid.NewGuid();
    [Column("assignment_id")] public Guid AssignmentId { get; set; }
    public WorkLocationAssignment Assignment { get; set; } = null!;
    [Column("day_of_week")] public DayOfWeek DayOfWeek { get; set; }
}
