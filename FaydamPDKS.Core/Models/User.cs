using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FaydamPDKS.Core.Models
{
    [Table("users")] // PostgreSQL'deki tablo isminiz küçük harf ise bu şekilde belirtiyoruz
    public class User
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("name")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Column("email")]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Column("employee_number")]
        [StringLength(40)]
        public string EmployeeNumber { get; set; } = string.Empty;

        [Column("department")]
        [StringLength(100)]
        public string? DepartmentLegacy { get; set; }

        [Column("workplace_id")]
        public Guid? WorkplaceId { get; set; }
        public Workplace? Workplace { get; set; }

        [Column("department_id")]
        public Guid? DepartmentId { get; set; }
        public Department? Department { get; set; }

        [Column("hire_date")]
        public DateOnly? HireDate { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("role_id")]
        [ForeignKey("Role")]
        public Guid RoleId { get; set; }

        public Role? Role { get; set; }
        public string PasswordHash { get; set; } = string.Empty;

        // Profil Yönetimi İçin Yeni Alanlar
        public string? PhoneNumber { get; set; } // Bildirimler için
        public string? ProfileImageUrl { get; set; }
        public bool IsEmailNotificationEnabled { get; set; } = true;
        public bool IsSmsNotificationEnabled { get; set; } = false;
    }
}
