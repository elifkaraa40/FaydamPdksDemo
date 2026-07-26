using FaydamPDKS.Core.DTOs;
using FaydamPDKS.Core.Enums;
using FaydamPDKS.Core.Interfaces;
using FaydamPDKS.Data;
using FaydamPDKS.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace FaydamPDKS.Web;

public sealed class WebReportingService(AppDbContext db, IWorkCalendarResolver workCalendar)
{
    private static readonly TimeZoneInfo IstanbulTimeZone = ResolveIstanbulTimeZone();

    public async Task<TransitionReportViewModel> GetTransitionsAsync(
        DateOnly from, DateOnly to, Guid? employeeId, string? eventType,
        CancellationToken cancellationToken = default)
    {
        ValidateRange(from, to);
        var normalizedEventType = eventType is "Giris" or "Cikis" ? eventType : null;
        var startUtc = TimeZoneInfo.ConvertTimeToUtc(from.ToDateTime(TimeOnly.MinValue), IstanbulTimeZone);
        var endUtc = TimeZoneInfo.ConvertTimeToUtc(to.AddDays(1).ToDateTime(TimeOnly.MinValue), IstanbulTimeZone);

        var query = db.AccessLogs.AsNoTracking()
            .Where(x => x.LogDate >= startUtc && x.LogDate < endUtc);
        if (employeeId.HasValue) query = query.Where(x => x.UserId == employeeId.Value);
        if (normalizedEventType is not null) query = query.Where(x => x.LogType == normalizedEventType);

        var records = await (
            from log in query
            join user in db.Users.AsNoTracking() on log.UserId equals user.Id
            join zone in db.Zones.AsNoTracking() on log.ZoneId equals zone.Id
            join department in db.Departments.AsNoTracking() on user.DepartmentId equals department.Id into departments
            from department in departments.DefaultIfEmpty()
            join workplace in db.Workplaces.AsNoTracking() on zone.WorkplaceId equals workplace.Id into workplaces
            from workplace in workplaces.DefaultIfEmpty()
            orderby log.LogDate descending
            select new
            {
                log.Id, log.UserId, user.EmployeeNumber, user.Name,
                Department = department == null ? user.DepartmentLegacy : department.Name,
                log.LogDate, log.LogType, ZoneName = zone.Name,
                WorkplaceName = workplace == null ? null : workplace.Name,
                log.Source
            }).ToListAsync(cancellationToken);

        var rows = records.Select(x =>
        {
            var utc = DateTime.SpecifyKind(x.LogDate, DateTimeKind.Utc);
            var local = TimeZoneInfo.ConvertTimeFromUtc(utc, IstanbulTimeZone);
            return new TransitionReportRow(x.Id, x.UserId, x.EmployeeNumber, x.Name, x.Department,
                DateOnly.FromDateTime(local), TimeOnly.FromDateTime(local), x.LogType,
                x.ZoneName, x.WorkplaceName, x.Source);
        }).ToList();

        return new TransitionReportViewModel(from, to, employeeId, normalizedEventType, rows,
            await GetEmployeeOptionsAsync(cancellationToken));
    }

    public async Task<LeaveReportViewModel> GetLeavesAsync(
        DateOnly from, DateOnly to, Guid? employeeId, LeaveType? leaveType,
        LeaveRequestStatus? status, CancellationToken cancellationToken = default)
    {
        ValidateRange(from, to);
        var query = db.LeaveRequests.AsNoTracking()
            .Include(x => x.User).ThenInclude(x => x.Department)
            .Where(x => x.StartDate <= to && x.EndDate >= from);
        if (employeeId.HasValue) query = query.Where(x => x.UserId == employeeId.Value);
        if (leaveType.HasValue) query = query.Where(x => x.LeaveType == leaveType.Value);
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);

        var requests = await query
            .OrderByDescending(x => x.StartDate)
            .ThenBy(x => x.User.Name)
            .ToListAsync(cancellationToken);
        var rows = new List<LeaveReportRow>(requests.Count);
        foreach (var request in requests)
        {
            var countedFrom = request.StartDate < from ? from : request.StartDate;
            var countedTo = request.EndDate > to ? to : request.EndDate;
            var workDayCount = await GetWorkDayCountAsync(request.UserId, countedFrom,
                countedTo, request.DayPortion, cancellationToken);
            rows.Add(new LeaveReportRow(request.Id, request.UserId, request.User.EmployeeNumber,
                request.User.Name, request.User.Department?.Name ?? request.User.DepartmentLegacy,
                request.LeaveType, request.StartDate, request.EndDate, request.DayPortion,
                workDayCount, request.Status, request.Reason));
        }

        return new LeaveReportViewModel(from, to, employeeId, leaveType, status, rows,
            await GetEmployeeOptionsAsync(cancellationToken));
    }

    private async Task<IReadOnlyList<EmployeeOptionDto>> GetEmployeeOptionsAsync(CancellationToken cancellationToken) =>
        await db.Users.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new EmployeeOptionDto(x.Id, x.EmployeeNumber, x.Name))
            .ToListAsync(cancellationToken);

    private async Task<double> GetWorkDayCountAsync(Guid employeeId, DateOnly startDate, DateOnly endDate,
        LeaveDayPortion portion, CancellationToken cancellationToken)
    {
        var count = 0d;
        for (var date = startDate; date <= endDate; date = date.AddDays(1))
            if ((await workCalendar.ResolveAsync(employeeId, date, cancellationToken)).IsWorkingDay) count++;
        return portion == LeaveDayPortion.FullDay ? count : Math.Min(.5, count);
    }

    private static void ValidateRange(DateOnly from, DateOnly to)
    {
        if (from > to) throw new ArgumentException("Başlangıç tarihi bitiş tarihinden sonra olamaz.");
        if (to.DayNumber - from.DayNumber > 92) throw new ArgumentException("En fazla 93 günlük rapor alınabilir.");
    }

    private static TimeZoneInfo ResolveIstanbulTimeZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time"); }
    }
}
