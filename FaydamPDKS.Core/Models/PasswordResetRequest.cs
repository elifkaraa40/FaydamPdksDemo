using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FaydamPDKS.Core.Enums;

namespace FaydamPDKS.Core.Models;

[Table("password_reset_requests")]
public sealed class PasswordResetRequest
{
    [Key, Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("user_id")]
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    [Column("channel")]
    public PasswordResetChannel Channel { get; set; }

    [Column("status")]
    public PasswordResetRequestStatus Status { get; set; }

    [MaxLength(128), Column("token_hash")]
    public string? TokenHash { get; set; }

    [Column("token_expires_at")]
    public DateTimeOffset? TokenExpiresAt { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [Column("reviewed_at")]
    public DateTimeOffset? ReviewedAt { get; set; }

    [Column("reviewed_by_user_id")]
    public Guid? ReviewedByUserId { get; set; }

    [MaxLength(500), Column("review_note")]
    public string? ReviewNote { get; set; }

    [Column("completed_at")]
    public DateTimeOffset? CompletedAt { get; set; }
}
