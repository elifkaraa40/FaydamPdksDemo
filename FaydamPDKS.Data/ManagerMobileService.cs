using FaydamPDKS.Core.DTOs;
using FaydamPDKS.Core.Enums;
using FaydamPDKS.Core.Interfaces;
using FaydamPDKS.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace FaydamPDKS.Data;

public sealed class ManagerMobileService(
    AppDbContext db,
    IAttendanceReportService reports,
    IAuditTrail audit,
    IWorkLocationService workLocations,
    IWorkCalendarResolver workCalendar,
    TimeProvider clock) : IManagerMobileService
{
    private static readonly SemaphoreSlim EmployeeNumberLock = new(1, 1);

    public async Task<ManagerApprovalsSummaryDto> GetApprovalsSummaryAsync(Guid managerId, CancellationToken ct = default)
    {
        var scope = await ScopeAsync(managerId, ct);
        return new(
            await ScopeUsers(db.Users.AsNoTracking().Where(x => x.AccountStatus == AccountStatus.PendingApproval), scope).CountAsync(ct),
            await ScopeRequests(db.LeaveRequests.AsNoTracking().Where(x => x.Status == LeaveRequestStatus.Pending), scope).CountAsync(ct),
            await ScopeRequests(db.AttendanceCorrectionRequests.AsNoTracking().Where(x => x.Status == AttendanceCorrectionStatus.Pending), scope).CountAsync(ct),
            await ScopeRequests(db.FieldWorkRequests.AsNoTracking().Where(x => x.Status == WorkLocationRequestStatus.Pending), scope).CountAsync(ct));
    }

    public async Task<ManagerDashboardDto> GetDashboardAsync(Guid managerId, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(clock.GetLocalNow().DateTime);
        var scope = await ScopeAsync(managerId, ct);
        var allowed = await ScopedPersonnel(scope).Select(x => x.Id).ToArrayAsync(ct);
        var report = await reports.GetAsync(today, today, cancellationToken: ct);
        var rows = report.Rows.Where(x => allowed.Contains(x.EmployeeId)).ToArray();
        var onBreak = await db.BreakRecords.AsNoTracking().CountAsync(x => allowed.Contains(x.UserId) && x.EndedAt == null, ct);
        return new(
            await GetApprovalsSummaryAsync(managerId, ct),
            rows.Count(x => x.FirstEntry.HasValue), rows.Count(x => x.LastExit.HasValue),
            rows.Count(x => IsMissing(x.Status)),
            rows.Count(x => x.WorkLocation == "Office"), rows.Count(x => x.WorkLocation == "Field"),
            rows.Count(x => x.WorkLocation == "Remote"), onBreak);
    }

    public async Task<PagedResultDto<ManagerRegistrationDto>> GetRegistrationsAsync(Guid managerId, AccountStatus? status, int page, int pageSize, CancellationToken ct = default)
    {
        var scope = await ScopeAsync(managerId, ct);
        var query = ScopeUsers(db.Users.AsNoTracking(), scope);
        query = query.Where(x => x.AccountStatus == (status ?? AccountStatus.PendingApproval)).OrderBy(x => x.Name);
        return await PageAsync(query.Select(x => new ManagerRegistrationDto(x.Id, x.Name, x.Email, x.PhoneNumber,
            x.AccountStatus, x.EmployeeNumber, x.DepartmentId)), page, pageSize, ct);
    }

    public async Task<bool> ReviewRegistrationAsync(Guid id, Guid managerId, ReviewRegistrationDto request, string? correlationId, CancellationToken ct = default)
    {
        var scope = await ScopeAsync(managerId, ct);
        var user = await ScopeUsers(db.Users, scope).SingleOrDefaultAsync(x => x.Id == id, ct);
        if (user is null) return false;
        if (user.AccountStatus != AccountStatus.PendingApproval) throw new InvalidOperationException("Bu kayıt daha önce sonuçlandırılmış.");
        var old = new { user.AccountStatus, user.EmployeeNumber, user.DepartmentId, user.WorkplaceId, user.IsActive };
        if (request.Approve)
        {
            await EmployeeNumberLock.WaitAsync(ct);
            try
            {
                var number = string.IsNullOrWhiteSpace(request.EmployeeNumber)
                    ? await GenerateEmployeeNumberAsync(ct)
                    : request.EmployeeNumber.Trim().ToUpperInvariant();
                if (number.Length < 2 || await db.Users.AnyAsync(x => x.Id != id && x.EmployeeNumber == number, ct))
                    throw new InvalidOperationException("Geçerli ve benzersiz bir personel numarası girin.");
                Department? department = null;
                if (request.DepartmentId.HasValue)
                {
                    department = await db.Departments.SingleOrDefaultAsync(x => x.Id == request.DepartmentId && x.IsActive, ct)
                        ?? throw new InvalidOperationException("Departman bulunamadı.");
                    if (scope.HasValue && department.WorkplaceId != scope.Value) throw new UnauthorizedAccessException("Departman yetki kapsamı dışında.");
                }
                user.EmployeeNumber = number;
                user.DepartmentId = department?.Id;
                user.WorkplaceId = department?.WorkplaceId ?? scope;
                user.AccountStatus = AccountStatus.Active;
                user.IsActive = true;
            }
            finally { EmployeeNumberLock.Release(); }
        }
        else
        {
            user.AccountStatus = AccountStatus.Rejected;
            user.IsActive = false;
        }
        db.Notifications.Add(NewNotification(user.Id,
            request.Approve ? NotificationType.RegistrationApproved : NotificationType.RegistrationRejected,
            request.Approve ? "Hesabınız onaylandı" : "Kaydınız onaylanmadı",
            request.Approve ? $"PDKS hesabınız {user.EmployeeNumber} sicil numarasıyla kullanıma açıldı." : AppendNote("Detay için işyerinizle iletişime geçin.", request.Note), user.Id));
        await audit.RecordAsync(managerId, request.Approve ? "Registration.Approved" : "Registration.Rejected", nameof(User), user.Id.ToString(), old,
            new { user.AccountStatus, user.EmployeeNumber, user.DepartmentId, user.WorkplaceId, user.IsActive, Note = Clean(request.Note) }, correlationId, ct);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<PagedResultDto<LeaveReviewListItemDto>> GetLeaveRequestsAsync(Guid managerId, LeaveRequestStatus? status, int page, int pageSize, CancellationToken ct = default)
    {
        var scope = await ScopeAsync(managerId, ct);
        var query = ScopeRequests(db.LeaveRequests.AsNoTracking().Include(x => x.User), scope);
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);
        var all = await query.OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
        var mapped = new List<LeaveReviewListItemDto>(all.Count);
        foreach (var x in all)
        {
            var workDays = 0d;
            for (var date = x.StartDate; date <= x.EndDate; date = date.AddDays(1))
                if ((await workCalendar.ResolveAsync(x.UserId, date, ct)).IsWorkingDay) workDays++;
            if (x.DayPortion != LeaveDayPortion.FullDay) workDays = Math.Min(.5, workDays);
            mapped.Add(new(x.Id, x.UserId, x.User.Name, x.LeaveType, x.StartDate, x.EndDate,
                x.EndDate.DayNumber - x.StartDate.DayNumber + 1, workDays, x.DayPortion, x.Reason, x.Status, x.CreatedAt, x.ReviewNote));
        }
        return Page(mapped, page, pageSize);
    }

    public async Task<bool> ReviewLeaveRequestAsync(Guid id, Guid managerId, ReviewLeaveRequestDto request, string? correlationId, CancellationToken ct = default)
    {
        var scope = await ScopeAsync(managerId, ct);
        var entity = await ScopeRequests(db.LeaveRequests.Include(x => x.User), scope).SingleOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return false;
        if (entity.Status != LeaveRequestStatus.Pending) throw new InvalidOperationException("Bu izin talebi daha önce sonuçlandırılmış.");
        if (entity.UserId == managerId) throw new InvalidOperationException("Kendi izin talebinizi karara bağlayamazsınız.");
        entity.Status = request.Approve ? LeaveRequestStatus.Approved : LeaveRequestStatus.Rejected;
        entity.ReviewedAt = clock.GetUtcNow(); entity.ReviewedByUserId = managerId; entity.ReviewNote = Clean(request.Note);
        await audit.RecordAsync(managerId, request.Approve ? "LeaveRequest.Approved" : "LeaveRequest.Rejected", nameof(LeaveRequest), id.ToString(),
            new { Status = LeaveRequestStatus.Pending }, new { entity.Status, entity.ReviewedAt, entity.ReviewNote }, correlationId, ct);
        // The original "new leave request" notification is no longer actionable
        // once a manager has decided. Keep it in history, but do not let it
        // appear before the approval/rejection notification in mobile.
        var createdNotification = await db.Notifications
            .Where(x => x.UserId == entity.UserId
                && x.RelatedEntityId == entity.Id
                && x.Type == NotificationType.LeaveRequestCreated
                && !x.ReadAt.HasValue)
            .ToListAsync(ct);
        foreach (var notification in createdNotification)
            notification.ReadAt = clock.GetUtcNow();
        db.Notifications.Add(NewNotification(entity.UserId, request.Approve ? NotificationType.LeaveApproved : NotificationType.LeaveRejected,
            request.Approve ? "İzin talebiniz onaylandı" : "İzin talebiniz reddedildi",
            AppendNote($"{entity.StartDate:dd.MM.yyyy} - {entity.EndDate:dd.MM.yyyy} tarihli izin talebiniz {(request.Approve ? "onaylandı" : "reddedildi")}.", request.Note), entity.Id));
        await db.SaveChangesAsync(ct); return true;
    }

    public async Task<PagedResultDto<AttendanceCorrectionReviewDto>> GetAttendanceCorrectionsAsync(Guid managerId, AttendanceCorrectionStatus? status, int page, int pageSize, CancellationToken ct = default)
    {
        var scope = await ScopeAsync(managerId, ct);
        var query = ScopeRequests(db.AttendanceCorrectionRequests.AsNoTracking().Include(x => x.User), scope);
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);
        return await PageAsync(query.OrderByDescending(x => x.CreatedAt).Select(x => new AttendanceCorrectionReviewDto(x.Id, x.UserId, x.User.Name,
            x.User.EmployeeNumber, x.WorkDate, x.RequestedEntry, x.RequestedExit, x.Reason, x.Status, x.CreatedAt, x.ReviewNote,
            x.CorrectionType, x.ProjectName, x.CustomerName, x.FieldAddress)), page, pageSize, ct);
    }

    public async Task<bool> ReviewAttendanceCorrectionAsync(Guid id, Guid managerId, ReviewAttendanceCorrectionDto request, string? correlationId, CancellationToken ct = default)
    {
        var scope = await ScopeAsync(managerId, ct);
        var entity = await ScopeRequests(db.AttendanceCorrectionRequests.Include(x => x.User), scope).SingleOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return false;
        if (entity.Status != AttendanceCorrectionStatus.Pending) throw new InvalidOperationException("Talep daha önce sonuçlandırılmış.");
        if (entity.UserId == managerId) throw new InvalidOperationException("Kendi düzeltme talebinizi karara bağlayamazsınız.");
        var old = new { entity.Status, entity.WorkDate, entity.RequestedEntry, entity.RequestedExit };
        entity.Status = request.Approve ? AttendanceCorrectionStatus.Approved : AttendanceCorrectionStatus.Rejected;
        entity.ReviewedAt = clock.GetUtcNow(); entity.ReviewedByUserId = managerId; entity.ReviewNote = Clean(request.Note);
        if (request.Approve && entity.CorrectionType == AttendanceCorrectionType.PastFieldWork)
            await workLocations.CreateAssignmentAsync(new CreateWorkLocationAssignmentDto { UserId = entity.UserId, LocationType = WorkLocationType.Field,
                StartDate = entity.WorkDate, EndDate = entity.WorkDate, RecurrenceType = WorkLocationRecurrenceType.EveryWorkday,
                Reason = entity.Reason, ProjectName = entity.ProjectName, CustomerName = entity.CustomerName, FieldAddress = entity.FieldAddress }, managerId, ct);
        await audit.RecordAsync(managerId, request.Approve ? "AttendanceCorrection.Approved" : "AttendanceCorrection.Rejected", nameof(AttendanceCorrectionRequest), id.ToString(), old,
            new { entity.Status, entity.ReviewedAt, entity.ReviewNote }, correlationId, ct);
        db.Notifications.Add(NewNotification(entity.UserId,
            request.Approve ? NotificationType.AttendanceCorrectionApproved : NotificationType.AttendanceCorrectionRejected,
            request.Approve ? "Puantaj düzeltmeniz onaylandı" : "Puantaj düzeltmeniz reddedildi",
            AppendNote($"{entity.WorkDate:dd.MM.yyyy} tarihli puantaj düzeltme talebiniz {(request.Approve ? "onaylandı" : "reddedildi")}.", request.Note), entity.Id));
        await db.SaveChangesAsync(ct); return true;
    }

    public async Task<PagedResultDto<FieldWorkRequestDto>> GetWorkLocationRequestsAsync(Guid managerId, WorkLocationRequestStatus? status, int page, int pageSize, CancellationToken ct = default)
    {
        var scope = await ScopeAsync(managerId, ct);
        var query = ScopeRequests(db.FieldWorkRequests.AsNoTracking().Include(x => x.User).Include(x => x.Days), scope);
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);
        var list = await query.OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
        return Page(list.Select(x => new FieldWorkRequestDto(x.Id, x.UserId, x.User.Name, x.LocationType, x.StartDate, x.EndDate,
            x.RecurrenceType, x.Days.Select(d => d.DayOfWeek).ToArray(), x.ProjectName, x.CustomerName, x.FieldAddress,
            x.Reason, x.Status, x.CreatedAt, x.ReviewNote)).ToArray(), page, pageSize);
    }

    public async Task<bool> ReviewWorkLocationRequestAsync(Guid id, Guid managerId, bool approve, string? note, string? correlationId, CancellationToken ct = default)
    {
        var scope = await ScopeAsync(managerId, ct);
        var entity = await ScopeRequests(db.FieldWorkRequests.AsNoTracking(), scope).SingleOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return false;
        if (entity.Status != WorkLocationRequestStatus.Pending) throw new InvalidOperationException("Bu talep daha önce sonuçlandırılmış.");
        if (!await workLocations.ReviewFieldRequestAsync(id, managerId, approve, note, ct)) return false;
        await audit.RecordAsync(managerId, approve ? "WorkLocationRequest.Approved" : "WorkLocationRequest.Rejected", nameof(FieldWorkRequest), id.ToString(),
            new { entity.Status }, new { Status = approve ? WorkLocationRequestStatus.Approved : WorkLocationRequestStatus.Rejected, Note = Clean(note) }, correlationId, ct);
        await db.SaveChangesAsync(ct); return true;
    }

    public async Task<PagedResultDto<ManagerPersonnelStatusDto>> GetPersonnelStatusAsync(Guid managerId, Guid? workplaceId, Guid? departmentId, string? status, string? search, int page, int pageSize, CancellationToken ct = default)
    {
        var scope = await ScopeAsync(managerId, ct);
        EnsureScope(scope, workplaceId);
        var users = ScopedPersonnel(scope);
        if (workplaceId.HasValue) users = users.Where(x => x.WorkplaceId == workplaceId);
        if (departmentId.HasValue) users = users.Where(x => x.DepartmentId == departmentId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            users = users.Where(x => x.Name.ToLower().Contains(term) || x.EmployeeNumber.ToLower().Contains(term));
        }
        var personnel = await users.Include(x => x.Department).Include(x => x.Workplace).OrderBy(x => x.Name).ToListAsync(ct);
        var ids = personnel.Select(x => x.Id).ToArray();
        var today = DateOnly.FromDateTime(clock.GetLocalNow().DateTime);
        var report = await reports.GetAsync(today, today, cancellationToken: ct);
        var rows = report.Rows.Where(x => ids.Contains(x.EmployeeId)).ToDictionary(x => x.EmployeeId);
        var breaks = await db.BreakRecords.AsNoTracking().Where(x => ids.Contains(x.UserId) && x.EndedAt == null).ToDictionaryAsync(x => x.UserId, ct);
        var result = personnel.Select(x =>
        {
            rows.TryGetValue(x.Id, out var row); breaks.TryGetValue(x.Id, out var activeBreak);
            return new ManagerPersonnelStatusDto(x.Id, x.EmployeeNumber, x.Name, x.Department?.Name ?? x.DepartmentLegacy,
                x.Workplace?.Name, row?.Status ?? "NoRecord", row?.FirstEntry, row?.LastExit, row?.WorkLocation ?? "Office",
                activeBreak is not null, activeBreak?.StartedAt, row is null || IsMissing(row.Status));
        });
        if (!string.IsNullOrWhiteSpace(status)) result = result.Where(x => string.Equals(x.AttendanceStatus, status, StringComparison.OrdinalIgnoreCase));
        return Page(result.ToArray(), page, pageSize);
    }

    public async Task<ManagerAttendanceReportDto> GetAttendanceReportAsync(Guid managerId, DateOnly from, DateOnly to, Guid? workplaceId, Guid? departmentId, Guid? userId, int page, int pageSize, CancellationToken ct = default)
    {
        var report = await FilterReportAsync(managerId, from, to, workplaceId, departmentId, userId, ct);
        return new(from, to, Page(report.Rows, page, pageSize));
    }

    public async Task<AttendanceReportDto> GetAttendanceReportExportAsync(Guid managerId, DateOnly from, DateOnly to, Guid? workplaceId, Guid? departmentId, Guid? userId, string? correlationId, CancellationToken ct = default)
    {
        var report = await FilterReportAsync(managerId, from, to, workplaceId, departmentId, userId, ct);
        await audit.RecordAsync(managerId, "AttendanceReport.Exported", nameof(AttendanceReportDto), $"{from:yyyy-MM-dd}:{to:yyyy-MM-dd}", null,
            new { workplaceId, departmentId, userId, RowCount = report.Rows.Count }, correlationId, ct);
        await db.SaveChangesAsync(ct);
        return report;
    }

    private async Task<AttendanceReportDto> FilterReportAsync(Guid managerId, DateOnly from, DateOnly to, Guid? workplaceId, Guid? departmentId, Guid? userId, CancellationToken ct)
    {
        var scope = await ScopeAsync(managerId, ct); EnsureScope(scope, workplaceId);
        var users = ScopedPersonnel(scope);
        if (workplaceId.HasValue) users = users.Where(x => x.WorkplaceId == workplaceId);
        if (departmentId.HasValue) users = users.Where(x => x.DepartmentId == departmentId);
        if (userId.HasValue) users = users.Where(x => x.Id == userId);
        var allowed = await users.Select(x => x.Id).ToArrayAsync(ct);
        if (userId.HasValue && !allowed.Contains(userId.Value)) throw new UnauthorizedAccessException("Personel yetki kapsamı dışında.");
        var report = await reports.GetAsync(from, to, cancellationToken: ct);
        return report with { Rows = report.Rows.Where(x => allowed.Contains(x.EmployeeId)).ToArray(), Employees = null };
    }

    private async Task<Guid?> ScopeAsync(Guid managerId, CancellationToken ct) =>
        (await db.Users.AsNoTracking().Where(x => x.Id == managerId).Select(x => new { x.WorkplaceId }).SingleOrDefaultAsync(ct))?.WorkplaceId;
    private IQueryable<User> ScopedPersonnel(Guid? scope) => ScopeUsers(db.Users.AsNoTracking().Where(x => x.IsActive && x.AccountStatus == AccountStatus.Active && (x.Role == null || x.Role.Name != "Yonetici")), scope);
    private static IQueryable<User> ScopeUsers(IQueryable<User> query, Guid? scope) => scope.HasValue ? query.Where(x => x.WorkplaceId == scope || x.WorkplaceId == null) : query;
    private static IQueryable<LeaveRequest> ScopeRequests(IQueryable<LeaveRequest> query, Guid? scope) => scope.HasValue ? query.Where(x => x.User.WorkplaceId == scope) : query;
    private static IQueryable<AttendanceCorrectionRequest> ScopeRequests(IQueryable<AttendanceCorrectionRequest> query, Guid? scope) => scope.HasValue ? query.Where(x => x.User.WorkplaceId == scope) : query;
    private static IQueryable<FieldWorkRequest> ScopeRequests(IQueryable<FieldWorkRequest> query, Guid? scope) => scope.HasValue ? query.Where(x => x.User.WorkplaceId == scope) : query;
    private static void EnsureScope(Guid? scope, Guid? requested) { if (scope.HasValue && requested.HasValue && scope != requested) throw new UnauthorizedAccessException("İşyeri yetki kapsamı dışında."); }
    private static bool IsMissing(string status) => status is "NoRecord" or "MissingEntry" or "MissingExit";
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string AppendNote(string message, string? note) => string.IsNullOrWhiteSpace(note) ? message : $"{message} Not: {note.Trim()}";
    private Notification NewNotification(Guid userId, NotificationType type, string title, string message, Guid relatedId) => new()
        { UserId = userId, Type = type, Title = title, Message = message, RelatedEntityId = relatedId, CreatedAt = clock.GetUtcNow() };
    private async Task<string> GenerateEmployeeNumberAsync(CancellationToken ct)
    {
        var numbers = await db.Users.AsNoTracking().Where(x => x.EmployeeNumber.StartsWith("PER-")).Select(x => x.EmployeeNumber).ToListAsync(ct);
        var maximum = numbers.Select(x => int.TryParse(x.AsSpan(4), out var value) ? value : 0).DefaultIfEmpty().Max();
        return $"PER-{maximum + 1:0000}";
    }
    private static (int Page, int Size) NormalizePage(int page, int size) => (Math.Max(1, page), Math.Clamp(size, 1, 100));
    private static PagedResultDto<T> Page<T>(IReadOnlyList<T> values, int page, int pageSize)
    {
        var p = NormalizePage(page, pageSize);
        return new(values.Skip((p.Page - 1) * p.Size).Take(p.Size).ToArray(), p.Page, p.Size, values.Count);
    }
    private static async Task<PagedResultDto<T>> PageAsync<T>(IQueryable<T> query, int page, int pageSize, CancellationToken ct)
    {
        var p = NormalizePage(page, pageSize); var count = await query.CountAsync(ct);
        return new(await query.Skip((p.Page - 1) * p.Size).Take(p.Size).ToArrayAsync(ct), p.Page, p.Size, count);
    }
}
