using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FaydamPDKS.Core.Models;

[Table("workplaces")]
public sealed class Workplace
{
    [Key, Column("id")] public Guid Id { get; set; }
    [Required, StringLength(30), Column("code")] public string Code { get; set; } = string.Empty;
    [Required, StringLength(120), Column("name")] public string Name { get; set; } = string.Empty;
    [Required, StringLength(100), Column("time_zone_id")] public string TimeZoneId { get; set; } = "Europe/Istanbul";
    [StringLength(250), Column("address")] public string? Address { get; set; }
    [Column("is_active")] public bool IsActive { get; set; } = true;
    public ICollection<Department> Departments { get; set; } = [];
}
