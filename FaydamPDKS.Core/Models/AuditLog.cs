using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FaydamPDKS.Core.Models;

[Table("audit_logs")]
public sealed class AuditLog
{
    [Key, Column("id")] public Guid Id { get; set; }
    [Column("actor_user_id")] public Guid? ActorUserId { get; set; }
    public User? ActorUser { get; set; }
    [Required, StringLength(80), Column("action")] public string Action { get; set; } = string.Empty;
    [Required, StringLength(100), Column("entity_type")] public string EntityType { get; set; } = string.Empty;
    [Required, StringLength(100), Column("entity_id")] public string EntityId { get; set; } = string.Empty;
    [Column("old_values_json", TypeName = "jsonb")] public string? OldValuesJson { get; set; }
    [Column("new_values_json", TypeName = "jsonb")] public string? NewValuesJson { get; set; }
    [Column("occurred_at")] public DateTimeOffset OccurredAt { get; set; }
    [StringLength(100), Column("correlation_id")] public string? CorrelationId { get; set; }
}
