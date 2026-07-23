using FaydamPDKS.Core.DTOs;
using FaydamPDKS.Core.Enums;
using FaydamPDKS.Core.Exceptions;
using FaydamPDKS.Core.Interfaces;
using FaydamPDKS.Core.Models;

namespace FaydamPDKS.Api;

public sealed class MobileLeaveRequestService(
    ILeaveRequestRepository leaveRequests,
    IUnitOfWork unitOfWork,
    IWorkCalendarResolver workCalendar,
    TimeProvider timeProvider,
    IConfiguration configuration,
    IManagerNotificationService? managerNotifications = null) : ILeaveRequestService
{
    public async Task<IReadOnlyList<LeaveRequestDto>> GetMineAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var result = new List<LeaveRequestDto>();
        foreach (var item in await leaveRequests.GetForUserAsync(userId, cancellationToken))
            result.Add(await MapAsync(item, cancellationToken));
        return result;
    }

    public async Task<LeaveRequestDto> CreateAsync(Guid userId, CreateLeaveRequestDto request, CancellationToken cancellationToken = default)
    {
        if (request.StartDate > request.EndDate)
            throw new ArgumentException("Başlangıç tarihi bitiş tarihinden sonra olamaz.");
        if (request.EndDate.DayNumber - request.StartDate.DayNumber + 1 > 365)
            throw new ArgumentException("Tek izin talebi 365 günden uzun olamaz.");
        if (request.DayPortion != LeaveDayPortion.FullDay && request.StartDate != request.EndDate)
            throw new ArgumentException("Yarım gün izin yalnızca tek bir tarih için seçilebilir.");
        var timeZone = ResolveTimeZone(configuration["Attendance:TimeZone"] ?? "Europe/Istanbul");
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), timeZone).DateTime);
        if (request.StartDate < today)
            throw new ArgumentException("Geçmiş tarihli izin talebi oluşturulamaz.");
        var overlap = await leaveRequests.FindActiveOverlapAsync(
            userId, request.StartDate, request.EndDate, cancellationToken);
        if (overlap is not null)
            throw new LeaveOverlapException(overlap.StartDate, overlap.EndDate);

        var entity = new LeaveRequest
        {
            UserId = userId,
            LeaveType = request.LeaveType,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            DayPortion = request.DayPortion,
            Reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim(),
            Status = LeaveRequestStatus.Pending,
            CreatedAt = timeProvider.GetUtcNow()
        };
        await leaveRequests.AddAsync(entity, cancellationToken);
        if (managerNotifications is not null)
            await managerNotifications.NotifyAsync(NotificationType.LeaveRequestCreated, "Yeni izin talebi",
                $"{request.StartDate:dd.MM.yyyy} - {request.EndDate:dd.MM.yyyy} tarihleri için yeni izin talebi gönderildi.", entity.Id, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await MapAsync(entity, cancellationToken);
    }

    public async Task<bool> CancelAsync(Guid userId, Guid requestId, CancellationToken cancellationToken = default)
    {
        var entity = await leaveRequests.GetForUserByIdAsync(userId, requestId, cancellationToken);
        if (entity is null) return false;
        if (entity.Status != LeaveRequestStatus.Pending)
            throw new InvalidOperationException("Yalnızca bekleyen izin talepleri iptal edilebilir.");
        entity.Status = LeaveRequestStatus.Cancelled;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<LeaveRequestDto> MapAsync(LeaveRequest x, CancellationToken cancellationToken) => new(
        x.Id, x.LeaveType, x.StartDate, x.EndDate,
        x.EndDate.DayNumber - x.StartDate.DayNumber + 1,
        await WorkDayCountAsync(x, cancellationToken), x.DayPortion,
        x.Reason, x.Status, x.CreatedAt, x.ReviewNote);

    private async Task<double> WorkDayCountAsync(LeaveRequest request, CancellationToken cancellationToken)
    {
        var count = 0d;
        for (var date = request.StartDate; date <= request.EndDate; date = date.AddDays(1))
            if ((await workCalendar.ResolveAsync(request.UserId, date, cancellationToken)).IsWorkingDay) count++;
        return request.DayPortion == LeaveDayPortion.FullDay ? count : Math.Min(.5, count);
    }

    private static TimeZoneInfo ResolveTimeZone(string id)
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time"); }
    }
}
