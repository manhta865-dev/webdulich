using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace webdulich.Models
{
    public class TieuChiGoiYViewModel
    {
        [Display(Name = "Ngân sách tối đa")]
        public decimal? NganSachToiDa { get; set; }

        [Display(Name = "Địa điểm cụ thể")]
        public int? DiaDiemId { get; set; }

        [Display(Name = "Bạn thích đi đâu?")]
        public string? LoaiDiaHinh { get; set; }

        [Display(Name = "Bạn muốn đi kiểu gì?")]
        public string? PhongCach { get; set; }

        [Display(Name = "Bạn muốn di chuyển bằng phương tiện gì?")]
        public string? PhuongTien { get; set; }

        [Display(Name = "Bạn muốn đi trong bao nhiêu ngày?")]
        public int? SoNgay { get; set; }

        [Display(Name = "Ưu tiên tour hot")]
        public bool UuTienTourHot { get; set; }
        public string? LoaiTour { get; set; }
    }
}