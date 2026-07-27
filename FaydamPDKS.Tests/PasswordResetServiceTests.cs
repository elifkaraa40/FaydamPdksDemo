using FaydamPDKS.Core.Enums;
using FaydamPDKS.Core.Models;
using FaydamPDKS.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FaydamPDKS.Tests;

public sealed class PasswordResetServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 27, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Manager_request_is_created_once_and_visible_only_in_manager_scope()
    {
        await using var db = TestInfrastructure.CreateContext();
        var workplaceA = Guid.NewGuid();
        var workplaceB = Guid.NewGuid();
        var managerA = User("manager-a@faydam.com", "Yönetici A", ManagerRole(), workplaceA);
        var managerB = User("manager-b@faydam.com", "Yönetici B", ManagerRole(), workplaceB);
        var employee = User("personel@faydam.com", "Personel", EmployeeRole(), workplaceA);
        db.AddRange(managerA.Role!, managerB.Role!, employee.Role!, managerA, managerB, employee);
        await db.SaveChangesAsync();
        var service = Service(db);

        await service.RequestManagerResetAsync(employee.Email);
        await service.RequestManagerResetAsync(employee.Email);

        Assert.Single(await db.PasswordResetRequests.ToListAsync());
        Assert.Single(await service.GetPendingManagerRequestsAsync(managerA.Id));
        Assert.Empty(await service.GetPendingManagerRequestsAsync(managerB.Id));
        Assert.Contains(await db.Notifications.ToListAsync(), x => x.UserId == managerA.Id
            && x.Type == NotificationType.PasswordResetRequested);
    }

    [Fact]
    public async Task Manager_approval_creates_temporary_password_and_revokes_existing_access()
    {
        await using var db = TestInfrastructure.CreateContext();
        var workplace = Guid.NewGuid();
        var manager = User("manager@faydam.com", "Yönetici", ManagerRole(), workplace);
        var employee = User("personel@faydam.com", "Personel", EmployeeRole(), workplace);
        var session = new DeviceSession
        {
            UserId = employee.Id, DeviceIdHash = "device", DeviceName = "Telefon",
            LoggedInAt = Now, LastActiveAt = Now
        };
        var refreshToken = new RefreshToken
        {
            UserId = employee.Id, DeviceSessionId = session.Id, TokenHash = "refresh",
            CreatedAt = Now, ExpiresAt = Now.AddDays(30)
        };
        db.AddRange(manager.Role!, employee.Role!, manager, employee, session, refreshToken);
        await db.SaveChangesAsync();
        var service = Service(db);
        await service.RequestManagerResetAsync(employee.Email);
        var request = await db.PasswordResetRequests.SingleAsync();

        var result = await service.ReviewManagerRequestAsync(request.Id, manager.Id, true, "Kimlik doğrulandı");

        Assert.True(result.Found);
        Assert.NotNull(result.TemporaryPassword);
        Assert.True(BCrypt.Net.BCrypt.Verify(result.TemporaryPassword, employee.PasswordHash));
        Assert.True(employee.MustChangePassword);
        Assert.Equal(PasswordResetRequestStatus.Approved, request.Status);
        Assert.NotNull(session.RevokedAt);
        Assert.NotNull(refreshToken.RevokedAt);
        Assert.DoesNotContain(result.TemporaryPassword!, string.Join(' ', await db.AuditLogs.Select(x => x.NewValuesJson).ToListAsync()));
    }

    [Fact]
    public async Task Email_token_is_single_use_and_revokes_existing_access()
    {
        await using var db = TestInfrastructure.CreateContext();
        var employee = User("personel@faydam.com", "Personel", EmployeeRole(), null);
        var session = new DeviceSession
        {
            UserId = employee.Id, DeviceIdHash = "device", DeviceName = "Telefon",
            LoggedInAt = Now, LastActiveAt = Now
        };
        db.AddRange(employee.Role!, employee, session);
        await db.SaveChangesAsync();
        var service = Service(db);
        var ticket = await service.CreateEmailResetAsync(employee.Email);

        Assert.NotNull(ticket);
        Assert.True(await service.ResetWithTokenAsync(ticket!.RawToken, "YeniParola123!"));
        Assert.False(await service.ResetWithTokenAsync(ticket.RawToken, "BaskaParola123!"));
        Assert.True(BCrypt.Net.BCrypt.Verify("YeniParola123!", employee.PasswordHash));
        Assert.NotNull(session.RevokedAt);
        Assert.Equal(PasswordResetRequestStatus.Completed, (await db.PasswordResetRequests.SingleAsync()).Status);
    }

    [Fact]
    public async Task Expired_email_token_is_rejected()
    {
        await using var db = TestInfrastructure.CreateContext();
        var employee = User("personel@faydam.com", "Personel", EmployeeRole(), null);
        db.AddRange(employee.Role!, employee);
        await db.SaveChangesAsync();
        var service = Service(db);
        var ticket = await service.CreateEmailResetAsync(employee.Email);
        var request = await db.PasswordResetRequests.SingleAsync();
        request.TokenExpiresAt = Now.AddSeconds(-1);
        await db.SaveChangesAsync();

        Assert.False(await service.ResetWithTokenAsync(ticket!.RawToken, "YeniParola123!"));
        Assert.Equal(PasswordResetRequestStatus.Expired, request.Status);
        Assert.True(BCrypt.Net.BCrypt.Verify("EskiParola123!", employee.PasswordHash));
    }

    private static PasswordResetService Service(AppDbContext db) =>
        new(db, new AuditTrail(db, new TestTimeProvider(Now)), new TestTimeProvider(Now));

    private static User User(string email, string name, Role role, Guid? workplaceId) => new()
    {
        Id = Guid.NewGuid(), Email = email, Name = name, EmployeeNumber = Guid.NewGuid().ToString("N"),
        Role = role, RoleId = role.Id, WorkplaceId = workplaceId, IsActive = true,
        AccountStatus = AccountStatus.Active, PasswordHash = BCrypt.Net.BCrypt.HashPassword("EskiParola123!")
    };

    private static Role ManagerRole() => new() { Id = Guid.NewGuid(), Name = "Yonetici", NormalizedName = "YONETICI" };
    private static Role EmployeeRole() => new() { Id = Guid.NewGuid(), Name = $"Personel-{Guid.NewGuid():N}", NormalizedName = "PERSONEL" };
}
