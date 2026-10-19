using System.ComponentModel.DataAnnotations.Schema;

namespace webdulich.Models
{
    public class TourLoaiDiaHinh
    {
        public int TourId { get; set; }

        public int LoaiDiaHinhId { get; set; }

        [ForeignKey("TourId")]
        public Tour? Tour { get; set; }

        [ForeignKey("LoaiDiaHinhId")]
        public LoaiDiaHinh? LoaiDiaHinh { get; set; }
    }
}