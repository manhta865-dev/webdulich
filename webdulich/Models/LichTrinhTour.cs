using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace webdulich.Models
{
    public class LichTrinhTour
    {
        [Key]
        public int Id { get; set; }

        public int TourId { get; set; }

        public int NgayThu { get; set; }

        [Required]
        public string TieuDe { get; set; } = string.Empty;

        public string? BuaAn { get; set; }

        public string? NoiDung { get; set; }

        [ForeignKey("TourId")]
        public Tour? Tour { get; set; }
    }
}