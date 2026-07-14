using FaydamPDKS.Core.DTOs;
using FaydamPDKS.Core.Enums;
using FaydamPDKS.Core.Interfaces;
using FaydamPDKS.Core.Models;

namespace FaydamPDKS.Api;

public sealed class MobileLeaveRequestService(
    ILeaveRequestRepository leaveRequests,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IConfiguration configuration) : ILeaveRequestService
{
    public async Task<IReadOnlyList<LeaveRequestDto>> GetMineAsync(Guid userId, CancellationToken cancellationToken = default) =>
        (await leaveRequests.GetForUserAsync(userId, cancellationToken)).Select(Map).ToArray();

    public async Task<LeaveRequestDto> CreateAsync(Guid userId, CreateLeaveRequestDto request, CancellationToken cancellationToken = default)
    {
        if (request.StartDate > request.EndDate)
            throw new ArgumentException("Başlangıç tarihi bitiş tarihinden sonra olamaz.");
        if (request.EndDate.DayNumber - request.StartDate.DayNumber + 1 > 365)
            throw new ArgumentException("Tek izin talebi 365 günden uzun olamaz.");
        var timeZone = ResolveTimeZone(configuration["Attendance:TimeZone"] ?? "Europe/Istanbul");
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), timeZone).DateTime);
        if (request.StartDate < today)
            throw new ArgumentException("Geçmiş tarihli izin talebi oluşturulamaz.");
        if (await leaveRequests.HasActiveOverlapAsync(userId, request.StartDate, request.EndDate, cancellationToken))
            throw new InvalidOperationException("Seçilen tarihlerde bekleyen veya onaylanmış başka bir izin var.");

        var entity = new LeaveRequest
        {
            UserId = userId,
            LeaveType = request.LeaveType,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim(),
            Status = LeaveRequestStatus.Pending,
            CreatedAt = timeProvider.GetUtcNow()
        };
        await leaveRequests.AddAsync(entity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(entity);
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

    private static LeaveRequestDto Map(LeaveRequest x) => new(
        x.Id, x.LeaveType, x.StartDate, x.EndDate,
        x.EndDate.DayNumber - x.StartDate.DayNumber + 1,
        x.Reason, x.Status, x.CreatedAt, x.ReviewNote);

    private static TimeZoneInfo ResolveTimeZone(string id)
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time"); }
    }
}
