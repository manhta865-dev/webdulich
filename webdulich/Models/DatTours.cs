using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace webdulich.Models
{
    public class DatTours
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string TenKhach { get; set; } = string.Empty;

        public string DienThoai { get; set; } = string.Empty;

        public int SoNguoi { get; set; }

        public DateTime NgayDi { get; set; }

        public int TourId { get; set; }

        [ForeignKey("TourId")]
        public Tour? Tour { get; set; }
    }
}