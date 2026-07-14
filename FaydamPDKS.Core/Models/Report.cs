using System.ComponentModel.DataAnnotations;

namespace FaydamPDKS.Core.Models
{
    public class Report
    {
        [Key] // Bu satırı eklediğinden emin ol
        public int Id { get; set; }

        // Diğer alanların...
        public string Title { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
