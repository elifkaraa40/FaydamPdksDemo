using FaydamPDKS.Core.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FaydamPDKS.Core.Models;

[Table("leave_requests")]
public sealed class LeaveRequest
{
    [Key, Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("user_id")]
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    [Column("leave_type")]
    public LeaveType LeaveType { get; set; }

    [Column("start_date")]
    public DateOnly StartDate { get; set; }

    [Column("end_date")]
    public DateOnly EndDate { get; set; }

    [Column("day_portion")]
    public LeaveDayPortion DayPortion { get; set; } = LeaveDayPortion.FullDay;

    [Column("reason"), StringLength(500)]
    public string? Reason { get; set; }

    [Column("status")]
    public LeaveRequestStatus Status { get; set; } = LeaveRequestStatus.Pending;

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [Column("reviewed_at")]
    public DateTimeOffset? ReviewedAt { get; set; }

    [Column("reviewed_by_user_id")]
    public Guid? ReviewedByUserId { get; set; }

    [Column("review_note"), StringLength(500)]
    public string? ReviewNote { get; set; }
}
