using System.ComponentModel.DataAnnotations;

namespace webdulich.Models
{
    public class LoaiDiaHinh
    {
        [Key]
        public int Id { get; set; }

        public string TenLoaiDiaHinh { get; set; } = string.Empty;

        public ICollection<TourLoaiDiaHinh> TourLoaiDiaHinhs { get; set; } = new List<TourLoaiDiaHinh>();
    }
}