using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace webdulich.Models
{
    public class Tour
    {
        [Key]
        public int Id { get; set; }

        public string TenTour { get; set; } = string.Empty;

        public string DiaDiem { get; set; } = string.Empty;

        public decimal Gia { get; set; }

        public string? HinhAnh { get; set; }

        public string? MoTa { get; set; }

        public int? DiaDiemId { get; set; }

        public int? DanhMucId { get; set; }

        public string? LoaiDiaHinh { get; set; }

        public string? PhongCach { get; set; }

        public int? SoNgay { get; set; }

        [NotMapped]
        public IFormFile? HinhFile { get; set; }
    }
}