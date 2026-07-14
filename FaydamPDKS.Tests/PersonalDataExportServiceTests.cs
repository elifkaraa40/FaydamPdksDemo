using FaydamPDKS.Core.Enums;
using FaydamPDKS.Core.Models;
using FaydamPDKS.Data;
using System.Text.Json;
using Xunit;

namespace FaydamPDKS.Tests;

public sealed class PersonalDataExportServiceTests
{
    [Fact]
    public async Task Exports_only_subject_data_without_credentials()
    {
        await using var context = TestInfrastructure.CreateContext();
        var user = new User { Id = Guid.NewGuid(), EmployeeNumber = "PER-0500", Name = "KVKK Test", Email = "kvkk@faydam.com", PasswordHash = "SECRET_PASSWORD_HASH", IsActive = true };
        var other = new User { Id = Guid.NewGuid(), EmployeeNumber = "PER-OTHER", Name = "Other", Email = "other@faydam.com", PasswordHash = "OTHER_SECRET", IsActive = true };
        context.AddRange(user, other,
            new AccessLog { UserId = user.Id, ZoneId = 1, LogDate = new DateTime(2026, 7, 14, 6, 0, 0, DateTimeKind.Utc), LogType = "Giris", Source = "Terminal" },
            new AccessLog { UserId = other.Id, ZoneId = 1, LogDate = new DateTime(2026, 7, 14, 6, 0, 0, DateTimeKind.Utc), LogType = "Giris", Source = "Terminal" },
            new LeaveRequest { Id = Guid.NewGuid(), UserId = user.Id, User = user, LeaveType = LeaveType.Annual, StartDate = new DateOnly(2026, 8, 1), EndDate = new DateOnly(2026, 8, 2), CreatedAt = DateTimeOffset.UtcNow },
            new Notification { Id = Guid.NewGuid(), UserId = user.Id, User = user, Type = NotificationType.Information, Title = "Bilgi", Message = "Mesaj", CreatedAt = DateTimeOffset.UtcNow });
        await context.SaveChangesAsync();

        var result = await new PersonalDataExportService(context, new TestTimeProvider(DateTimeOffset.UtcNow)).ExportAsync(user.Id);

        Assert.NotNull(result);
        Assert.Single(result.AttendanceEvents);
        Assert.Single(result.LeaveRequests);
        Assert.Single(result.Notifications);
        var json = JsonSerializer.Serialize(result);
        Assert.DoesNotContain("SECRET_PASSWORD_HASH", json);
        Assert.DoesNotContain("refreshToken", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(other.Email, json);
    }
}
