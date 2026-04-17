using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using webdulich.Models;

namespace webdulich.Controllers
{
    public class GoiYTourController : Controller
    {
        private readonly DuLichDbContext _context;

        public GoiYTourController(DuLichDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var model = new GoiYTourPageViewModel();
            NapDuLieuLuaChon(model);
            model.KetQua = LayDanhSachGoiY(model.TieuChi);
            return View(model);
        }

        [HttpPost]
        public IActionResult Index(GoiYTourPageViewModel model)
        {
            NapDuLieuLuaChon(model);
            model.KetQua = LayDanhSachGoiY(model.TieuChi);
            return View(model);
        }

        private void NapDuLieuLuaChon(GoiYTourPageViewModel model)
        {
            model.DiaDiemOptions = _context.DiaDiems
                .Select(dd => new SelectListItem
                {
                    Value = dd.MaDiaDiem.ToString(),
                    Text = dd.TenDiaDiem
                })
                .ToList();

            model.LoaiDiaHinhOptions = new List<SelectListItem>
            {
                new SelectListItem { Value = "Núi", Text = "Núi" },
                new SelectListItem { Value = "Biển", Text = "Biển" },
                new SelectListItem { Value = "Đồng bằng", Text = "Đồng bằng" },
                new SelectListItem { Value = "Thành phố", Text = "Thành phố" }
            };

            model.PhongCachOptions = new List<SelectListItem>
            {
                new SelectListItem { Value = "Cặp đôi", Text = "Cặp đôi" },
                new SelectListItem { Value = "Gia đình", Text = "Gia đình" },
                new SelectListItem { Value = "Nghỉ dưỡng", Text = "Nghỉ dưỡng" },
                new SelectListItem { Value = "Khám phá", Text = "Khám phá" },
                new SelectListItem { Value = "Phượt", Text = "Phượt" }
            };

            model.SoNgayOptions = new List<SelectListItem>
            {
                new SelectListItem { Value = "1", Text = "1 ngày" },
                new SelectListItem { Value = "2", Text = "2 ngày" },
                new SelectListItem { Value = "3", Text = "3 ngày" },
                new SelectListItem { Value = "4", Text = "4 ngày" },
                new SelectListItem { Value = "5", Text = "5 ngày trở lên" }
            };
        }

        private List<TourGoiYItemViewModel> LayDanhSachGoiY(TieuChiGoiYViewModel tieuChi)
        {
            var tours = _context.Tours.ToList();
            var diaDiems = _context.DiaDiems.ToList();
            var datTours = _context.DatTours.ToList();

            var bookingDict = datTours
                .Where(x => x.TourId > 0)
                .GroupBy(x => x.TourId)
                .ToDictionary(g => g.Key, g => g.Count());

            var diaDiemDict = diaDiems
                .ToDictionary(x => x.MaDiaDiem, x => x.TenDiaDiem);

            bool coLoc =
                tieuChi.NganSachToiDa.HasValue ||
                tieuChi.DiaDiemId.HasValue ||
                !string.IsNullOrEmpty(tieuChi.LoaiDiaHinh) ||
                !string.IsNullOrEmpty(tieuChi.PhongCach) ||
                tieuChi.SoNgay.HasValue ||
                tieuChi.UuTienTourHot;

            var ketQua = new List<TourGoiYItemViewModel>();

            foreach (var tour in tours)
            {
                int diem = 0;
                List<string> lyDo = new List<string>();

                int soLuotDat = bookingDict.ContainsKey(tour.Id) ? bookingDict[tour.Id] : 0;

                string tenDiaDiemHienThi = tour.DiaDiem;
                if (tour.DiaDiemId.HasValue && diaDiemDict.ContainsKey(tour.DiaDiemId.Value))
                {
                    tenDiaDiemHienThi = diaDiemDict[tour.DiaDiemId.Value];
                }

                if (!coLoc)
                {
                    diem += soLuotDat * 3;

                    if (soLuotDat > 0)
                    {
                        lyDo.Add("Được nhiều khách lựa chọn");
                    }

                    if (tour.Gia <= 3000000)
                    {
                        diem += 2;
                        lyDo.Add("Mức giá dễ tiếp cận");
                    }
                }
                else
                {
                    if (tieuChi.DiaDiemId.HasValue && tour.DiaDiemId == tieuChi.DiaDiemId.Value)
                    {
                        diem += 6;
                        lyDo.Add("Đúng địa điểm bạn chọn");
                    }

                    if (!string.IsNullOrEmpty(tieuChi.LoaiDiaHinh) &&
                        !string.IsNullOrEmpty(tour.LoaiDiaHinh) &&
                        tour.LoaiDiaHinh.Trim().ToLower() == tieuChi.LoaiDiaHinh.Trim().ToLower())
                    {
                        diem += 5;
                        lyDo.Add("Đúng kiểu địa hình bạn muốn");
                    }

                    if (!string.IsNullOrEmpty(tieuChi.PhongCach) &&
                        !string.IsNullOrEmpty(tour.PhongCach) &&
                        tour.PhongCach.Trim().ToLower() == tieuChi.PhongCach.Trim().ToLower())
                    {
                        diem += 5;
                        lyDo.Add("Đúng phong cách du lịch bạn thích");
                    }

                    if (tieuChi.NganSachToiDa.HasValue)
                    {
                        if (tour.Gia <= tieuChi.NganSachToiDa.Value)
                        {
                            diem += 5;
                            lyDo.Add("Phù hợp ngân sách");

                            decimal chenhLech = tieuChi.NganSachToiDa.Value - tour.Gia;
                            if (chenhLech <= 1000000)
                            {
                                diem += 1;
                                lyDo.Add("Giá khá sát nhu cầu của bạn");
                            }
                        }
                        else
                        {
                            diem -= 3;
                        }
                    }

                    if (tieuChi.SoNgay.HasValue && tour.SoNgay.HasValue)
                    {
                        if (tour.SoNgay.Value == tieuChi.SoNgay.Value)
                        {
                            diem += 4;
                            lyDo.Add("Khớp đúng số ngày bạn muốn");
                        }
                        else if (tour.SoNgay.Value <= tieuChi.SoNgay.Value)
                        {
                            diem += 2;
                            lyDo.Add("Thời gian tour phù hợp");
                        }
                    }

                    if (tieuChi.UuTienTourHot)
                    {
                        diem += Math.Min(soLuotDat, 5);

                        if (soLuotDat > 0)
                        {
                            lyDo.Add($"Tour hot với {soLuotDat} lượt đặt");
                        }
                    }
                    else
                    {
                        if (soLuotDat > 0)
                        {
                            diem += Math.Min(soLuotDat, 3);
                            lyDo.Add($"Có {soLuotDat} lượt đặt");
                        }
                    }
                }

                if (diem > 0)
                {
                    ketQua.Add(new TourGoiYItemViewModel
                    {
                        Tour = tour,
                        DiemGoiY = diem,
                        SoLuotDat = soLuotDat,
                        LyDoGoiY = string.Join(", ", lyDo),
                        TenDiaDiemHienThi = tenDiaDiemHienThi
                    });
                }
            }

            return ketQua
                .OrderByDescending(x => x.DiemGoiY)
                .ThenByDescending(x => x.SoLuotDat)
                .ThenBy(x => x.Tour.Gia)
                .Take(9)
                .ToList();
        }
    }
}