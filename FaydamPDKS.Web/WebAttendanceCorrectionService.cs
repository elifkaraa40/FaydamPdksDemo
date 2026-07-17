using FaydamPDKS.Core.DTOs;
using FaydamPDKS.Core.Enums;
using FaydamPDKS.Core.Interfaces;
using FaydamPDKS.Core.Models;

namespace FaydamPDKS.Web;

public sealed class WebAttendanceCorrectionService(
    IAttendanceCorrectionRepository corrections,
    INotificationRepository notifications,
    IAuditTrail auditTrail,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IWorkLocationService? workLocations = null) : IWebAttendanceCorrectionService
{
    public async Task<IReadOnlyList<AttendanceCorrectionReviewDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
        (await corrections.GetAllAsync(cancellationToken)).Select(x => new AttendanceCorrectionReviewDto(x.Id, x.UserId,
            x.User.Name, x.User.EmployeeNumber, x.WorkDate, x.RequestedEntry, x.RequestedExit, x.Reason,
            x.Status, x.CreatedAt, x.ReviewNote, x.CorrectionType, x.ProjectName, x.CustomerName, x.FieldAddress)).ToArray();

    public async Task<bool> ReviewAsync(Guid id, Guid reviewerId, ReviewAttendanceCorrectionDto request, string? correlationId, CancellationToken cancellationToken = default)
    {
        var entity = await corrections.GetAsync(id, true, cancellationToken);
        if (entity is null) return false;
        if (entity.Status != AttendanceCorrectionStatus.Pending) throw new InvalidOperationException("Talep daha önce sonuçlandırılmış.");
        if (entity.UserId == reviewerId) throw new InvalidOperationException("Kendi düzeltme talebinizi karara bağlayamazsınız.");
        var oldValues = new { entity.Status, entity.WorkDate, entity.RequestedEntry, entity.RequestedExit };
        entity.Status = request.Approve ? AttendanceCorrectionStatus.Approved : AttendanceCorrectionStatus.Rejected;
        entity.ReviewedAt = timeProvider.GetUtcNow();
        entity.ReviewedByUserId = reviewerId;
        entity.ReviewNote = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
        if (request.Approve && entity.CorrectionType == AttendanceCorrectionType.PastFieldWork)
        {
            if (workLocations is null) throw new InvalidOperationException("Çalışma konumu servisi kullanılamıyor.");
            await workLocations.CreateAssignmentAsync(new CreateWorkLocationAssignmentDto
            {
                UserId = entity.UserId, LocationType = WorkLocationType.Field, StartDate = entity.WorkDate, EndDate = entity.WorkDate,
                RecurrenceType = WorkLocationRecurrenceType.EveryWorkday, Reason = entity.Reason, ProjectName = entity.ProjectName,
                CustomerName = entity.CustomerName, FieldAddress = entity.FieldAddress
            }, reviewerId, cancellationToken);
        }

        await auditTrail.RecordAsync(reviewerId,
            request.Approve ? "AttendanceCorrection.Approved" : "AttendanceCorrection.Rejected",
            nameof(AttendanceCorrectionRequest), entity.Id.ToString(), oldValues,
            new { entity.Status, entity.ReviewedAt, entity.ReviewNote }, correlationId, cancellationToken);
        await notifications.AddAsync(new Notification
        {
            UserId = entity.UserId,
            Type = request.Approve ? NotificationType.AttendanceCorrectionApproved : NotificationType.AttendanceCorrectionRejected,
            Title = request.Approve ? "Puantaj düzeltmeniz onaylandı" : "Puantaj düzeltmeniz reddedildi",
            Message = $"{entity.WorkDate:dd.MM.yyyy} tarihli puantaj düzeltme talebiniz {(request.Approve ? "onaylandı" : "reddedildi")}.",
            RelatedEntityId = entity.Id, CreatedAt = timeProvider.GetUtcNow()
        }, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
