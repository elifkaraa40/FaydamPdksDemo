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

    [Fact]
    public async Task Dashboard_counts_real_current_states_and_filters_return_same_people()
    {
        await using var db = TestInfrastructure.CreateContext();
        var managerRole = new Role
        {
            Id = Guid.NewGuid(),
            Name = "Yonetici",
            NormalizedName = "YONETICI"
        };
        var personnelRole = new Role
        {
            Id = Guid.NewGuid(),
            Name = "Personel",
            NormalizedName = "PERSONEL"
        };
        var manager = ActiveUser("Yönetici", "YON-1", managerRole);
        var office = ActiveUser("Ofiste", "PER-1", personnelRole);
        var exited = ActiveUser("Çıkış Yaptı", "PER-2", personnelRole);
        var missing = ActiveUser("Eksik", "PER-3", personnelRole);
        var field = ActiveUser("Sahada", "PER-4", personnelRole);
        var now = new DateTimeOffset(2026, 7, 22, 12, 0, 0, TimeSpan.Zero);
        var today = new DateOnly(2026, 7, 22);
        db.AddRange(managerRole, personnelRole, manager, office, exited, missing, field);
        db.AccessLogs.AddRange(
            Log(office.Id, new DateTime(2026, 7, 22, 6, 0, 0, DateTimeKind.Utc), "Giris"),
            Log(exited.Id, new DateTime(2026, 7, 22, 6, 5, 0, DateTimeKind.Utc), "Giris"),
            Log(exited.Id, new DateTime(2026, 7, 22, 9, 0, 0, DateTimeKind.Utc), "Cikis"));
        db.WorkLocationAssignments.Add(new WorkLocationAssignment
        {
            UserId = field.Id,
            User = field,
            LocationType = WorkLocationType.Field,
            StartDate = today,
            EndDate = today,
            RecurrenceType = WorkLocationRecurrenceType.Once,
            CreatedByUserId = manager.Id,
            CreatedAt = now,
            IsActive = true
        });
        db.BreakRecords.Add(new BreakRecord
        {
            UserId = office.Id,
            StartedAt = now.AddMinutes(-10),
            StartDeviceEventId = "office-break"
        });
        await db.SaveChangesAsync();

        var configuration = Configuration();
        var service = Service(db, configuration, new TestTimeProvider(now));

        var dashboard = await service.GetDashboardAsync(manager.Id);
        var active = await service.GetPersonnelStatusAsync(
            manager.Id, null, null, "active", null, 1, 20);
        var officeList = await service.GetPersonnelStatusAsync(
            manager.Id, null, null, "office", null, 1, 20);
        var exitedList = await service.GetPersonnelStatusAsync(
            manager.Id, null, null, "exited", null, 1, 20);
        var missingList = await service.GetPersonnelStatusAsync(
            manager.Id, null, null, "missing", null, 1, 20);
        var breakList = await service.GetPersonnelStatusAsync(
            manager.Id, null, null, "break", null, 1, 20);

        Assert.Equal(4, dashboard.TotalPersonnel);
        Assert.Equal(2, dashboard.EnteredToday);
        Assert.Equal(1, dashboard.ExitedToday);
        Assert.Equal(1, dashboard.MissingAttendance);
        Assert.Equal(1, dashboard.OfficePersonnel);
        Assert.Equal(1, dashboard.FieldPersonnel);
        Assert.Equal(1, dashboard.PersonnelOnBreak);
        Assert.Equal(dashboard.EnteredToday, active.TotalCount);
        Assert.Equal(dashboard.OfficePersonnel, officeList.TotalCount);
        Assert.Equal(dashboard.ExitedToday, exitedList.TotalCount);
        Assert.Equal(dashboard.MissingAttendance, missingList.TotalCount);
        Assert.Equal(dashboard.PersonnelOnBreak, breakList.TotalCount);
        Assert.Contains(active.Items, x => x.FullName == "Ofiste");
        Assert.Contains(active.Items, x => x.FullName == "Sahada");
    }

    private static ManagerMobileService Service(
        AppDbContext db,
        IConfiguration configuration,
        TimeProvider clock)
    {
        var workLocations = new WorkLocationService(db, configuration, clock,
            new ManagerNotificationService(db, clock), new NotificationRepository(db));
        return new ManagerMobileService(
            db,
            new AttendanceReportService(db, configuration),
            new AuditTrail(db, clock),
            workLocations,
            new WorkCalendarResolver(db),
            clock,
            configuration);
    }

    private static IConfiguration Configuration() =>
        new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["Features:WorkLocations"] = "true",
                ["Attendance:TimeZone"] = "Europe/Istanbul",
                ["Attendance:DefaultShiftStart"] = "09:00",
                ["Attendance:DefaultShiftEnd"] = "18:00",
                ["Attendance:LateToleranceMinutes"] = "5",
                ["Attendance:EarlyLeaveToleranceMinutes"] = "5",
                ["Attendance:BreakMinutes"] = "60"
            }).Build();

    private static User ActiveUser(string name, string number, Role role) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Email = $"{number.ToLowerInvariant()}@test.local",
        EmployeeNumber = number,
        RoleId = role.Id,
        Role = role,
        AccountStatus = AccountStatus.Active,
        IsActive = true
    };

    private static AccessLog Log(Guid userId, DateTime at, string type) => new()
    {
        UserId = userId,
        ZoneId = 1,
        LogDate = at,
        LogType = type,
        Source = "MobileQr"
    };
}
