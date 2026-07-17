using System.Security.Claims;
using FaydamPDKS.Core.Enums;
using FaydamPDKS.Core.Interfaces;
using FaydamPDKS.Core.Models;
using FaydamPDKS.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FaydamPDKS.Web.Controllers;

[Authorize(Roles = "Yonetici")]
public sealed class RegistrationsController(AppDbContext context, IAuditTrail auditTrail, TimeProvider timeProvider) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var model = new RegistrationApprovalPage(
            await context.Users.AsNoTracking().Where(x => x.AccountStatus == AccountStatus.PendingApproval)
                .OrderBy(x => x.Name).ToListAsync(cancellationToken),
            await context.Departments.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name).ToListAsync(cancellationToken));
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Approve(Guid id, string employeeNumber, Guid? departmentId, CancellationToken cancellationToken)
    {
        var user = await context.Users.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (user is null) return NotFound();
        employeeNumber = employeeNumber?.Trim() ?? string.Empty;
        if (employeeNumber.Length < 2 || await context.Users.AnyAsync(x => x.Id != id && x.EmployeeNumber == employeeNumber, cancellationToken))
        {
            TempData["Error"] = "Geçerli ve benzersiz bir personel numarası girin.";
            return RedirectToAction(nameof(Index));
        }
        Department? department = null;
        if (departmentId.HasValue)
        {
            department = await context.Departments.SingleOrDefaultAsync(x => x.Id == departmentId && x.IsActive, cancellationToken);
            if (department is null) { TempData["Error"] = "Departman bulunamadı."; return RedirectToAction(nameof(Index)); }
        }
        var oldStatus = user.AccountStatus;
        user.EmployeeNumber = employeeNumber;
        user.DepartmentId = department?.Id;
        user.WorkplaceId = department?.WorkplaceId;
        user.AccountStatus = AccountStatus.Active;
        user.IsActive = true;
        context.Notifications.Add(NewNotification(user.Id, NotificationType.RegistrationApproved, "Hesabınız onaylandı", "PDKS hesabınız kullanıma açıldı."));
        await auditTrail.RecordAsync(GetActorId(), "Registration.Approved", nameof(User), user.Id.ToString(), new { Status = oldStatus }, new { user.AccountStatus, user.EmployeeNumber, user.DepartmentId }, HttpContext.TraceIdentifier, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        TempData["Success"] = $"{user.Name} onaylandı.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Reject(Guid id, CancellationToken cancellationToken)
    {
        var user = await context.Users.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (user is null) return NotFound();
        user.AccountStatus = AccountStatus.Rejected;
        user.IsActive = false;
        context.Notifications.Add(NewNotification(user.Id, NotificationType.RegistrationRejected, "Kaydınız onaylanmadı", "Detay için işyerinizle iletişime geçin."));
        await auditTrail.RecordAsync(GetActorId(), "Registration.Rejected", nameof(User), user.Id.ToString(), null, new { user.AccountStatus }, HttpContext.TraceIdentifier, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        TempData["Success"] = "Kayıt reddedildi.";
        return RedirectToAction(nameof(Index));
    }

    private Notification NewNotification(Guid userId, NotificationType type, string title, string message) =>
        new() { UserId = userId, Type = type, Title = title, Message = message, CreatedAt = timeProvider.GetUtcNow() };
    private Guid? GetActorId() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}

public sealed record RegistrationApprovalPage(IReadOnlyList<User> Users, IReadOnlyList<Department> Departments);
