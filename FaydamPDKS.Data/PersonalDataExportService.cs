using FaydamPDKS.Core.DTOs;
using FaydamPDKS.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FaydamPDKS.Data;

public sealed class PersonalDataExportService(AppDbContext context, TimeProvider timeProvider) : IPersonalDataExportService
{
    public async Task<PersonalDataExportDto?> ExportAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await context.Users.AsNoTracking().Include(x => x.Workplace).Include(x => x.Department).SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null) return null;
        var rawEvents = await context.AccessLogs.AsNoTracking().Where(x => x.UserId == userId).OrderBy(x => x.LogDate).ToListAsync(cancellationToken);
        var events = rawEvents.Select(x => new PersonalAttendanceEventDto(x.Id,
            new DateTimeOffset(DateTime.SpecifyKind(x.LogDate, DateTimeKind.Utc)), x.LogType, x.ZoneId, x.Source, x.DeviceEventId)).ToArray();
        var leaves = await context.LeaveRequests.AsNoTracking().Where(x => x.UserId == userId).OrderBy(x => x.CreatedAt).ToListAsync(cancellationToken);
        var corrections = await context.AttendanceCorrectionRequests.AsNoTracking().Where(x => x.UserId == userId).OrderBy(x => x.CreatedAt).ToListAsync(cancellationToken);
        var notifications = await context.Notifications.AsNoTracking().Where(x => x.UserId == userId).OrderBy(x => x.CreatedAt).ToListAsync(cancellationToken);
        return new PersonalDataExportDto(timeProvider.GetUtcNow(),
            new(user.Id, user.EmployeeNumber, user.Name, user.Email, user.PhoneNumber, user.Workplace?.Name, user.Department?.Name ?? user.DepartmentLegacy, user.HireDate, user.IsActive, user.IsEmailNotificationEnabled, user.IsSmsNotificationEnabled),
            events,
            leaves.Select(x => new PersonalLeaveDto(x.Id, x.LeaveType.ToString(), x.StartDate, x.EndDate, x.Reason, x.Status.ToString(), x.CreatedAt, x.ReviewedAt, x.ReviewNote)).ToArray(),
            corrections.Select(x => new PersonalCorrectionDto(x.Id, x.WorkDate, x.RequestedEntry, x.RequestedExit, x.Reason, x.Status.ToString(), x.CreatedAt, x.ReviewedAt, x.ReviewNote)).ToArray(),
            notifications.Select(x => new PersonalNotificationDto(x.Id, x.Type.ToString(), x.Title, x.Message, x.CreatedAt, x.ReadAt)).ToArray());
    }
}
