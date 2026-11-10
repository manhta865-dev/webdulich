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
                .Include(x => x.TaiKhoan)
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
                    .ThenInclude(x => x!.TaiKhoan)
                .Include(x => x.TaiXe) // LẤY TX
                    .ThenInclude(x => x!.TaiKhoan)
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

        // Danh sách hành khách của một chuyến (RB-41): chỉ hướng dẫn viên hoặc tài xế được phân công đúng chuyến đó
        // mới xem được. Hướng dẫn viên thấy 4 số cuối giấy tờ để làm thủ tục nhận phòng, vé tham quan; tài xế không thấy.
        public IActionResult DanhSachKhach(int id)
        {
            var role = HttpContext.Session.GetString("role");
            var taiKhoanId = HttpContext.Session.GetInt32("taiKhoanId");
            if (!taiKhoanId.HasValue)
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }
            if (role != "huongdanvien" && role != "taixe")
            {
                return RedirectToAction("Index", "TrangChu");
            }
            var nhanVien = _context.NhanViens.AsNoTracking().FirstOrDefault(x => x.TaiKhoanId == taiKhoanId.Value);
            if (nhanVien == null)
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            bool laHuongDanVien = role == "huongdanvien";
            bool duocPhanCong = _context.PhanCongTours.Any(p => p.LichKhoiHanhTourId == id
                && (laHuongDanVien ? p.HuongDanVienId == nhanVien.Id : p.TaiXeId == nhanVien.Id));
            if (!duocPhanCong)
            {
                // Không nói lịch có tồn tại hay không: người không được phân công không cần biết
                return NotFound();
            }

            var lich = _context.LichKhoiHanhTours.AsNoTracking()
                .Include(l => l.Tour)
                .Include(l => l.SoDoChoNgoi)
                .First(l => l.Id == id);

            // Ghế còn hiệu lực của các đơn chưa hủy trên chuyến
            var dsGhe = _context.HanhKhachDatTours.AsNoTracking()
                .Include(h => h.DatTour)
                .Where(h => h.NgayNha == null && h.DatTour!.LichKhoiHanhTourId == id && h.DatTour.TrangThai != "DaHuy")
                .ToList();
            var dsGheId = dsGhe.Select(h => h.Id).ToList();
            var dsDonId = dsGhe.Select(h => h.DatTourId).Distinct().ToList();

            var choTheoGhe = _context.ChoNgoiChuyenDis.AsNoTracking()
                .Where(c => c.LichKhoiHanhTourId == id)
                .Select(c => new { c.HanhKhachDatTourId, c.ViTriChoNgoi!.MaViTri, c.ViTriChoNgoi.Hang, c.ViTriChoNgoi.Cot })
                .ToList()
                .ToDictionary(c => c.HanhKhachDatTourId);

            // Thanh toán: đơn nhóm theo từng ghế, đơn cá nhân theo cả đơn (cùng cách tính với trang đơn và Lịch sử)
            var gdGhe = _context.GiaoDichThanhToans.AsNoTracking()
                .Where(g => g.HanhKhachDatTourId != null && dsGheId.Contains(g.HanhKhachDatTourId.Value))
                .ToList();
            var ttDon = _context.DatTours.AsNoTracking()
                .Where(d => dsDonId.Contains(d.Id))
                .ChonDon(_context)
                .Select(d => new { d.Id, d.TrangThaiThanhToan })
                .ToList()
                .ToDictionary(d => d.Id, d => d.TrangThaiThanhToan);

            string ThanhToanCua(HanhKhachDatTour h)
            {
                if (h.DatTour!.LoaiDon == "Nhom")
                {
                    return TrangThaiGhe.Tinh(h, gdGhe) switch
                    {
                        TrangThaiGhe.DaTra => ThanhToanKhach.DaTra,
                        TrangThaiGhe.ChoXacNhan => ThanhToanKhach.DangKiemTra,
                        _ => ThanhToanKhach.ChuaTra
                    };
                }
                return ttDon.TryGetValue(h.DatTourId, out var tt) && tt == TrangThaiThanhToanDon.DaThanhToan ? ThanhToanKhach.DaTra
                    : tt == TrangThaiThanhToanDon.DangKiemTra ? ThanhToanKhach.DangKiemTra
                    : ThanhToanKhach.ChuaTra;
            }

            var model = new DanhSachKhachViewModel
            {
                LichId = lich.Id,
                TenTour = lich.Tour?.TenTour ?? string.Empty,
                NgayKhoiHanh = lich.NgayKhoiHanh,
                GioKhoiHanh = lich.GioKhoiHanh,
                TenXe = lich.SoDoChoNgoi?.TenSoDo,
                LaHuongDanVien = laHuongDanVien,
                DsKhach = dsGhe.Select(h =>
                {
                    choTheoGhe.TryGetValue(h.Id, out var cho);
                    return new HanhKhachTrenChuyen
                    {
                        HoTen = h.HoTen,
                        LoaiHanhKhach = h.LoaiHanhKhach,
                        MaCho = cho?.MaViTri,
                        HangCho = cho?.Hang ?? int.MaxValue,
                        CotCho = cho?.Cot ?? int.MaxValue,
                        GiayToRutGon = laHuongDanVien ? ThanhToanKhach.RutGonGiayTo(h.SoGiayTo) : null,
                        MaChuyenDi = h.DatTour!.MaChuyenDi,
                        LaDonNhom = h.DatTour.LoaiDon == "Nhom",
                        NguoiLienHe = h.DatTour.TenKhach,
                        DienThoaiLienHe = h.DatTour.DienThoai,
                        ThanhToan = ThanhToanCua(h),
                        HanGiuCho = h.DatTour.TrangThai == "GiuCho" ? h.DatTour.HanGiuCho : null
                    };
                })
                // Theo thứ tự chỗ ngồi trên xe, ghế chưa có chỗ xếp cuối
                .OrderBy(k => k.HangCho).ThenBy(k => k.CotCho).ThenBy(k => k.HoTen)
                .ToList()
            };

            return View(model);
        }
    }
}