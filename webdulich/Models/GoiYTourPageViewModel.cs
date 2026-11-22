using Microsoft.AspNetCore.Mvc.Rendering;

namespace webdulich.Models
{
    public class GoiYTourPageViewModel
    {
        public TieuChiGoiYViewModel TieuChi { get; set; } = new TieuChiGoiYViewModel();

        public List<TourGoiYItemViewModel> KetQua { get; set; } = new List<TourGoiYItemViewModel>();

        public List<SelectListItem> DiaDiemOptions { get; set; } = new List<SelectListItem>();

        public List<SelectListItem> LoaiDiaHinhOptions { get; set; } = new List<SelectListItem>();

        public List<SelectListItem> PhongCachOptions { get; set; } = new List<SelectListItem>();

        public List<SelectListItem> SoNgayOptions { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> LoaiTourOptions { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> PhuongTienOptions { get; set; } = new List<SelectListItem>();

        // Gợi ý bằng AI (Gemini): khách mô tả chuyến đi bằng lời, AI chọn trong các tour đang bán
        public bool CoAi { get; set; }
        public string? MoTaAi { get; set; }
        public string? TomTatAi { get; set; }
        public string? LoiAi { get; set; }
        public List<TourGoiYItemViewModel> KetQuaAi { get; set; } = new List<TourGoiYItemViewModel>();
    }
}