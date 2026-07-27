using FaydamPDKS.Core.DTOs;
using FaydamPDKS.Core.Enums;
using FaydamPDKS.Core.Interfaces;
using FaydamPDKS.Core.Models;

namespace FaydamPDKS.Web;

public sealed class WebWorkCalendarAdminService(
    IWorkCalendarRepository calendar,
    IOrganizationRepository organizations,
    IUnitOfWork unitOfWork,
    IPublicHolidaySyncService? holidaySync = null) : IWorkCalendarAdminService
{
    public async Task<WorkCalendarPageDto> GetPageAsync(
        int? year = null,
        int? month = null,
        string? view = null,
        Guid? workplaceId = null,
        CancellationToken cancellationToken = default)
    {
        var selectedYear = Math.Clamp(year ?? DateTime.Today.Year, 2000, 2200);
        var selectedMonth = Math.Clamp(month ?? DateTime.Today.Month, 1, 12);
        var viewMode = string.Equals(view, "year", StringComparison.OrdinalIgnoreCase) ? "year" : "month";
        PublicHolidaySyncResult? syncResult = holidaySync is null
            ? null
            : await holidaySync.SyncYearAsync(selectedYear, cancellationToken);

        var allDays = await calendar.GetAllAsync(cancellationToken);
        var effectiveDays = allDays
            .Where(x => x.Date.Year == selectedYear)
            .Where(x => workplaceId.HasValue
                ? x.WorkplaceId == null || x.WorkplaceId == workplaceId
                : x.WorkplaceId == null)
            .GroupBy(x => x.Date)
            .Select(x => x.OrderByDescending(day => day.WorkplaceId.HasValue).First())
            .OrderBy(x => x.Date);
        var days = effectiveDays.Select(x => new WorkCalendarDayListItemDto(
            x.Id,
            x.WorkplaceId,
            x.Workplace?.Name ?? "Tüm işyerleri",
            x.Date,
            x.Name,
            x.DayType,
            x.IsHalfDay,
            x.IsSystemGenerated,
            x.Source,
            x.SourceUpdatedAt)).ToArray();
        var workplaces = (await organizations.GetWorkplacesAsync(cancellationToken)).Where(x => x.IsActive)
            .Select(x => new WorkplaceOptionDto(x.Id, x.Code, x.Name)).ToArray();
        return new(days, workplaces)
        {
            SelectedYear = selectedYear,
            SelectedMonth = selectedMonth,
            ViewMode = viewMode,
            SelectedWorkplaceId = workplaceId,
            HolidaySyncWarning = syncResult?.Warning,
            LastHolidaySyncAt = syncResult?.LastSuccessfulAt
        };
    }

    public async Task CreateAsync(CreateWorkCalendarDayDto request, CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(request.DayType)) throw new InvalidOperationException("Geçerli gün tipi seçin.");
        if (request.Date == default || request.Date.Year is < 2000 or > 2200)
            throw new InvalidOperationException("Geçerli bir tarih seçin.");
        if (string.IsNullOrWhiteSpace(request.Name)) throw new InvalidOperationException("Gün adı veya açıklama yazın.");
        if (request.DayType == CalendarDayType.WorkingDayOverride && request.IsHalfDay)
            throw new InvalidOperationException("Yarım gün seçeneği yalnızca tatil kayıtlarında kullanılabilir.");
        if (request.WorkplaceId.HasValue && !await organizations.ActiveWorkplaceExistsAsync(request.WorkplaceId.Value, cancellationToken))
            throw new InvalidOperationException("Aktif işyeri bulunamadı.");

        var existing = await calendar.GetAsync(request.WorkplaceId, request.Date, cancellationToken);
        if (existing is null)
        {
            existing = new WorkCalendarDay
            {
                Id = Guid.NewGuid(),
                WorkplaceId = request.WorkplaceId,
                Date = request.Date
            };
            await calendar.AddAsync(existing, cancellationToken);
        }

        existing.Name = request.Name.Trim();
        existing.DayType = request.DayType;
        existing.IsHalfDay = request.DayType == CalendarDayType.Holiday && request.IsHalfDay;
        existing.IsSystemGenerated = false;
        existing.Source = "Yönetici";
        existing.SourceUpdatedAt = null;
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
