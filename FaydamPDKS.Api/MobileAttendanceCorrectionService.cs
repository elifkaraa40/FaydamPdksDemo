using FaydamPDKS.Core.DTOs;
using FaydamPDKS.Core.Enums;
using FaydamPDKS.Core.Interfaces;
using FaydamPDKS.Core.Models;

namespace FaydamPDKS.Api;

public sealed class MobileAttendanceCorrectionService(
    IAttendanceCorrectionRepository corrections,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IConfiguration configuration,
    IManagerNotificationService? managerNotifications = null) : IAttendanceCorrectionService
{
    public async Task<IReadOnlyList<AttendanceCorrectionDto>> GetMineAsync(Guid userId, CancellationToken cancellationToken = default) =>
        (await corrections.GetForUserAsync(userId, cancellationToken)).Select(Map).ToArray();

    public async Task<AttendanceCorrectionDto> CreateAsync(Guid userId, CreateAttendanceCorrectionDto request, CancellationToken cancellationToken = default)
    {
        var timeZone = ResolveTimeZone(configuration["Attendance:TimeZone"] ?? "Europe/Istanbul");
        var localNow = TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), timeZone).DateTime;
        var today = DateOnly.FromDateTime(localNow);
        var now = TimeOnly.FromDateTime(localNow);
        if (request.WorkDate >= today) throw new ArgumentException("Puantaj düzeltme talebi sadece geçmiş tarihler için oluşturulabilir.");
        if (request.WorkDate < today.AddDays(-90)) throw new ArgumentException("En fazla son 90 gün için düzeltme talep edilebilir.");
        if (request.CorrectionType == AttendanceCorrectionType.TimeCorrection && request.RequestedEntry == request.RequestedExit) throw new ArgumentException("Giriş ve çıkış saati aynı olamaz.");
        var reason = request.Reason.Trim();
        if (reason.Length < 10) throw new ArgumentException("Düzeltme gerekçesi en az 10 karakter olmalıdır.");
        if (await corrections.HasPendingAsync(userId, request.WorkDate, cancellationToken))
            throw new InvalidOperationException("Bu tarih için zaten bekleyen bir düzeltme talebi var.");

        var entity = new AttendanceCorrectionRequest
        {
            Id = Guid.NewGuid(), UserId = userId, WorkDate = request.WorkDate,
            CorrectionType = request.CorrectionType,
            RequestedEntry = request.RequestedEntry, RequestedExit = request.RequestedExit,
            ProjectName = request.ProjectName?.Trim(), CustomerName = request.CustomerName?.Trim(), FieldAddress = request.FieldAddress?.Trim(),
            Reason = reason, Status = AttendanceCorrectionStatus.Pending, CreatedAt = timeProvider.GetUtcNow()
        };
        await corrections.AddAsync(entity, cancellationToken);
        if (managerNotifications is not null)
            await managerNotifications.NotifyAsync(NotificationType.AttendanceCorrectionCreated, "Yeni puantaj düzeltme talebi",
                $"{request.WorkDate:dd.MM.yyyy} tarihli puantaj için düzeltme talebi gönderildi.", entity.Id, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    private static AttendanceCorrectionDto Map(AttendanceCorrectionRequest x) => new(x.Id, x.WorkDate, x.RequestedEntry, x.RequestedExit, x.Reason, x.Status, x.CreatedAt, x.ReviewNote, x.CorrectionType, x.ProjectName, x.CustomerName, x.FieldAddress);
    private static TimeZoneInfo ResolveTimeZone(string id)
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time"); }
    }
}
