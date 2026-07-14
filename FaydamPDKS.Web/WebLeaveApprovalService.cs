using FaydamPDKS.Core.DTOs;
using FaydamPDKS.Core.Enums;
using FaydamPDKS.Core.Interfaces;
using FaydamPDKS.Core.Models;
namespace FaydamPDKS.Web;

public sealed class WebLeaveApprovalService(
    ILeaveRequestRepository leaveRequests,
    INotificationRepository notifications,
    IAuditTrail auditTrail,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IWebLeaveApprovalService
{
    public async Task<IReadOnlyList<LeaveReviewListItemDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
        (await leaveRequests.GetAllWithUsersAsync(cancellationToken)).Select(Map).ToArray();

    public async Task<LeaveReviewListItemDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await leaveRequests.GetByIdWithUserAsync(id, false, cancellationToken);
        return entity is null ? null : Map(entity);
    }

    public async Task<bool> ReviewAsync(Guid id, Guid reviewerUserId, ReviewLeaveRequestDto request, CancellationToken cancellationToken = default)
    {
        var entity = await leaveRequests.GetByIdWithUserAsync(id, true, cancellationToken);
        if (entity is null) return false;
        if (entity.Status != LeaveRequestStatus.Pending)
            throw new InvalidOperationException("Bu izin talebi daha önce sonuçlandırılmış.");
        if (entity.UserId == reviewerUserId)
            throw new InvalidOperationException("Kendi izin talebinizi onaylayamaz veya reddedemezsiniz.");

        entity.Status = request.Approve ? LeaveRequestStatus.Approved : LeaveRequestStatus.Rejected;
        entity.ReviewedAt = timeProvider.GetUtcNow();
        entity.ReviewedByUserId = reviewerUserId;
        entity.ReviewNote = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
        await auditTrail.RecordAsync(reviewerUserId, request.Approve ? "LeaveRequest.Approved" : "LeaveRequest.Rejected",
            nameof(LeaveRequest), entity.Id.ToString(),
            new { Status = LeaveRequestStatus.Pending },
            new { entity.Status, entity.ReviewNote, entity.ReviewedAt }, cancellationToken: cancellationToken);
        await notifications.AddAsync(new Notification
        {
            UserId = entity.UserId,
            Type = request.Approve ? NotificationType.LeaveApproved : NotificationType.LeaveRejected,
            Title = request.Approve ? "İzin talebiniz onaylandı" : "İzin talebiniz reddedildi",
            Message = $"{entity.StartDate:dd.MM.yyyy} - {entity.EndDate:dd.MM.yyyy} tarihli izin talebiniz {(request.Approve ? "onaylandı" : "reddedildi")}.",
            RelatedEntityId = entity.Id,
            CreatedAt = timeProvider.GetUtcNow()
        }, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static LeaveReviewListItemDto Map(LeaveRequest x) => new(
        x.Id, x.UserId, x.User.Name, x.LeaveType, x.StartDate, x.EndDate,
        x.EndDate.DayNumber - x.StartDate.DayNumber + 1,
        x.Reason, x.Status, x.CreatedAt, x.ReviewNote);
}
