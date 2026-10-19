using System.ComponentModel.DataAnnotations.Schema;

namespace webdulich.Models
{
    public class TourPhongCach
    {
        public int TourId { get; set; }

        public int PhongCachId { get; set; }

        [ForeignKey("TourId")]
        public Tour? Tour { get; set; }

        [ForeignKey("PhongCachId")]
        public PhongCach? PhongCach { get; set; }
    }
}