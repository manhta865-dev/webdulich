using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace webdulich.Models
{
    public class TourHinhAnh
    {
        public int Id { get; set; }

        public int TourId { get; set; }

        [Required]
        public string TenFile { get; set; } = string.Empty;

        public int ThuTu { get; set; }

        [ForeignKey("TourId")]
        public Tour? Tour { get; set; }
    }
}