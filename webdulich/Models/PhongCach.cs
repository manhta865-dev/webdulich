using System.ComponentModel.DataAnnotations;

namespace webdulich.Models
{
    public class PhongCach
    {
        [Key]
        public int Id { get; set; }

        public string TenPhongCach { get; set; } = string.Empty;

        public ICollection<TourPhongCach> TourPhongCachs { get; set; } = new List<TourPhongCach>();
    }
}