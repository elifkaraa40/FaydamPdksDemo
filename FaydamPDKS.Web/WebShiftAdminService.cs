using FaydamPDKS.Core.DTOs;
using FaydamPDKS.Core.Interfaces;
using FaydamPDKS.Core.Models;

namespace FaydamPDKS.Web;

public sealed class WebShiftAdminService(
    IShiftAdminRepository shifts,
    IUserRepository users,
    IUnitOfWork unitOfWork) : IShiftAdminService
{
    public async Task<ShiftAdminPageDto> GetPageAsync(CancellationToken cancellationToken = default)
    {
        var shiftItems = (await shifts.GetShiftsAsync(cancellationToken)).Select(x => new ShiftListItemDto(
            x.Id, x.Name, x.StartsAt, x.EndsAt, x.BreakMinutes, x.LateToleranceMinutes,
            x.EarlyLeaveToleranceMinutes, x.IsActive)).ToArray();
        var assignmentItems = (await shifts.GetAssignmentsAsync(cancellationToken)).Select(x => new ShiftAssignmentListItemDto(
            x.Id, x.EmployeeId, x.Employee?.Name ?? "Bilinmeyen personel", x.Employee?.EmployeeNumber ?? "—",
            x.Shift?.Name ?? "Bilinmeyen vardiya", x.ValidFrom, x.ValidTo)).ToArray();
        var employees = (await users.GetAllWithRoleAsync(cancellationToken)).Where(x => x.IsActive)
            .Select(x => new EmployeeOptionDto(x.Id, x.EmployeeNumber, x.Name)).ToArray();
        return new ShiftAdminPageDto(shiftItems, assignmentItems, employees);
    }

    public async Task CreateShiftAsync(CreateShiftDto request, CancellationToken cancellationToken = default)
    {
        var name = request.Name.Trim();
        if (request.StartsAt == request.EndsAt)
            throw new InvalidOperationException("Vardiya başlangıç ve bitiş saati aynı olamaz.");
        var duration = request.EndsAt > request.StartsAt
            ? (request.EndsAt - request.StartsAt).TotalMinutes
            : (TimeSpan.FromDays(1) - (request.StartsAt - request.EndsAt)).TotalMinutes;
        if (request.BreakMinutes >= duration)
            throw new InvalidOperationException("Mola süresi toplam vardiya süresinden kısa olmalıdır.");
        if (await shifts.NameExistsAsync(name, cancellationToken))
            throw new InvalidOperationException("Bu isimde bir vardiya zaten bulunuyor.");

        await shifts.AddShiftAsync(new Shift
        {
            Id = Guid.NewGuid(), Name = name, StartsAt = request.StartsAt, EndsAt = request.EndsAt,
            BreakMinutes = request.BreakMinutes, LateToleranceMinutes = request.LateToleranceMinutes,
            EarlyLeaveToleranceMinutes = request.EarlyLeaveToleranceMinutes, IsActive = true
        }, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task AssignAsync(CreateShiftAssignmentDto request, CancellationToken cancellationToken = default)
    {
        if (request.ValidTo.HasValue && request.ValidTo.Value < request.ValidFrom)
            throw new InvalidOperationException("Bitiş tarihi başlangıç tarihinden önce olamaz.");
        var employee = await users.GetByIdWithRoleAsync(request.EmployeeId, false, cancellationToken);
        if (employee is null || !employee.IsActive)
            throw new InvalidOperationException("Aktif personel bulunamadı.");
        if (!await shifts.ActiveShiftExistsAsync(request.ShiftId, cancellationToken))
            throw new InvalidOperationException("Aktif vardiya bulunamadı.");
        if (await shifts.HasOverlapAsync(request.EmployeeId, request.ValidFrom, request.ValidTo, cancellationToken))
            throw new InvalidOperationException("Personelin seçilen tarih aralığında başka bir vardiya ataması var.");

        await shifts.AddAssignmentAsync(new EmployeeShiftAssignment
        {
            Id = Guid.NewGuid(), EmployeeId = request.EmployeeId, ShiftId = request.ShiftId,
            ValidFrom = request.ValidFrom, ValidTo = request.ValidTo
        }, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
