using FaydamPDKS.Core.DTOs;
using FaydamPDKS.Core.Enums;
using FaydamPDKS.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace FaydamPDKS.Data;

public sealed class DashboardQueryService(
    AppDbContext context,
    IConfiguration configuration,
    TimeProvider timeProvider) : IDashboardQueryService
{
    public async Task<DashboardDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var timeZone = ResolveTimeZone(configuration["Attendance:TimeZone"] ?? "Europe/Istanbul");
        var localNow = TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), timeZone);
        var today = DateOnly.FromDateTime(localNow.DateTime);
        var localStart = today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        var fromUtc = TimeZoneInfo.ConvertTimeToUtc(localStart, timeZone);
        var toUtc = TimeZoneInfo.ConvertTimeToUtc(localStart.AddDays(1), timeZone);
        var shiftStart = TimeOnly.Parse(configuration["Attendance:DefaultShiftStart"] ?? "09:00");
        var tolerance = int.TryParse(configuration["Attendance:LateToleranceMinutes"], out var configuredTolerance)
            ? configuredTolerance
            : 5;
        var lateBoundaryUtc = TimeZoneInfo.ConvertTimeToUtc(today.ToDateTime(shiftStart, DateTimeKind.Unspecified).AddMinutes(tolerance), timeZone);

        var totalPersonnel = await context.Users.CountAsync(cancellationToken);
        var presentUserIds = await context.AccessLogs.AsNoTracking()
            .Where(x => x.LogDate >= fromUtc && x.LogDate < toUtc && x.LogType == "Giris")
            .Select(x => x.UserId).Distinct().ToListAsync(cancellationToken);
        var lateCount = await context.AccessLogs.AsNoTracking()
            .Where(x => x.LogDate >= fromUtc && x.LogDate < toUtc && x.LogType == "Giris")
            .GroupBy(x => x.UserId)
            .CountAsync(x => x.Min(y => y.LogDate) > lateBoundaryUtc, cancellationToken);
        var onLeaveCount = await context.LeaveRequests.AsNoTracking()
            .CountAsync(x => x.Status == LeaveRequestStatus.Approved && x.StartDate <= today && x.EndDate >= today, cancellationToken);
        var pendingLeaveCount = await context.LeaveRequests.AsNoTracking()
            .CountAsync(x => x.Status == LeaveRequestStatus.Pending, cancellationToken);

        var movements = await (
            from log in context.AccessLogs.AsNoTracking()
            join user in context.Users.AsNoTracking() on log.UserId equals user.Id
            join zone in context.Zones.AsNoTracking() on log.ZoneId equals zone.Id into zones
            from zone in zones.DefaultIfEmpty()
            orderby log.LogDate descending
            select new { user.Name, user.Id, log.LogDate, log.LogType, ZoneName = zone == null ? "Bilinmeyen bölge" : zone.Name })
            .Take(5).ToListAsync(cancellationToken);

        var pendingLeaves = await context.LeaveRequests.AsNoTracking().Include(x => x.User)
            .Where(x => x.Status == LeaveRequestStatus.Pending)
            .OrderBy(x => x.CreatedAt).Take(3)
            .Select(x => new DashboardLeaveDto(x.Id, x.User.Name, x.StartDate, x.EndDate, x.LeaveType.ToString()))
            .ToListAsync(cancellationToken);

        return new DashboardDto(
            today,
            totalPersonnel,
            presentUserIds.Count,
            lateCount,
            onLeaveCount,
            Math.Max(0, totalPersonnel - presentUserIds.Count - onLeaveCount),
            pendingLeaveCount,
            movements.Select(x => new DashboardMovementDto(
                x.Name,
                $"FDM-{x.Id.ToString()[..6].ToUpperInvariant()}",
                TimeZoneInfo.ConvertTime(new DateTimeOffset(DateTime.SpecifyKind(x.LogDate, DateTimeKind.Utc)), timeZone),
                x.LogType,
                x.ZoneName)).ToArray(),
            pendingLeaves);
    }

    private static TimeZoneInfo ResolveTimeZone(string id)
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time"); }
    }
}
