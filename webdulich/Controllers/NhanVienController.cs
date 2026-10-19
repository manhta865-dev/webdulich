using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webdulich.Models;

namespace webdulich.Controllers
{
    public class NhanVienController : Controller
    {
        private readonly DuLichDbContext _context;

        public NhanVienController(DuLichDbContext context)
        {
            _context = context;
        }
        //          Action này chạy khi nhân viên vào link:
        //NhanVien/LichLamViec
        public IActionResult LichLamViec()
        {
            // 3. Lấy thông tin đăng nhập từ Session
            var role = HttpContext.Session.GetString("role");
            var taiKhoanId = HttpContext.Session.GetInt32("taiKhoanId");
            // KTR ĐĂNG NHẬP CHƯA 
            if (!taiKhoanId.HasValue)
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }
            // Chặn người không phải nhân viên
            if (role != "huongdanvien" && role != "taixe")
            {
                return RedirectToAction("Index", "TrangChu");
            }
            // Chặn người không phải nhân viên
            var nhanVien = _context.NhanViens
                .FirstOrDefault(x => x.TaiKhoanId == taiKhoanId.Value);
            // K THẤY QUAY VE DANG NHAP 
            if (nhanVien == null)
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }
            // TRUY VẤN PHAN CONG TOUR
            var query = _context.PhanCongTours
                .Include(x => x.LichKhoiHanhTour)
                    .ThenInclude(x => x.Tour)
                        .ThenInclude(x => x.DiaDiemInfo)// LẤY LỊCH KHOI HÀNH VÀ TOUR 
                .Include(x => x.LichKhoiHanhTour)
                    .ThenInclude(x => x.Tour)
                        .ThenInclude(x => x.PhuongTienInfo) // LẤY PHƯƠNG TIỆN 
                .Include(x => x.HuongDanVien) // LẤY HDV
                .Include(x => x.TaiXe) // LẤY TX
                .AsQueryable();
            // Nếu role là huongdanvien, chỉ lấy các tour mà nhân viên đó được phân công làm hướng dẫn viên.
            if (role == "huongdanvien")
            {
                query = query.Where(x => x.HuongDanVienId == nhanVien.Id);
            }
            else
            {
                query = query.Where(x => x.TaiXeId == nhanVien.Id);
            }
            // GƯIR THÔNG TIN SANG VIEW
            ViewBag.NhanVien = nhanVien;
            // 17. Trả danh sách lịch làm việc ra View
            return View(query
                .OrderBy(x => x.LichKhoiHanhTour!.NgayKhoiHanh)
                .ToList());
        }
    }
}