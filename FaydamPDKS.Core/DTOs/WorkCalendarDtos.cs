using FaydamPDKS.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace FaydamPDKS.Core.DTOs;

public sealed record WorkCalendarDayListItemDto(
    Guid Id,
    Guid? WorkplaceId,
    string ScopeName,
    DateOnly Date,
    string Name,
    CalendarDayType DayType,
    bool IsHalfDay,
    bool IsSystemGenerated,
    string? Source,
    DateTimeOffset? SourceUpdatedAt);

public sealed record WorkplaceOptionDto(Guid Id, string Code, string Name);
public sealed record WorkCalendarPageDto(IReadOnlyList<WorkCalendarDayListItemDto> Days, IReadOnlyList<WorkplaceOptionDto> Workplaces)
{
    public int SelectedYear { get; init; } = DateTime.Today.Year;
    public int SelectedMonth { get; init; } = DateTime.Today.Month;
    public string ViewMode { get; init; } = "month";
    public Guid? SelectedWorkplaceId { get; init; }
    public string? HolidaySyncWarning { get; init; }
    public DateTimeOffset? LastHolidaySyncAt { get; init; }
}

public sealed class CreateWorkCalendarDayDto
{
    public Guid? WorkplaceId { get; set; }
    [Required] public DateOnly Date { get; set; }
    [Required, StringLength(150)] public string Name { get; set; } = string.Empty;
    [Required] public CalendarDayType DayType { get; set; }
    public bool IsHalfDay { get; set; }
}
