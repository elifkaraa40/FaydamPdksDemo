using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FaydamPDKS.Core.Models
{
    [Table("zones")]
    public class Zone
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("workplace_id")]
        public Guid? WorkplaceId { get; set; }

        public Workplace? Workplace { get; set; }

        [Column("name")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty; // Örn: "Ana Giriş Turnikesi", "Focus Odası Kapısı"

        [Column("is_active")]
        public bool IsActive { get; set; } = true;
    }
}
