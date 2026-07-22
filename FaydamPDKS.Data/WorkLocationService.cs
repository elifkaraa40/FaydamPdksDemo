using FaydamPDKS.Core.DTOs;
using FaydamPDKS.Core.Enums;
using FaydamPDKS.Core.Interfaces;
using FaydamPDKS.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace FaydamPDKS.Data;

public sealed class WorkLocationService(
    AppDbContext db,
    IConfiguration configuration,
    TimeProvider clock,
    IManagerNotificationService managerNotifications,
    INotificationRepository notifications) : IWorkLocationService
{
    public bool FeatureEnabled => !bool.TryParse(configuration["Features:WorkLocations"], out var enabled) || enabled;

    public async Task<WorkLocationPageDto> GetManagementPageAsync(CancellationToken ct = default)
    {
        var assignments = await db.WorkLocationAssignments.AsNoTracking().Include(x => x.User).Include(x => x.Days)
            .OrderByDescending(x => x.IsActive).ThenByDescending(x => x.StartDate).ToListAsync(ct);
        var requests = await db.FieldWorkRequests.AsNoTracking().Include(x => x.User).Include(x => x.Days)
            .OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
        var employees = await db.Users.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name)
            .Select(x => new EmployeeOptionDto(x.Id, x.EmployeeNumber, x.Name)).ToListAsync(ct);
        return new(assignments.Select(Map).ToList(), requests.Select(Map).ToList(), employees, FeatureEnabled);
    }

    public async Task CreateAssignmentAsync(CreateWorkLocationAssignmentDto r, Guid actorId, CancellationToken ct = default)
    {
        EnsureEnabled(); Validate(r.StartDate, r.EndDate, r.RecurrenceType, r.Days);
        if (!await db.Users.AnyAsync(x => x.Id == r.UserId && x.IsActive, ct)) throw new InvalidOperationException("Personel bulunamadı.");
        var end = r.EndDate ?? DateOnly.MaxValue;
        if (await db.WorkLocationAssignments.AnyAsync(x => x.UserId == r.UserId && x.IsActive && x.StartDate <= end && (!x.EndDate.HasValue || x.EndDate >= r.StartDate), ct))
            throw new InvalidOperationException("Bu personelin seçilen tarih aralığında başka bir çalışma konumu planı var.");
        var entity = new WorkLocationAssignment { Id = Guid.NewGuid(), UserId = r.UserId, LocationType = r.LocationType,
            StartDate = r.StartDate, EndDate = r.EndDate, RecurrenceType = r.RecurrenceType, Reason = Clean(r.Reason),
            ProjectName = Clean(r.ProjectName), CustomerName = Clean(r.CustomerName), FieldAddress = Clean(r.FieldAddress),
            CreatedByUserId = actorId, CreatedAt = clock.GetUtcNow(), IsActive = true };
        foreach (var day in NormalizeDays(r.RecurrenceType, r.Days)) entity.Days.Add(new() { Id = Guid.NewGuid(), DayOfWeek = day });
        db.WorkLocationAssignments.Add(entity); await db.SaveChangesAsync(ct);
    }

    public async Task<bool> EndAssignmentAsync(Guid id, Guid actorId, CancellationToken ct = default)
    {
        var entity = await db.WorkLocationAssignments.FirstOrDefaultAsync(x => x.Id == id && x.IsActive, ct);
        if (entity is null) return false;
        entity.IsActive = false; entity.EndedAt = clock.GetUtcNow(); entity.EndedByUserId = actorId;
        var today = DateOnly.FromDateTime(clock.GetLocalNow().DateTime);
        if (!entity.EndDate.HasValue || entity.EndDate > today) entity.EndDate = today;
        await db.SaveChangesAsync(ct); return true;
    }

    public async Task<IReadOnlyList<FieldWorkRequestDto>> GetMyRequestsAsync(Guid userId, CancellationToken ct = default) =>
        (await db.FieldWorkRequests.AsNoTracking().Include(x => x.User).Include(x => x.Days).Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt).ToListAsync(ct)).Select(Map).ToList();

    public async Task CreateFieldRequestAsync(Guid userId, CreateFieldWorkRequestDto r, CancellationToken ct = default)
    {
        if (r.StartDate == r.EndDate)
        {
            r.RecurrenceType = WorkLocationRecurrenceType.Once;
            r.Days = [];
        }
        EnsureEnabled(); Validate(r.StartDate, r.EndDate, r.RecurrenceType, r.Days);
        var today = DateOnly.FromDateTime(clock.GetLocalNow().DateTime);
        if (r.StartDate < today) throw new InvalidOperationException("Geçmiş çalışma konumu kayıtları puantaj düzeltme talebi olarak gönderilmelidir.");
        if (r.LocationType is not (WorkLocationType.Field or WorkLocationType.Remote))
            throw new InvalidOperationException("Talep türü Field veya Remote olmalıdır.");
        if (await db.FieldWorkRequests.AnyAsync(x => x.UserId == userId
                && (x.Status == WorkLocationRequestStatus.Pending || x.Status == WorkLocationRequestStatus.Approved)
                && x.StartDate <= r.EndDate && x.EndDate >= r.StartDate, ct)
            || await db.LeaveRequests.AnyAsync(x => x.UserId == userId
                && (x.Status == LeaveRequestStatus.Pending || x.Status == LeaveRequestStatus.Approved)
                && x.StartDate <= r.EndDate && x.EndDate >= r.StartDate, ct))
            throw new InvalidOperationException("Seçilen tarih aralığında çakışan izin veya çalışma konumu kaydı var.");
        if (r.LocationType == WorkLocationType.Field && string.IsNullOrWhiteSpace(r.ProjectName))
            throw new InvalidOperationException("Saha görevi için proje bilgisi zorunludur.");
        var entity = new FieldWorkRequest { Id = Guid.NewGuid(), UserId = userId, LocationType = r.LocationType,
            StartDate = r.StartDate, EndDate = r.EndDate, RecurrenceType = r.RecurrenceType,
            ProjectName = r.LocationType == WorkLocationType.Field ? Clean(r.ProjectName) : null,
            CustomerName = r.LocationType == WorkLocationType.Field ? Clean(r.CustomerName) : null,
            FieldAddress = r.LocationType == WorkLocationType.Field ? Clean(r.FieldAddress) : null,
            Reason = r.Reason.Trim(), Status = WorkLocationRequestStatus.Pending, CreatedAt = clock.GetUtcNow() };
        foreach (var day in NormalizeDays(r.RecurrenceType, r.Days)) entity.Days.Add(new() { Id = Guid.NewGuid(), DayOfWeek = day });
        db.FieldWorkRequests.Add(entity);
        var locationLabel = r.LocationType == WorkLocationType.Field ? "saha görevi" : "uzaktan çalışma";
        var requesterName = await db.Users.AsNoTracking().Where(x => x.Id == userId).Select(x => x.Name).SingleAsync(ct);
        await managerNotifications.NotifyAsync(NotificationType.FieldWorkRequestCreated, "Yeni çalışma konumu talebi",
            $"{requesterName}, {r.StartDate:dd.MM.yyyy} - {r.EndDate:dd.MM.yyyy} tarihleri için {locationLabel} talebi oluşturdu.", entity.Id, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> CancelFieldRequestAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var entity = await db.FieldWorkRequests.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId && x.Status == WorkLocationRequestStatus.Pending, ct);
        if (entity is null) return false; entity.Status = WorkLocationRequestStatus.Cancelled; await db.SaveChangesAsync(ct); return true;
    }

    public async Task<bool> ReviewFieldRequestAsync(Guid id, Guid reviewerId, bool approve, string? note, CancellationToken ct = default)
    {
        var entity = await db.FieldWorkRequests.Include(x => x.Days).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return false;
        if (entity.Status != WorkLocationRequestStatus.Pending) throw new InvalidOperationException("Bu talep daha önce sonuçlandırılmış.");
        entity.Status = approve ? WorkLocationRequestStatus.Approved : WorkLocationRequestStatus.Rejected;
        entity.ReviewedByUserId = reviewerId; entity.ReviewedAt = clock.GetUtcNow(); entity.ReviewNote = Clean(note);
        if (approve)
        {
            var dto = new CreateWorkLocationAssignmentDto { UserId = entity.UserId, LocationType = entity.LocationType,
                StartDate = entity.StartDate, EndDate = entity.EndDate, RecurrenceType = entity.RecurrenceType,
                Days = entity.Days.Select(x => x.DayOfWeek).ToArray(), Reason = entity.Reason, ProjectName = entity.ProjectName,
                CustomerName = entity.CustomerName, FieldAddress = entity.FieldAddress };
            await CreateAssignmentAsync(dto, reviewerId, ct);
        }
        var locationLabel = entity.LocationType == WorkLocationType.Field ? "Saha görevi" : "Uzaktan çalışma";
        await notifications.AddAsync(new Notification
        {
            UserId = entity.UserId,
            Type = approve ? NotificationType.FieldWorkRequestApproved : NotificationType.FieldWorkRequestRejected,
            Title = approve ? $"{locationLabel} talebiniz onaylandı" : $"{locationLabel} talebiniz reddedildi",
            Message = $"{entity.StartDate:dd.MM.yyyy} - {entity.EndDate:dd.MM.yyyy} tarihli talebiniz {(approve ? "onaylandı" : "reddedildi")}.",
            RelatedEntityId = entity.Id,
            CreatedAt = clock.GetUtcNow()
        }, ct);
        await db.SaveChangesAsync(ct); return true;
    }

    public async Task<WorkLocationAssignment?> GetForDateAsync(Guid userId, DateOnly date, CancellationToken ct = default)
    {
        if (!FeatureEnabled) return null;
        var list = await db.WorkLocationAssignments.AsNoTracking().Include(x => x.Days)
            .Where(x => x.UserId == userId && x.IsActive && x.StartDate <= date && (!x.EndDate.HasValue || x.EndDate >= date)).ToListAsync(ct);
        return list.Where(x => Applies(x.RecurrenceType, x.Days.Select(d => d.DayOfWeek), date))
            .OrderByDescending(x => x.LocationType == WorkLocationType.Field).ThenByDescending(x => x.CreatedAt).FirstOrDefault();
    }

    public static bool Applies(WorkLocationRecurrenceType recurrence, IEnumerable<DayOfWeek> days, DateOnly date) =>
        recurrence is WorkLocationRecurrenceType.EveryWorkday or WorkLocationRecurrenceType.Once || days.Contains(date.DayOfWeek);
    private void EnsureEnabled() { if (!FeatureEnabled) throw new InvalidOperationException("Çalışma konumu özelliği kapalı."); }
    private static void Validate(DateOnly start, DateOnly? end, WorkLocationRecurrenceType recurrence, DayOfWeek[] days)
    { if (start == default || (end.HasValue && end < start)) throw new InvalidOperationException("Geçerli bir tarih aralığı seçin.");
      if (recurrence == WorkLocationRecurrenceType.Once && end != start) throw new InvalidOperationException("Tekrarlanmayan planın başlangıç ve bitiş tarihi aynı olmalıdır.");
      if (recurrence == WorkLocationRecurrenceType.SelectedWeekdays && NormalizeDays(recurrence, days).Length == 0) throw new InvalidOperationException("Haftalık tekrarda en az bir gün seçilmelidir."); }
    private static DayOfWeek[] NormalizeDays(WorkLocationRecurrenceType recurrence, IEnumerable<DayOfWeek>? days) =>
        recurrence == WorkLocationRecurrenceType.SelectedWeekdays ? (days ?? []).Where(x => x is not DayOfWeek.Saturday and not DayOfWeek.Sunday).Distinct().ToArray() : [];
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static WorkLocationAssignmentDto Map(WorkLocationAssignment x) => new(x.Id, x.UserId, x.User.Name, x.LocationType, x.StartDate, x.EndDate, x.RecurrenceType, x.Days.Select(d => d.DayOfWeek).ToArray(), x.Reason, x.ProjectName, x.CustomerName, x.FieldAddress, x.IsActive);
    private static FieldWorkRequestDto Map(FieldWorkRequest x) => new(x.Id, x.UserId, x.User.Name, x.LocationType, x.StartDate, x.EndDate, x.RecurrenceType, x.Days.Select(d => d.DayOfWeek).ToArray(), x.ProjectName, x.CustomerName, x.FieldAddress, x.Reason, x.Status, x.CreatedAt, x.ReviewNote);
}
