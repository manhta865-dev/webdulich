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

    }
}