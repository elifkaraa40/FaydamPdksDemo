using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FaydamPDKS.Core.Models;

[Table("departments")]
public sealed class Department
{
    [Key, Column("id")] public Guid Id { get; set; }
    [Column("workplace_id")] public Guid WorkplaceId { get; set; }
    public Workplace Workplace { get; set; } = null!;
    [Required, StringLength(30), Column("code")] public string Code { get; set; } = string.Empty;
    [Required, StringLength(120), Column("name")] public string Name { get; set; } = string.Empty;
    [Column("is_active")] public bool IsActive { get; set; } = true;
}
