using FaydamPDKS.Core.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FaydamPDKS.Core.Models;

[Table("work_calendar_days")]
public sealed class WorkCalendarDay
{
    [Key, Column("id")] public Guid Id { get; set; }
    [Column("workplace_id")] public Guid? WorkplaceId { get; set; }
    public Workplace? Workplace { get; set; }
    [Column("date")] public DateOnly Date { get; set; }
    [Required, StringLength(150), Column("name")] public string Name { get; set; } = string.Empty;
    [Column("day_type")] public CalendarDayType DayType { get; set; }
}
