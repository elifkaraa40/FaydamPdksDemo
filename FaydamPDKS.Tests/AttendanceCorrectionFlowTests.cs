using FaydamPDKS.Api;
using FaydamPDKS.Core.DTOs;
using FaydamPDKS.Core.Enums;
using FaydamPDKS.Core.Models;
using FaydamPDKS.Data;
using FaydamPDKS.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace FaydamPDKS.Tests;

public sealed class AttendanceCorrectionFlowTests
{
    [Fact]
    public async Task Approved_mobile_request_overrides_daily_attendance_and_creates_audit_notification()
    {
        await using var context = TestInfrastructure.CreateContext();
        var employee = new User { Id = Guid.NewGuid(), EmployeeNumber = "PER-0300", Name = "Personel", Email = "personel@faydam.com", IsActive = true };
        var reviewer = new User { Id = Guid.NewGuid(), EmployeeNumber = "YON-0300", Name = "Yönetici", Email = "yonetici@faydam.com", IsActive = true };
        context.AddRange(employee, reviewer);
        await context.SaveChangesAsync();
        var time = new TestTimeProvider(new DateTimeOffset(2026, 7, 14, 10, 0, 0, TimeSpan.Zero));
        var config = Configuration();
        var repository = new AttendanceCorrectionRepository(context);
        var unitOfWork = new UnitOfWork(context);
        var mobile = new MobileAttendanceCorrectionService(repository, unitOfWork, time, config);

        var created = await mobile.CreateAsync(employee.Id, new CreateAttendanceCorrectionDto
        {
            WorkDate = new DateOnly(2026, 7, 14), RequestedEntry = new TimeOnly(9, 0),
            RequestedExit = new TimeOnly(18, 0), Reason = "Terminal çıkışımı kaydetmedi."
        });
        await Assert.ThrowsAsync<InvalidOperationException>(() => mobile.CreateAsync(employee.Id, new CreateAttendanceCorrectionDto
        {
            WorkDate = created.WorkDate, RequestedEntry = new TimeOnly(9, 5), RequestedExit = new TimeOnly(18, 0), Reason = "Aynı güne ikinci talep."
        }));

        var review = new WebAttendanceCorrectionService(repository, new NotificationRepository(context), new AuditTrail(context, time), unitOfWork, time);
        Assert.True(await review.ReviewAsync(created.Id, reviewer.Id, new ReviewAttendanceCorrectionDto(true, "Terminal kaydı doğrulandı."), "test-trace"));

        var attendance = new MobileAttendanceService(new AccessLogRepository(context), new ShiftResolver(context), repository, new WorkCalendarResolver(context), new BreakService(context, time), unitOfWork, config, time);
        var summary = await attendance.GetTodayAsync(employee.Id);
        Assert.Equal("Complete", summary.Status);
        Assert.Equal(480, summary.WorkedMinutes);
        Assert.Equal(AttendanceCorrectionStatus.Approved, (await context.AttendanceCorrectionRequests.SingleAsync()).Status);
        Assert.Equal(NotificationType.AttendanceCorrectionApproved, (await context.Notifications.SingleAsync()).Type);
        Assert.Equal("AttendanceCorrection.Approved", (await context.AuditLogs.SingleAsync()).Action);
    }

    [Fact]
    public async Task Manager_cannot_review_own_correction()
    {
        await using var context = TestInfrastructure.CreateContext();
        var user = new User { Id = Guid.NewGuid(), EmployeeNumber = "YON-1", Name = "Yönetici", Email = "y@f.com", IsActive = true };
        var correction = new AttendanceCorrectionRequest { Id = Guid.NewGuid(), UserId = user.Id, User = user, WorkDate = new DateOnly(2026, 7, 14), RequestedEntry = new TimeOnly(9, 0), RequestedExit = new TimeOnly(18, 0), Reason = "Test gerekçesi", CreatedAt = DateTimeOffset.UtcNow };
        context.AddRange(user, correction);
        await context.SaveChangesAsync();
        var time = new TestTimeProvider(DateTimeOffset.UtcNow);
        var service = new WebAttendanceCorrectionService(new AttendanceCorrectionRepository(context), new NotificationRepository(context), new AuditTrail(context, time), new UnitOfWork(context), time);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ReviewAsync(correction.Id, user.Id, new ReviewAttendanceCorrectionDto(true, null), null));
    }

    private static IConfiguration Configuration() => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["Attendance:TimeZone"] = "Europe/Istanbul", ["Attendance:DefaultShiftStart"] = "09:00",
        ["Attendance:DefaultShiftEnd"] = "18:00", ["Attendance:BreakMinutes"] = "60"
    }).Build();
}
