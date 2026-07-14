using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FaydamPDKS.Core.Models
{
    [Table("permissions")]
    public class Permission
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("User")]
        public Guid UserId { get; set; }
        public User? User { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        [StringLength(200)]
        public string Reason { get; set; } = string.Empty; // Örn: "Yıllık İzin", "Hastalık"

        public bool IsApproved { get; set; } = false; // Yöneticinin onayı
    }
}