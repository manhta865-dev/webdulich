using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webdulich.Models;

namespace webdulich.Controllers
{
    // Điểm tham quan của từng ngày lịch trình (Mo23, RB-49, RB-50). Giao diện nằm trong trang Admin/LichTrinh,
    // mọi thao tác xong đều quay về trang đó. Gỡ điểm khỏi ngày chỉ xóa dòng nối, điểm vẫn giữ để ngày khác dùng lại.
    public class AdminDiemThamQuanController : Controller
    {
        private readonly DuLichDbContext _context;

        public AdminDiemThamQuanController(DuLichDbContext context)
        {
            _context = context;
        }

        private bool LaAdmin() => HttpContext.Session.GetString("role") == "admin";

        private const string ToaDoSai = "Tọa độ chưa đúng. Hãy dán dạng vĩ độ, kinh độ như 11.9404, 108.4583 (lấy từ Google Maps), điểm phải ở trong Việt Nam.";

        private IActionResult VeLichTrinh(int tourId) => RedirectToAction("LichTrinh", "Admin", new { id = tourId });

        // Thêm điểm vào một ngày: chọn điểm có sẵn, hoặc nhập tên và tọa độ để tạo điểm mới trong điểm đến của tour
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Them(int lichTrinhId, int? diemCoSanId, string? ten, string? toaDo)
        {
            if (!LaAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            var ngay = _context.LichTrinhTours.Include(x => x.Tour).Include(x => x.DsDiemThamQuan).FirstOrDefault(x => x.Id == lichTrinhId);
            if (ngay?.Tour == null)
            {
                return NotFound();
            }

            DiemThamQuan? diem;
            if (diemCoSanId.HasValue)
            {
                diem = _context.DiemThamQuans.FirstOrDefault(x => x.Id == diemCoSanId.Value);
                if (diem == null)
                {
                    TempData["error"] = "Không tìm thấy điểm tham quan đã chọn.";
                    return VeLichTrinh(ngay.TourId);
                }
            }
            else
            {
                if (ngay.Tour.DiaDiemId == null)
                {
                    TempData["error"] = "Tour chưa chọn địa điểm nên chưa thêm được điểm tham quan. Hãy chọn địa điểm ở trang sửa tour.";
                    return VeLichTrinh(ngay.TourId);
                }
                ten = ten?.Trim();
                if (string.IsNullOrEmpty(ten) || ten.Length > 150)
                {
                    TempData["error"] = "Nhập tên điểm tham quan (tối đa 150 ký tự), hoặc chọn một điểm có sẵn.";
                    return VeLichTrinh(ngay.TourId);
                }
                if (!LichTrinhTour.DocToaDo(toaDo, out var vi, out var kinh) || !vi.HasValue || !kinh.HasValue)
                {
                    TempData["error"] = ToaDoSai;
                    return VeLichTrinh(ngay.TourId);
                }

                // Cùng điểm đến đã có điểm trùng tên thì dùng lại điểm đó (UNIQUE DiaDiemId, Ten), không tạo bản sao
                diem = _context.DiemThamQuans.FirstOrDefault(x => x.DiaDiemId == ngay.Tour.DiaDiemId && x.Ten == ten);
                if (diem == null)
                {
                    diem = new DiemThamQuan { DiaDiemId = ngay.Tour.DiaDiemId.Value, Ten = ten, ViDo = vi.Value, KinhDo = kinh.Value };
                    _context.DiemThamQuans.Add(diem);
                }
            }

            if (diem.Id != 0 && ngay.DsDiemThamQuan.Any(x => x.DiemThamQuanId == diem.Id))
            {
                TempData["error"] = $"Ngày {ngay.NgayThu} đã có điểm \"{diem.Ten}\".";
                return VeLichTrinh(ngay.TourId);
            }

            ngay.DsDiemThamQuan.Add(new LichTrinhDiemThamQuan
            {
                DiemThamQuan = diem,
                ThuTu = ngay.DsDiemThamQuan.Count == 0 ? 1 : ngay.DsDiemThamQuan.Max(x => x.ThuTu) + 1
            });
            _context.SaveChanges();

            TempData["success"] = $"Đã thêm \"{diem.Ten}\" vào ngày {ngay.NgayThu}.";
            return VeLichTrinh(ngay.TourId);
        }

        // Gỡ điểm khỏi một ngày rồi đánh số lại thứ tự 1, 2, 3...
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Go(int lichTrinhId, int diemId)
        {
            if (!LaAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            var ngay = _context.LichTrinhTours.Include(x => x.DsDiemThamQuan).FirstOrDefault(x => x.Id == lichTrinhId);
            var noi = ngay?.DsDiemThamQuan.FirstOrDefault(x => x.DiemThamQuanId == diemId);
            if (ngay == null || noi == null)
            {
                return NotFound();
            }

            ngay.DsDiemThamQuan.Remove(noi);
            _context.SaveChanges();
            DanhSoLai(ngay.DsDiemThamQuan.OrderBy(x => x.ThuTu).ToList());

            TempData["success"] = $"Đã gỡ điểm khỏi ngày {ngay.NgayThu}.";
            return VeLichTrinh(ngay.TourId);
        }

        // Đưa điểm lên trước (huong = -1) hoặc ra sau (huong = 1) một bậc trong ngày
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DoiThuTu(int lichTrinhId, int diemId, int huong)
        {
            if (!LaAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            var ngay = _context.LichTrinhTours.Include(x => x.DsDiemThamQuan).FirstOrDefault(x => x.Id == lichTrinhId);
            if (ngay == null)
            {
                return NotFound();
            }

            var ds = ngay.DsDiemThamQuan.OrderBy(x => x.ThuTu).ToList();
            int i = ds.FindIndex(x => x.DiemThamQuanId == diemId), j = i + Math.Sign(huong);
            if (i >= 0 && j >= 0 && j < ds.Count)
            {
                (ds[i], ds[j]) = (ds[j], ds[i]);
                DanhSoLai(ds);
            }
            return VeLichTrinh(ngay.TourId);
        }

        // Sửa tên hoặc tọa độ của một điểm; mọi ngày đang dùng điểm này đều thấy thay đổi
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Sua(int id, int tourId, string? ten, string? toaDo)
        {
            if (!LaAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            var diem = _context.DiemThamQuans.FirstOrDefault(x => x.Id == id);
            if (diem == null)
            {
                return NotFound();
            }

            ten = ten?.Trim();
            if (string.IsNullOrEmpty(ten) || ten.Length > 150)
            {
                TempData["error"] = "Tên điểm tham quan không được trống, tối đa 150 ký tự.";
                return VeLichTrinh(tourId);
            }
            if (!LichTrinhTour.DocToaDo(toaDo, out var vi, out var kinh) || !vi.HasValue || !kinh.HasValue)
            {
                TempData["error"] = ToaDoSai;
                return VeLichTrinh(tourId);
            }
            if (_context.DiemThamQuans.Any(x => x.Id != id && x.DiaDiemId == diem.DiaDiemId && x.Ten == ten))
            {
                TempData["error"] = $"Điểm đến này đã có điểm tên \"{ten}\".";
                return VeLichTrinh(tourId);
            }

            diem.Ten = ten;
            diem.ViDo = vi.Value;
            diem.KinhDo = kinh.Value;
            _context.SaveChanges();

            TempData["success"] = $"Đã lưu điểm \"{ten}\".";
            return VeLichTrinh(tourId);
        }

        // Đánh số 1..n theo thứ tự danh sách. Làm hai lượt vì UNIQUE (LichTrinhTourId, ThuTu):
        // lượt đầu đẩy hết ra xa để không trùng với số đang có, lượt sau mới đặt số thật.
        private void DanhSoLai(List<LichTrinhDiemThamQuan> ds)
        {
            if (ds.Count == 0)
            {
                return;
            }
            int lech = ds.Max(x => x.ThuTu) + ds.Count + 1;
            for (int k = 0; k < ds.Count; k++)
            {
                ds[k].ThuTu = lech + k;
            }
            _context.SaveChanges();
            for (int k = 0; k < ds.Count; k++)
            {
                ds[k].ThuTu = k + 1;
            }
            _context.SaveChanges();
        }
    }
}
