using FaydamPDKS.Core.DTOs;
using FaydamPDKS.Core.Interfaces;
using FaydamPDKS.Core.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace FaydamPDKS.Data;

public sealed class AttendanceTerminalService(AppDbContext context, TimeProvider timeProvider) : IAttendanceTerminalService
{
    public async Task<TerminalPageDto> GetPageAsync(CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        var terminals = await context.AttendanceTerminals.AsNoTracking().Include(x => x.Workplace).OrderBy(x => x.Name).ToListAsync(cancellationToken);
        var items = terminals.Select(x => new TerminalListItemDto(x.Id, x.Name, x.SerialNumber, x.Workplace.Name,
            x.LastSeenAt, x.FirmwareVersion, x.PendingEventCount, x.LastError, x.IsActive,
            x.IsActive && x.LastSeenAt.HasValue && now - x.LastSeenAt.Value <= TimeSpan.FromMinutes(5))).ToArray();
        var workplaces = await context.Workplaces.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name)
            .Select(x => new WorkplaceOptionDto(x.Id, x.Code, x.Name)).ToArrayAsync(cancellationToken);
        return new(items, workplaces);
    }

    public async Task<RegisteredTerminalDto> RegisterAsync(RegisterTerminalDto request, CancellationToken cancellationToken = default)
    {
        if (!await context.Workplaces.AnyAsync(x => x.Id == request.WorkplaceId && x.IsActive, cancellationToken))
            throw new InvalidOperationException("Aktif işyeri bulunamadı.");
        var serial = request.SerialNumber.Trim().ToUpperInvariant();
        if (await context.AttendanceTerminals.AnyAsync(x => x.SerialNumber == serial, cancellationToken))
            throw new InvalidOperationException("Bu seri numarasıyla terminal zaten kayıtlı.");
        var apiKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var terminal = new AttendanceTerminal
        {
            Id = Guid.NewGuid(), WorkplaceId = request.WorkplaceId, Name = request.Name.Trim(), SerialNumber = serial,
            ApiKeyHash = Hash(apiKey), IsActive = true, CreatedAt = timeProvider.GetUtcNow()
        };
        context.AttendanceTerminals.Add(terminal);
        await context.SaveChangesAsync(cancellationToken);
        return new(terminal.Id, apiKey);
    }

    public async Task<bool> HeartbeatAsync(Guid terminalId, string apiKey, TerminalHeartbeatDto request, CancellationToken cancellationToken = default)
    {
        var terminal = await context.AttendanceTerminals.SingleOrDefaultAsync(x => x.Id == terminalId && x.IsActive, cancellationToken);
        if (terminal is null || !CryptographicOperations.FixedTimeEquals(Convert.FromHexString(terminal.ApiKeyHash), Convert.FromHexString(Hash(apiKey)))) return false;
        terminal.LastSeenAt = timeProvider.GetUtcNow();
        terminal.FirmwareVersion = Clean(request.FirmwareVersion);
        terminal.PendingEventCount = request.PendingEventCount;
        terminal.LastError = Clean(request.LastError);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<RegisteredTerminalDto?> RotateKeyAsync(Guid terminalId, CancellationToken cancellationToken = default)
    {
        var terminal = await context.AttendanceTerminals.SingleOrDefaultAsync(x => x.Id == terminalId && x.IsActive, cancellationToken);
        if (terminal is null) return null;
        var apiKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        terminal.ApiKeyHash = Hash(apiKey);
        await context.SaveChangesAsync(cancellationToken);
        return new(terminal.Id, apiKey);
    }

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
