using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FaydamPDKS.Core.Models;

[Table("shifts")]
public sealed class Shift
{
    [Key, Column("id")]
    public Guid Id { get; set; }

    [Required, StringLength(100), Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("starts_at")]
    public TimeOnly StartsAt { get; set; }

    [Column("ends_at")]
    public TimeOnly EndsAt { get; set; }

    [Column("late_tolerance_minutes")]
    public int LateToleranceMinutes { get; set; }

    [Column("early_leave_tolerance_minutes")]
    public int EarlyLeaveToleranceMinutes { get; set; }

    [Column("break_minutes")]
    public int BreakMinutes { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;
}
