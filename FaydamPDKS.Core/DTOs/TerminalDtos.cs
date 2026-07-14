using System.ComponentModel.DataAnnotations;

namespace FaydamPDKS.Core.DTOs;

public sealed class RegisterTerminalDto
{
    [Required] public Guid WorkplaceId { get; set; }
    [Required, StringLength(100)] public string Name { get; set; } = string.Empty;
    [Required, StringLength(100)] public string SerialNumber { get; set; } = string.Empty;
}
public sealed record RegisteredTerminalDto(Guid Id, string ApiKey);
public sealed class TerminalHeartbeatDto
{
    [StringLength(80)] public string? FirmwareVersion { get; set; }
    [Range(0, 1_000_000)] public int PendingEventCount { get; set; }
    [StringLength(500)] public string? LastError { get; set; }
}
public sealed record TerminalListItemDto(Guid Id, string Name, string SerialNumber, string WorkplaceName, DateTimeOffset? LastSeenAt, string? FirmwareVersion, int PendingEventCount, string? LastError, bool IsActive, bool IsOnline);
public sealed record TerminalPageDto(IReadOnlyList<TerminalListItemDto> Terminals, IReadOnlyList<WorkplaceOptionDto> Workplaces);
