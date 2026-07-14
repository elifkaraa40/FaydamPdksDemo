using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FaydamPDKS.Core.Models
{
    [Table("access_logs")]
    public class AccessLog
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required(ErrorMessage = "Kullanıcı ID (UserId) boş bırakılamaz!")]
        [Column("user_id")]
        public Guid UserId { get; set; }

        [Required(ErrorMessage = "Geçiş noktası (ZoneId) zorunludur!")]
        [Column("zone_id")]
        public int ZoneId { get; set; }

        [Column("log_date")]
        public DateTime LogDate { get; set; } = DateTime.UtcNow; // Geçiş yapılan anlık tarih/saat

        [Column("device_event_id")]
        [StringLength(100)]
        public string? DeviceEventId { get; set; }

        [Column("source")]
        [StringLength(30)]
        public string Source { get; set; } = "Terminal";

        [Required(ErrorMessage = "Log tipi (Giriş veya Çıkış) boş bırakılamaz!")]
        [Column("log_type")]
        [StringLength(10)]
        public string LogType { get; set; } = string.Empty; // "Giris" veya "Cikis"
    }
}
