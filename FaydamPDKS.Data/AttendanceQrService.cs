using FaydamPDKS.Core.Attendance;
using FaydamPDKS.Core.DTOs;
using FaydamPDKS.Core.Enums;
using FaydamPDKS.Core.Interfaces;
using FaydamPDKS.Core.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace FaydamPDKS.Data;

public sealed class AttendanceQrService(AppDbContext context, TimeProvider timeProvider, IManagerNotificationService? managerNotifications = null) : IAttendanceQrService
{
    public async Task<AttendanceQrPageDto> GetPageAsync(CancellationToken cancellationToken = default)
    {
        var qrCodes = await context.AttendanceQrCodes.AsNoTracking().Include(x => x.Workplace).Include(x => x.Zone)
            .OrderByDescending(x => x.IsActive).ThenBy(x => x.Workplace.Name).ThenBy(x => x.Name)
            .Select(x => new AttendanceQrListItemDto(x.Id, x.Name, x.Workplace.Name, x.Zone.Name, x.EventType,
                x.IsActive, x.IsLegacy, x.CreatedAt, x.RevokedAt)).ToArrayAsync(cancellationToken);
        var transitionRows = await (from log in context.AccessLogs.AsNoTracking()
            join user in context.Users.AsNoTracking() on log.UserId equals user.Id
            join zone in context.Zones.AsNoTracking() on log.ZoneId equals zone.Id
            orderby log.LogDate descending
            select new { user.Name, user.EmployeeNumber, ZoneName = zone.Name, log.LogType, log.LogDate, log.Source })
            .Take(100).ToArrayAsync(cancellationToken);
        var transitions = transitionRows.Select(x => new AttendanceTransitionDto(x.Name, x.EmployeeNumber, x.ZoneName,
            x.LogType, new DateTimeOffset(DateTime.SpecifyKind(x.LogDate, DateTimeKind.Utc)), x.Source)).ToArray();
        var workplaces = await context.Workplaces.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name)
            .Select(x => new WorkplaceOptionDto(x.Id, x.Code, x.Name)).ToArrayAsync(cancellationToken);
        var zones = await context.Zones.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name)
            .Select(x => new ZoneOptionDto(x.Id, x.Name)).ToArrayAsync(cancellationToken);
        return new(qrCodes, transitions, workplaces, zones);
    }

    public async Task<GeneratedAttendanceQrDto> CreateAsync(CreateAttendanceQrDto request, CancellationToken cancellationToken = default)
    {
        if (!await context.Workplaces.AnyAsync(x => x.Id == request.WorkplaceId && x.IsActive, cancellationToken))
            throw new InvalidOperationException("Aktif işyeri bulunamadı.");
        if (!await context.Zones.AnyAsync(x => x.Id == request.ZoneId && x.IsActive, cancellationToken))
            throw new InvalidOperationException("Aktif giriş-çıkış bölgesi bulunamadı.");
        return await GenerateAsync(request.WorkplaceId, request.ZoneId, request.Name.Trim(), request.EventType, null, cancellationToken);
    }

    public async Task<GeneratedAttendanceQrDto?> RotateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var current = await context.AttendanceQrCodes.SingleOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken);
        if (current is null) return null;
        return await GenerateAsync(current.WorkplaceId, current.ZoneId, current.Name, current.EventType, current, cancellationToken);
    }

    public async Task<bool> DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var current = await context.AttendanceQrCodes.SingleOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken);
        if (current is null) return false;
        current.IsActive = false;
        current.RevokedAt = timeProvider.GetUtcNow();
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<ScanAttendanceQrResponse?> ScanAsync(Guid employeeId, ScanAttendanceQrRequest request, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        var deviceEventId = request.DeviceEventId.Trim();
        if (await context.AccessLogs.AsNoTracking().AnyAsync(x => x.DeviceEventId == deviceEventId, cancellationToken))
            throw new InvalidOperationException("DUPLICATE_EVENT");
        var hash = Hash(request.QrValue.Trim());
        var qr = await context.AttendanceQrCodes.AsNoTracking().Include(x => x.Workplace).Include(x => x.Zone)
            .SingleOrDefaultAsync(x => x.TokenHash == hash && x.IsActive, cancellationToken);
        if (qr is null) return null;

        var timeZone = ResolveTimeZone(qr.Workplace.TimeZoneId);
        var localNow = TimeZoneInfo.ConvertTime(now, timeZone);
        var localDayStart = DateOnly.FromDateTime(localNow.DateTime)
            .ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        var dayStartUtc = TimeZoneInfo.ConvertTimeToUtc(localDayStart, timeZone);
        var lastEventType = await context.AccessLogs.AsNoTracking()
            .Where(x => x.UserId == employeeId && x.LogDate >= dayStartUtc)
            .OrderByDescending(x => x.LogDate)
            .Select(x => x.LogType)
            .FirstOrDefaultAsync(cancellationToken);
        var requestedType = qr.EventType == AttendanceEventType.Entry ? "Giris" : "Cikis";
        if (string.Equals(lastEventType, requestedType, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("DUPLICATE_TRANSITION");

        if (qr.EventType == AttendanceEventType.Exit && lastEventType is null && managerNotifications is not null)
        {
            var employeeName = await context.Users.AsNoTracking().Where(x => x.Id == employeeId)
                .Select(x => x.Name).SingleOrDefaultAsync(cancellationToken) ?? "Personel";
            await managerNotifications.NotifyAsync(NotificationType.MissingAttendanceDetected, "Eksik giriş kaydı",
                $"{employeeName} çıkış yaptı ancak bugüne ait giriş kaydı bulunamadı.", null, cancellationToken);
        }

        if (qr.EventType == AttendanceEventType.Exit)
        {
            var activeBreak = await context.BreakRecords
                .SingleOrDefaultAsync(x => x.UserId == employeeId && x.EndedAt == null, cancellationToken);
            if (activeBreak is not null)
            {
                activeBreak.EndedAt = now;
                activeBreak.AutoClosed = true;
            }
        }

        context.AccessLogs.Add(new AccessLog
        {
            UserId = employeeId,
            ZoneId = qr.ZoneId,
            LogDate = now.UtcDateTime,
            LogType = requestedType,
            DeviceEventId = deviceEventId,
            Source = "MobileQr"
        });
        await context.SaveChangesAsync(cancellationToken);
        return new(qr.EventType.ToString(), qr.Workplace.Name, qr.Zone.Name, now);
    }

    private async Task<GeneratedAttendanceQrDto> GenerateAsync(Guid workplaceId, int zoneId, string name,
        AttendanceEventType eventType, AttendanceQrCode? replaced, CancellationToken cancellationToken)
    {
        var rawValue = $"faydam://attendance/scan?token={Base64Url(RandomNumberGenerator.GetBytes(32))}";
        var created = new AttendanceQrCode
        {
            Id = Guid.NewGuid(), WorkplaceId = workplaceId, ZoneId = zoneId, Name = name,
            EventType = eventType, TokenHash = Hash(rawValue), IsActive = true, IsLegacy = false,
            CreatedAt = timeProvider.GetUtcNow()
        };
        var otherActive = await context.AttendanceQrCodes
            .Where(x => x.WorkplaceId == workplaceId && x.ZoneId == zoneId && x.EventType == eventType && x.IsActive)
            .ToListAsync(cancellationToken);
        foreach (var item in otherActive)
        {
            item.IsActive = false;
            item.RevokedAt = created.CreatedAt;
            item.ReplacedById = created.Id;
        }
        if (replaced is not null && !otherActive.Contains(replaced))
        {
            replaced.IsActive = false;
            replaced.RevokedAt = created.CreatedAt;
            replaced.ReplacedById = created.Id;
        }
        context.AttendanceQrCodes.Add(created);
        await context.SaveChangesAsync(cancellationToken);
        return new(created.Id, created.Name, rawValue, created.EventType);
    }

    public static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    private static string Base64Url(byte[] value) => Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static TimeZoneInfo ResolveTimeZone(string? id)
    {
        var requested = string.IsNullOrWhiteSpace(id) ? "Europe/Istanbul" : id;
        try { return TimeZoneInfo.FindSystemTimeZoneById(requested); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time"); }
    }
}
