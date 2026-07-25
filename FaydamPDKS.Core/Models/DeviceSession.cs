using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FaydamPDKS.Core.Models;

[Table("device_sessions")]
public sealed class DeviceSession
{
    [Key, Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("user_id")]
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    [Required, MaxLength(64), Column("device_id_hash")]
    public string DeviceIdHash { get; set; } = string.Empty;

    [Required, MaxLength(150), Column("device_name")]
    public string DeviceName { get; set; } = string.Empty;

    [Column("logged_in_at")]
    public DateTimeOffset LoggedInAt { get; set; }

    [Column("last_active_at")]
    public DateTimeOffset LastActiveAt { get; set; }

    [Column("revoked_at")]
    public DateTimeOffset? RevokedAt { get; set; }

    [MaxLength(80), Column("revoke_reason")]
    public string? RevokeReason { get; set; }

    [MaxLength(2048), Column("push_token")]
    public string? PushToken { get; set; }

    [MaxLength(20), Column("push_platform")]
    public string? PushPlatform { get; set; }

    [MaxLength(5), Column("push_language")]
    public string? PushLanguage { get; set; }

    [Column("push_token_updated_at")]
    public DateTimeOffset? PushTokenUpdatedAt { get; set; }

    [Column("push_token_disabled_at")]
    public DateTimeOffset? PushTokenDisabledAt { get; set; }

    public bool IsActive => RevokedAt is null;
}
