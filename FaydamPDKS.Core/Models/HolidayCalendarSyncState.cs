using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FaydamPDKS.Core.Models;

[Table("holiday_calendar_sync_states")]
public sealed class HolidayCalendarSyncState
{
    [Key, Column("year")] public int Year { get; set; }
    [Column("last_attempted_at")] public DateTimeOffset LastAttemptedAt { get; set; }
    [Column("last_successful_at")] public DateTimeOffset? LastSuccessfulAt { get; set; }
    [StringLength(500), Column("warning")] public string? Warning { get; set; }
    [StringLength(250), Column("source_url")] public string SourceUrl { get; set; } = string.Empty;
}
