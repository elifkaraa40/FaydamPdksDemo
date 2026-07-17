using FaydamPDKS.Core.Attendance;
using FaydamPDKS.Core.DTOs;
using FaydamPDKS.Core.Models;
using FaydamPDKS.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FaydamPDKS.Tests;

public sealed class AttendanceQrServiceTests
{
    [Fact]
    public async Task Scan_uses_server_side_event_type_and_zone()
    {
        await using var context = TestInfrastructure.CreateContext();
        var now = new DateTimeOffset(2026, 7, 16, 9, 0, 0, TimeSpan.Zero);
        var (workplace, zone, employee) = await SeedAsync(context);
        const string rawQr = "existing-entry-qr";
        context.AttendanceQrCodes.Add(new AttendanceQrCode
        {
            Id = Guid.NewGuid(), WorkplaceId = workplace.Id, ZoneId = zone.Id, Name = "Giriş",
            EventType = AttendanceEventType.Entry, TokenHash = AttendanceQrService.Hash(rawQr),
            IsActive = true, IsLegacy = true, CreatedAt = now
        });
        await context.SaveChangesAsync();
        var service = new AttendanceQrService(context, new TestTimeProvider(now));

        var result = await service.ScanAsync(employee.Id, new ScanAttendanceQrRequest(rawQr, now, "event-1"));

        Assert.NotNull(result);
        Assert.Equal("Entry", result.EventType);
        var log = await context.AccessLogs.SingleAsync();
        Assert.Equal("Giris", log.LogType);
        Assert.Equal(zone.Id, log.ZoneId);
        Assert.Equal("MobileQr", log.Source);
        Assert.Equal(now.UtcDateTime, log.LogDate);
    }

    [Fact]
    public async Task Exit_without_entry_is_recorded_for_missing_entry_review()
    {
        await using var context = TestInfrastructure.CreateContext();
        var now = new DateTimeOffset(2026, 7, 16, 15, 0, 0, TimeSpan.Zero);
        var (workplace, zone, employee) = await SeedAsync(context);
        const string rawQr = "exit-without-entry";
        context.AttendanceQrCodes.Add(new AttendanceQrCode
        {
            Id = Guid.NewGuid(), WorkplaceId = workplace.Id, ZoneId = zone.Id, Name = "Çıkış",
            EventType = AttendanceEventType.Exit, TokenHash = AttendanceQrService.Hash(rawQr),
            IsActive = true, CreatedAt = now
        });
        await context.SaveChangesAsync();

        var result = await new AttendanceQrService(context, new TestTimeProvider(now))
            .ScanAsync(employee.Id, new ScanAttendanceQrRequest(rawQr, now.AddDays(-3), "exit-event"));

        Assert.NotNull(result);
        Assert.Equal("Cikis", (await context.AccessLogs.SingleAsync()).LogType);
        Assert.Equal(now, result.OccurredAt);
    }

    [Fact]
    public async Task Same_transition_cannot_be_scanned_twice_in_a_row()
    {
        await using var context = TestInfrastructure.CreateContext();
        var now = new DateTimeOffset(2026, 7, 16, 9, 0, 0, TimeSpan.Zero);
        var (workplace, zone, employee) = await SeedAsync(context);
        const string rawQr = "entry-qr";
        context.AttendanceQrCodes.Add(new AttendanceQrCode
        {
            Id = Guid.NewGuid(), WorkplaceId = workplace.Id, ZoneId = zone.Id, Name = "Giriş",
            EventType = AttendanceEventType.Entry, TokenHash = AttendanceQrService.Hash(rawQr),
            IsActive = true, CreatedAt = now
        });
        await context.SaveChangesAsync();
        var service = new AttendanceQrService(context, new TestTimeProvider(now));
        await service.ScanAsync(employee.Id, new ScanAttendanceQrRequest(rawQr, now, "entry-1"));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ScanAsync(employee.Id, new ScanAttendanceQrRequest(rawQr, now, "entry-2")));

        Assert.Equal("DUPLICATE_TRANSITION", error.Message);
        Assert.Single(context.AccessLogs);
    }

    [Fact]
    public async Task Exit_scan_auto_closes_active_break()
    {
        await using var context = TestInfrastructure.CreateContext();
        var now = new DateTimeOffset(2026, 7, 16, 15, 0, 0, TimeSpan.Zero);
        var (workplace, zone, employee) = await SeedAsync(context);
        const string rawQr = "auto-close-exit";
        context.AccessLogs.Add(new AccessLog { UserId = employee.Id, ZoneId = zone.Id, LogType = "Giris", LogDate = now.AddHours(-7).UtcDateTime });
        context.BreakRecords.Add(new BreakRecord { UserId = employee.Id, StartedAt = now.AddMinutes(-15), StartDeviceEventId = "break-start" });
        context.AttendanceQrCodes.Add(new AttendanceQrCode
        {
            Id = Guid.NewGuid(), WorkplaceId = workplace.Id, ZoneId = zone.Id, Name = "Çıkış",
            EventType = AttendanceEventType.Exit, TokenHash = AttendanceQrService.Hash(rawQr), IsActive = true, CreatedAt = now
        });
        await context.SaveChangesAsync();

        await new AttendanceQrService(context, new TestTimeProvider(now))
            .ScanAsync(employee.Id, new ScanAttendanceQrRequest(rawQr, now, "exit-auto-close"));

        var breakRecord = await context.BreakRecords.SingleAsync();
        Assert.Equal(now, breakRecord.EndedAt);
        Assert.True(breakRecord.AutoClosed);
    }

    [Fact]
    public async Task Rotate_revokes_old_qr_and_only_new_value_scans()
    {
        await using var context = TestInfrastructure.CreateContext();
        var now = new DateTimeOffset(2026, 7, 16, 9, 0, 0, TimeSpan.Zero);
        var (workplace, zone, employee) = await SeedAsync(context);
        var service = new AttendanceQrService(context, new TestTimeProvider(now));
        var first = await service.CreateAsync(new CreateAttendanceQrDto
        {
            WorkplaceId = workplace.Id, ZoneId = zone.Id, Name = "Merkez Çıkış", EventType = AttendanceEventType.Exit
        });

        var rotated = await service.RotateAsync(first.Id);

        Assert.NotNull(rotated);
        Assert.Null(await service.ScanAsync(employee.Id, new ScanAttendanceQrRequest(first.RawValue, now, "old-event")));
        var scanned = await service.ScanAsync(employee.Id, new ScanAttendanceQrRequest(rotated.RawValue, now, "new-event"));
        Assert.NotNull(scanned);
        Assert.Equal("Exit", scanned.EventType);
        Assert.Equal("Cikis", (await context.AccessLogs.SingleAsync()).LogType);
    }

    private static async Task<(Workplace Workplace, Zone Zone, User Employee)> SeedAsync(FaydamPDKS.Data.AppDbContext context)
    {
        var role = new Role { Id = Guid.NewGuid(), Name = "Personel", NormalizedName = "PERSONEL" };
        var workplace = new Workplace { Id = Guid.NewGuid(), Code = "MERKEZ", Name = "Merkez", TimeZoneId = "Europe/Istanbul", IsActive = true };
        var zone = new Zone { Name = "Ana Giriş", WorkplaceId = workplace.Id, Workplace = workplace, IsActive = true };
        var employee = new User { Id = Guid.NewGuid(), Name = "Test Personel", Email = "test@faydam.com", EmployeeNumber = "T-1", PasswordHash = "x", RoleId = role.Id, Role = role, IsActive = true };
        context.AddRange(role, workplace, zone, employee);
        await context.SaveChangesAsync();
        return (workplace, zone, employee);
    }
}
