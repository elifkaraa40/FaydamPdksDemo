using FaydamPDKS.Api.Controllers;
using FaydamPDKS.Core.DTOs;
using FaydamPDKS.Core.Enums;
using FaydamPDKS.Core.Models;
using FaydamPDKS.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace FaydamPDKS.Tests;

public sealed class ManagerMobileServiceTests
{
    [Fact]
    public async Task Pending_registration_can_only_be_reviewed_once_and_creates_audit_and_notification()
    {
        await using var db = TestInfrastructure.CreateContext();
        var managerRole = new Role { Id = Guid.NewGuid(), Name = "Yonetici", NormalizedName = "YONETICI" };
        var personnelRole = new Role { Id = Guid.NewGuid(), Name = "Personel", NormalizedName = "PERSONEL" };
        var manager = new User { Id = Guid.NewGuid(), Name = "Yönetici", Email = "manager@test.local", EmployeeNumber = "YON-1",
            RoleId = managerRole.Id, Role = managerRole, AccountStatus = AccountStatus.Active, IsActive = true };
        var personnel = new User { Id = Guid.NewGuid(), Name = "Yeni Personel", Email = "personnel@test.local", EmployeeNumber = string.Empty,
            RoleId = personnelRole.Id, Role = personnelRole, AccountStatus = AccountStatus.PendingApproval, IsActive = true };
        db.AddRange(managerRole, personnelRole, manager, personnel);
        await db.SaveChangesAsync();

        var clock = new TestTimeProvider(new DateTimeOffset(2026, 7, 22, 9, 0, 0, TimeSpan.Zero));
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Features:WorkLocations"] = "true", ["Attendance:TimeZone"] = "Europe/Istanbul"
        }).Build();
        var workLocations = new WorkLocationService(db, configuration, clock,
            new ManagerNotificationService(db, clock), new NotificationRepository(db));
        var service = new ManagerMobileService(db, new AttendanceReportService(db, configuration), new AuditTrail(db, clock),
            workLocations, new WorkCalendarResolver(db), clock);

        Assert.True(await service.ReviewRegistrationAsync(personnel.Id, manager.Id,
            new ReviewRegistrationDto { Approve = true }, "test-correlation"));
        var saved = await db.Users.FindAsync(personnel.Id);
        Assert.Equal(AccountStatus.Active, saved!.AccountStatus);
        Assert.StartsWith("PER-", saved.EmployeeNumber);
        Assert.Contains(db.Notifications, x => x.UserId == personnel.Id && x.Type == NotificationType.RegistrationApproved);
        Assert.Contains(db.AuditLogs, x => x.ActorUserId == manager.Id && x.EntityId == personnel.Id.ToString()
            && x.CorrelationId == "test-correlation" && x.OldValuesJson != null && x.NewValuesJson != null);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ReviewRegistrationAsync(personnel.Id, manager.Id,
            new ReviewRegistrationDto { Approve = false }, "second-review"));
    }

    [Fact]
    public void Manager_and_active_colleague_controllers_declare_role_authorization()
    {
        var manager = typeof(ManagerMobileController).GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>().Single();
        var colleagues = typeof(MobileBreaksController).GetMethod(nameof(MobileBreaksController.ActiveColleagues))!
            .GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>().Single();
        Assert.Equal("Yonetici", manager.Roles);
        Assert.Equal("Personel", colleagues.Roles);
    }
}
