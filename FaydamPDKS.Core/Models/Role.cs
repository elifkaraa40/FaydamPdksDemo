using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FaydamPDKS.Core.Models
{
    [Table("roles")]

    public class Role
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Required]
        [Column("name")]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [Column("normalized_name")]
        [StringLength(50)]
        public string NormalizedName { get; set; } = string.Empty;

        [Column("description")]
        [StringLength(200)]
        public string? Description { get; set; } //rolun açıklaması
        public ICollection<User>? Users { get; set; }
    }
}
