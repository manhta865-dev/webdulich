using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webdulich.Models;

namespace webdulich.Controllers
{
    public class DiaDiemController : Controller
    {
        private readonly DuLichDbContext _context;

        public DiaDiemController(DuLichDbContext context)
        {
            _context = context;
        }
        // Phương thức này trả về một truy vấn IQueryable<Tour> đã bao gồm tất cả các quan hệ cần thiết để hiển thị thông tin chi tiết của tour, bao gồm địa điểm, danh mục, phương tiện, lịch trình, hình ảnh, loại địa hình và phong cách.
        private IQueryable<Tour> LayQueryTourDayDu()
        {
            return _context.Tours
                .Include(x => x.DiaDiemInfo)
                .Include(x => x.DanhMuc)
                .Include(x => x.PhuongTienInfo)
                .Include(x => x.LichTrinhTours)
                .Include(x => x.TourHinhAnhs)
                .Include(x => x.TourLoaiDiaHinhs)
                    .ThenInclude(x => x.LoaiDiaHinh)
                .Include(x => x.TourPhongCachs)
                    .ThenInclude(x => x.PhongCach);
        }
        // Truy vấn gọn cho trang danh sách: chỉ nạp những gì thẻ tour hiển thị (địa điểm, danh mục, phương tiện, loại địa hình, phong cách).
        // Bỏ lịch trình và hình ảnh phụ vì danh sách không dùng; tách thành nhiều câu SQL nhỏ và không theo dõi thay đổi để SQL Server không phải xin nhiều bộ nhớ.
        private IQueryable<Tour> LayQueryTourChoDanhSach()
        {
            // Danh sách chỉ hiện tour đang bán
            return _context.Tours
                .AsNoTracking()
                .Where(x => !x.NgungBan)
                .Include(x => x.DiaDiemInfo)
                .Include(x => x.DanhMuc)
                .Include(x => x.PhuongTienInfo)
                .Include(x => x.TourLoaiDiaHinhs)
                    .ThenInclude(x => x.LoaiDiaHinh)
                .Include(x => x.TourPhongCachs)
                    .ThenInclude(x => x.PhongCach)
                .AsSplitQuery();
        }
        // Phương thức này xử lý yêu cầu hiển thị danh sách tour. Nó nhận vào hai tham số: keyword để tìm kiếm theo tên tour, tên địa điểm, tên phương tiện, tên danh mục, tên loại địa hình hoặc tên phong cách; và loaiDiaHinh để lọc theo loại địa hình cụ thể. Kết quả được trả về dưới dạng một danh sách các tour đã được lọc và sắp xếp.
        public IActionResult DanhSach(string keyword, string loaiDiaHinh)
        {
            var ds = LayQueryTourChoDanhSach();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();

                ds = ds.Where(x =>
                    x.TenTour.Contains(keyword) ||
                    (x.DiaDiemInfo != null && x.DiaDiemInfo.TenDiaDiem.Contains(keyword)) ||
                    (x.PhuongTienInfo != null && x.PhuongTienInfo.TenPhuongTien.Contains(keyword)) ||
                    (x.DanhMuc != null && x.DanhMuc.TenDanhMuc.Contains(keyword)) ||
                    x.TourLoaiDiaHinhs.Any(ldh =>
                        ldh.LoaiDiaHinh != null &&
                        ldh.LoaiDiaHinh.TenLoaiDiaHinh.Contains(keyword)) ||
                    x.TourPhongCachs.Any(pc =>
                        pc.PhongCach != null &&
                        pc.PhongCach.TenPhongCach.Contains(keyword))
                );
            }
            // Lọc theo loại địa hình nếu có
            if (!string.IsNullOrWhiteSpace(loaiDiaHinh))
            {
                loaiDiaHinh = loaiDiaHinh.Trim();

                ds = ds.Where(x =>
                    x.TourLoaiDiaHinhs.Any(ldh =>
                        ldh.LoaiDiaHinh != null &&
                        ldh.LoaiDiaHinh.TenLoaiDiaHinh == loaiDiaHinh)
                );
            }
            // Sắp xếp theo ngày tạo mới nhất
            ViewBag.Keyword = keyword;
            ViewBag.LoaiDiaHinh = loaiDiaHinh;

            return View(ds.ToList());
        }
        // Phương thức này xử lý yêu cầu hiển thị trang chủ của địa điểm. Nó đơn giản chuyển hướng đến phương thức DanhSach để hiển thị danh sách tour.
        public IActionResult Index()
        {
            return RedirectToAction("DanhSach");
        }
        // Phương thức này xử lý yêu cầu hiển thị chi tiết của một tour cụ thể. Nó nhận vào tham số id để xác định tour cần hiển thị. Nếu tour không tồn tại, nó trả về lỗi 404 Not Found. Nếu tour tồn tại, nó cũng lấy danh sách lịch khởi hành sắp tới của tour đó và truyền cả thông tin tour và lịch khởi hành vào view để hiển thị.
        public IActionResult ChiTiet(int id)
        {
            var tour = LayQueryTourDayDu()
                .FirstOrDefault(x => x.Id == id);

            if (tour == null)
            {
                return NotFound();
            }
            // Lấy danh sách lịch khởi hành sắp tới của tour, loại bỏ những lịch đã hủy và những lịch đã khởi hành trước ngày hôm nay
            var ngayMai = GioVN.HomNay.AddDays(1);

            // Tour ngừng bán thì không còn lịch để đặt; lịch tạm ngưng hoặc đã hủy cũng không hiện
            ViewBag.LichKhoiHanh = _context.LichKhoiHanhTours
                .Where(x => x.TourId == id
                    && !tour.NgungBan
                    && x.TrangThai == TrangThaiLich.MoBan
                    && x.NgayKhoiHanh >= ngayMai)
                .OrderBy(x => x.NgayKhoiHanh)
                .ToList();

            ViewBag.Videos = _context.Videos.AsNoTracking()
                .Where(x => x.TourId == id && x.DangHien)
                .OrderBy(x => x.ThuTu).ThenBy(x => x.Id)
                .ToList();

            return View(tour);
        }
    }
}