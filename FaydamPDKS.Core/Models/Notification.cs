using FaydamPDKS.Core.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FaydamPDKS.Core.Models;

[Table("notifications")]
public sealed class Notification
{
    [Key, Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("user_id")]
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    [Column("type")]
    public NotificationType Type { get; set; }

    [Column("title"), Required, StringLength(150)]
    public string Title { get; set; } = string.Empty;

    [Column("message"), Required, StringLength(1000)]
    public string Message { get; set; } = string.Empty;

    [Column("related_entity_id")]
    public Guid? RelatedEntityId { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [Column("read_at")]
    public DateTimeOffset? ReadAt { get; set; }

    public bool IsRead => ReadAt.HasValue;
}
