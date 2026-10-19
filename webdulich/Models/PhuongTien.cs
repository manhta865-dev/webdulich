using System.ComponentModel.DataAnnotations;

namespace webdulich.Models
{
    public class PhuongTien
    {
        [Key]
        public int Id { get; set; }

        public string TenPhuongTien { get; set; } = string.Empty;

        public ICollection<Tour> Tours { get; set; } = new List<Tour>();
    }
}