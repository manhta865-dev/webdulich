using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text.Json;
using webdulich.Models;

namespace webdulich.Controllers
{
    public class DatTourController : Controller
    {
        private readonly DuLichDbContext _context; // Biến để truy cập cơ sở dữ liệu thông qua Entity Framework Core
        private readonly IConfiguration _cauHinh;

        public DatTourController(DuLichDbContext context, IConfiguration cauHinh)
        {
            _context = context;
            _cauHinh = cauHinh;
        }

        // Đơn cá nhân giữ chỗ cùng số giờ với đơn nhóm (RB-38)
        private int SoGioGiuCho => _cauHinh.GetValue("DonNhom:SoGioGiuCho", 24);
        
        private IQueryable<Tour> LayQueryTourDayDu()
        {
            return _context.Tours
                .Include(x => x.DiaDiemInfo)
                .Include(x => x.DanhMuc)
                .Include(x => x.PhuongTienInfo)
                .Include(x => x.TourLoaiDiaHinhs)
                    .ThenInclude(x => x.LoaiDiaHinh)
                .Include(x => x.TourPhongCachs)
                    .ThenInclude(x => x.PhongCach);
        }
        // Hàm này sẽ lấy thông tin tài khoản đang đăng nhập dựa trên session hoặc tham số user
        private int? LayTaiKhoanIdDangNhap(string? user)
        {
            // Chỉ tin taiKhoanId trong Session. Trước đây còn tìm theo họ tên nên người trùng tên
            // có thể thấy hoặc hủy đơn của nhau (lỗi 7).
            return HttpContext.Session.GetInt32("taiKhoanId");
        }

        // Mã chuyến đi và giá ghế dùng chung với đơn nhóm (XuLyDon)
        private string TaoMaChuyenDi() => XuLyDon.TaoMaChuyenDi(_context);

        private static decimal GiaGhe(string? loaiHanhKhach, decimal giaNguoiLon, decimal giaTreEm) =>
            XuLyDon.GiaGhe(loaiHanhKhach, giaNguoiLon, giaTreEm);
        // Hàm này sẽ lấy danh sách hành khách từ form dựa trên số người đã nhập
        private List<HanhKhachDatTour> LayDanhSachHanhKhachTuForm(int soNguoi)
        {
            // Tạo một danh sách để chứa thông tin hành khách
            var danhSachHanhKhach = new List<HanhKhachDatTour>();

            for (int i = 0; i < soNguoi; i++)
            {
                // Lấy thông tin hành khách từ form với tên trường theo định dạng HanhKhach[0].HoTen, HanhKhach[0].LoaiGiayTo, v.v.
                var hoTen = Request.Form[$"HanhKhach[{i}].HoTen"].ToString().Trim();
                var loaiGiayTo = Request.Form[$"HanhKhach[{i}].LoaiGiayTo"].ToString().Trim();
                var soGiayTo = Request.Form[$"HanhKhach[{i}].SoGiayTo"].ToString().Trim();
                var loaiHanhKhach = Request.Form[$"HanhKhach[{i}].LoaiHanhKhach"].ToString().Trim();
                // Kiểm tra hợp lệ của thông tin hành khách và thêm lỗi vào ModelState nếu có vấn đề
                if (string.IsNullOrWhiteSpace(hoTen))
                {
                    ModelState.AddModelError("", $"Vui lòng nhập họ tên hành khách {i + 1}.");
                }
                //  Nếu loại giấy tờ không được chọn, thêm lỗi vào ModelState
                if (string.IsNullOrWhiteSpace(loaiGiayTo))
                {
                    ModelState.AddModelError("", $"Vui lòng chọn loại giấy tờ hành khách {i + 1}.");
                }
                // Nếu số giấy tờ không được nhập, thêm lỗi vào ModelState
                if (string.IsNullOrWhiteSpace(soGiayTo))
                {
                    ModelState.AddModelError("", $"Vui lòng nhập số giấy tờ hành khách {i + 1}.");
                }

                if (string.IsNullOrWhiteSpace(loaiHanhKhach))
                {
                    loaiHanhKhach = "Người lớn";
                }
                // Nếu thông tin hành khách hợp lệ, thêm vào danh sách
                danhSachHanhKhach.Add(new HanhKhachDatTour
                {
                    HoTen = hoTen,
                    LoaiGiayTo = loaiGiayTo,
                    SoGiayTo = soGiayTo,
                    LoaiHanhKhach = loaiHanhKhach
                });
            }

            return danhSachHanhKhach;
        }
        // Hàm này sẽ kiểm tra mã khuyến mãi có hợp lệ hay không dựa trên các điều kiện như ngày bắt đầu, ngày kết thúc, trạng thái, và số lượt dùng
        // Kiểm mã và tính tiền giảm dùng chung với đơn nhóm (XuLyDon)
        private KhuyenMai? LayKhuyenMaiHopLe(string? maKhuyenMai) => XuLyDon.LayKhuyenMaiHopLe(_context, maKhuyenMai);

        private static decimal TinhTienGiam(KhuyenMai khuyenMai, decimal tongTien) => XuLyDon.TinhTienGiam(khuyenMai, tongTien);
        // Hàm này sẽ tính tổng tiền trước khi áp dụng khuyến mãi dựa trên giá người lớn và giá trẻ em của tour hoặc lịch khởi hành, và số lượng người lớn và trẻ em
        private decimal TinhTongTienTruocGiam(Tour tour, LichKhoiHanhTour? lich, int soNguoiLon, int soTreNho)
        {
            // Nếu lịch khởi hành có giá người lớn và giá trẻ em, sử dụng chúng để tính tổng tiền. Nếu không, sử dụng giá của tour để tính tổng tiền, với giả định rằng giá trẻ em bằng một nửa giá người lớn
            decimal giaNguoiLon = lich?.GiaNguoiLon ?? tour.Gia;
            decimal giaTreEm = lich?.GiaTreEm ?? giaNguoiLon * 0.5m;
            // Tính tổng tiền bằng cách nhân số người lớn với giá người lớn và số trẻ nhỏ với giá trẻ em, sau đó cộng lại với nhau để có tổng tiền trước khi áp dụng khuyến mãi
            return (soNguoiLon * giaNguoiLon) + (soTreNho * giaTreEm);
        }
        // Hàm này sẽ định dạng số tiền thành chuỗi có định dạng tiền tệ Việt Nam, với dấu phân cách hàng nghìn và đơn vị VNĐ
        private string FormatTien(decimal soTien)
        {
            return soTien.ToString("N0") + " VNĐ";
        }
        // Hàm này sẽ xóa các session liên quan đến khuyến mãi khi người dùng thay đổi thông tin đặt tour hoặc bỏ mã khuyến mãi, để đảm bảo rằng thông tin khuyến mãi không còn áp dụng cho các tour mới được đặt
        private void XoaSessionKhuyenMai()
        {
            // Xóa các session liên quan đến khuyến mãi để đảm bảo rằng thông tin khuyến mãi không còn áp dụng cho các tour mới được đặt 
            HttpContext.Session.Remove("DatTour_KhuyenMaiId");
            HttpContext.Session.Remove("DatTour_MaKhuyenMai");
        }
        // Hàm này sẽ hiển thị trang đặt tour cho một tour cụ thể và lịch khởi hành nếu có, kiểm tra quyền truy cập của người dùng, và chuẩn bị dữ liệu cần thiết để hiển thị trang đặt tour
        public IActionResult DatTour(int id, int? lichId)
        {
            // Kiểm tra xem người dùng đã đăng nhập hay chưa bằng cách lấy thông tin từ session. Nếu chưa đăng nhập, chuyển hướng đến trang đăng nhập. Nếu người dùng là admin, chuyển hướng về trang chủ vì admin không được phép đặt tour
            var user = HttpContext.Session.GetString("user");
            var role = HttpContext.Session.GetString("role");
            // Nếu người dùng chưa đăng nhập, chuyển hướng đến trang đăng nhập để yêu cầu họ đăng nhập trước khi đặt tour. Nếu người dùng đã đăng nhập nhưng có vai trò là admin, chuyển hướng về trang chủ vì admin không được phép đặt tour
            if (string.IsNullOrEmpty(user))
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }
            // Nếu người dùng có vai trò là admin, chuyển hướng về trang chủ vì admin không được phép đặt tour
            if (role == "admin")
            {
                return RedirectToAction("Index", "TrangChu");
            }
            // Truy vấn cơ sở dữ liệu để lấy thông tin tour dựa trên id, bao gồm các thông tin liên quan như địa điểm, danh mục, phương tiện, loại địa hình, và phong cách. Nếu không tìm thấy tour, trả về lỗi 404 Not Found
            var tour = LayQueryTourDayDu().FirstOrDefault(x => x.Id == id);
            // Nếu không tìm thấy tour, trả về lỗi 404 Not Found để thông báo rằng tour không tồn tại
            if (tour == null)
            {   
                return NotFound();
            }
            // 
            if (!lichId.HasValue)
            {
                return RedirectToAction("ChiTiet", "DiaDiem", new { id = tour.Id });
            }
            // 
            var lich = _context.LichKhoiHanhTours
                .FirstOrDefault(x => x.Id == lichId.Value && x.TourId == tour.Id);
            if (lich == null)
            {
                return NotFound();
            }
            if (tour.NgungBan || lich.TrangThai != TrangThaiLich.MoBan || lich.NgayKhoiHanh.Date <= GioVN.HomNay)
            {
                TempData["Loi"] = "Lịch khởi hành này không còn nhận đặt tour.";
                return RedirectToAction("ChiTiet", "DiaDiem", new { id = tour.Id });
            }

            ViewBag.Tour = tour;
            ViewBag.LichKhoiHanh = lich;

            var model = new DatTours
            {
                TourId = tour.Id,
                LichKhoiHanhTourId = lich.Id,
                TaiKhoanId = LayTaiKhoanIdDangNhap(user),
                TenKhach = user,
                SoNguoi = 1,
                NgayDi = lich.NgayKhoiHanh
            };

            return View(model);
        }
        // Hàm này sẽ xử lý yêu cầu POST khi người dùng gửi thông tin đặt tour, kiểm tra tính hợp lệ của dữ liệu, lưu thông tin đặt tour vào session để chuẩn bị cho bước thanh toán, và chuyển hướng đến trang thanh toán nếu dữ liệu hợp lệ
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DatTour(DatTours model, int SoNguoiLon, int SoTreNho)
        {
            // Kiểm tra xem người dùng đã đăng nhập hay chưa bằng cách lấy thông tin từ session. Nếu chưa đăng nhập, chuyển hướng đến trang đăng nhập. Nếu người dùng là admin, chuyển hướng về trang chủ vì admin không được phép đặt tour
            var user = HttpContext.Session.GetString("user");
            var role = HttpContext.Session.GetString("role");
            // Nếu người dùng chưa đăng nhập, chuyển hướng đến trang đăng nhập để yêu cầu họ đăng nhập trước khi đặt tour. Nếu người dùng đã đăng nhập nhưng có vai trò là admin, chuyển hướng về trang chủ vì admin không được phép đặt tour
            if (string.IsNullOrEmpty(user))
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            if (role == "admin")
            {
                return RedirectToAction("Index", "TrangChu");
            }
            // Truy vấn cơ sở dữ liệu để lấy thông tin tour dựa trên model.TourId, bao gồm các thông tin liên quan như địa điểm, danh mục, phương tiện, loại địa hình, và phong cách. Nếu không tìm thấy tour, trả về lỗi 404 Not Found
            var tour = LayQueryTourDayDu().FirstOrDefault(x => x.Id == model.TourId);

            if (tour == null)
            {
                return NotFound();
            }
            // Gán thông tin khách hàng và số người vào model đặt tour, sau đó kiểm tra tính hợp lệ của dữ liệu như lịch khởi hành, số người, ngày đi, và thông tin hành khách. Nếu có lỗi, thêm lỗi vào ModelState để hiển thị trên giao diện người dùng
            model.TenKhach = user;
            model.TaiKhoanId = LayTaiKhoanIdDangNhap(user);
            model.SoNguoi = SoNguoiLon + SoTreNho;
            // Kiểm tra tính hợp lệ của lịch khởi hành nếu người dùng đã chọn, và thêm lỗi vào ModelState nếu lịch khởi hành không hợp lệ hoặc không còn đủ chỗ. Nếu lịch khởi hành hợp lệ, gán ngày đi từ lịch khởi hành để đảm bảo tính nhất quán
            LichKhoiHanhTour? lich = null;
            // Nếu người dùng đã chọn lịch khởi hành, truy vấn cơ sở dữ liệu để lấy thông tin lịch khởi hành dựa trên model.LichKhoiHanhTourId và model.TourId. Nếu không tìm thấy lịch khởi hành hoặc lịch khởi hành không thuộc tour đang đặt, thêm lỗi vào ModelState để thông báo rằng lịch khởi hành không hợp lệ. Nếu tìm thấy lịch khởi hành, gán ngày đi từ lịch khởi hành và kiểm tra xem lịch khởi hành còn đủ chỗ cho số người đặt hay không. Nếu không còn đủ chỗ, thêm lỗi vào ModelState để thông báo rằng lịch này không còn đủ chỗ. Nếu lịch khởi hành hợp lệ và còn đủ chỗ, lưu thông tin lịch khởi hành vào ViewBag để hiển thị trên giao diện người dùng
            if (model.LichKhoiHanhTourId > 0)
            {
                // Truy vấn cơ sở dữ liệu để lấy thông tin lịch khởi hành dựa trên model.LichKhoiHanhTourId và model.TourId. Nếu không tìm thấy lịch khởi hành hoặc lịch khởi hành không thuộc tour đang đặt, thêm lỗi vào ModelState để thông báo rằng lịch khởi hành không hợp lệ. Nếu tìm thấy lịch khởi hành, gán ngày đi từ lịch khởi hành và kiểm tra xem lịch khởi hành còn đủ chỗ cho số người đặt hay không. Nếu không còn đủ chỗ, thêm lỗi vào ModelState để thông báo rằng lịch này không còn đủ chỗ. Nếu lịch khởi hành hợp lệ và còn đủ chỗ, lưu thông tin lịch khởi hành vào ViewBag để hiển thị trên giao diện người dùng
                lich = _context.LichKhoiHanhTours
                    .FirstOrDefault(x => x.Id == model.LichKhoiHanhTourId && x.TourId == model.TourId);
                // Nếu không tìm thấy lịch khởi hành hoặc lịch khởi hành không thuộc tour đang đặt, thêm lỗi vào ModelState để thông báo rằng lịch khởi hành không hợp lệ. Nếu tìm thấy lịch khởi hành, gán ngày đi từ lịch khởi hành và kiểm tra xem lịch khởi hành còn đủ chỗ cho số người đặt hay không. Nếu không còn đủ chỗ, thêm lỗi vào ModelState để thông báo rằng lịch này không còn đủ chỗ. Nếu lịch khởi hành hợp lệ và còn đủ chỗ, lưu thông tin lịch khởi hành vào ViewBag để hiển thị trên giao diện người dùng
                if (lich == null)
                {
                    ModelState.AddModelError("", "Lịch khởi hành không hợp lệ.");
                }
                else
                {
                    // Gán ngày đi từ lịch khởi hành để đảm bảo tính nhất quán, và kiểm tra xem lịch khởi hành còn đủ chỗ cho số người đặt hay không. Nếu không còn đủ chỗ, thêm lỗi vào ModelState để thông báo rằng lịch này không còn đủ chỗ. Nếu lịch khởi hành hợp lệ và còn đủ chỗ, lưu thông tin lịch khởi hành vào ViewBag để hiển thị trên giao diện người dùng
                    model.NgayDi = lich.NgayKhoiHanh;
                    // ktr số chỗ còn đủ hay k 
                    // cần ktr lại đặt cùng lúc 2 người cùng 1 chỗ 
                    if (lich.SoChoCon < model.SoNguoi)
                    {
                        ModelState.AddModelError("SoNguoi", "Lịch này không còn đủ chỗ.");
                    }
                    ViewBag.LichKhoiHanh = lich;
                }
            }
            else
            {
                // Nếu người dùng không chọn lịch khởi hành, thêm lỗi vào ModelState để yêu cầu họ chọn lịch khởi hành trước khi đặt tour
                ModelState.AddModelError("", "Vui lòng chọn lịch khởi hành trước khi đặt tour.");
            }

            if (model.SoNguoi <= 0) // Kiểm tra nếu số người đặt tour nhỏ hơn hoặc bằng 0, nếu có, thêm lỗi vào ModelState để thông báo rằng số người phải lớn hơn 0
            {
                ModelState.AddModelError("SoNguoi", "Số người phải lớn hơn 0.");
            }

            if (model.NgayDi < GioVN.HomNay) // Kiểm tra nếu ngày đi nhỏ hơn ngày hiện tại, nếu có, thêm lỗi vào ModelState để thông báo rằng ngày đi phải từ hôm nay trở đi
            {
                ModelState.AddModelError("NgayDi", "Ngày đi phải từ hôm nay trở đi.");
            }

            if (SoNguoiLon < 1)// Kiểm tra nếu số người lớn nhỏ hơn 1, nếu có, thêm lỗi vào ModelState để thông báo rằng phải có ít nhất 1 người lớn trong đoàn
            {
                ModelState.AddModelError("", "Phải có ít nhất 1 người lớn.");
            }

            if (SoTreNho < 0) // Kiểm tra nếu số trẻ nhỏ nhỏ hơn 0, nếu có, thêm lỗi vào ModelState để thông báo rằng số trẻ nhỏ không hợp lệ
            {
                ModelState.AddModelError("", "Số trẻ nhỏ không hợp lệ.");
            }
            // Lấy danh sách hành khách từ form dựa trên số người đã nhập, kiểm tra tính hợp lệ của thông tin hành khách và thêm lỗi vào ModelState nếu có vấn đề. Nếu có lỗi trong ModelState, trả về lại view với thông tin tour và model để người dùng có thể sửa lỗi. Nếu dữ liệu hợp lệ, lưu thông tin đặt tour vào session để chuẩn bị cho bước thanh toán, bao gồm thông tin khách hàng, số người, ngày đi, thông tin hành khách, và các thông tin liên quan đến tour và lịch khởi hành. Sau đó chuyển hướng đến trang thanh toán
            var danhSachHanhKhach = LayDanhSachHanhKhachTuForm(model.SoNguoi);

            if (!ModelState.IsValid)
            {
                ViewBag.Tour = tour;
                return View(model);
            }
            // Lưu thông tin đặt tour vào session để chuẩn bị cho bước thanh toán, bao gồm thông tin khách hàng, số người, ngày đi, thông tin hành khách, và các thông tin liên quan đến tour và lịch khởi hành. Sau đó chuyển hướng đến trang thanh toán
            HttpContext.Session.SetInt32("DatTour_TourId", model.TourId);
            HttpContext.Session.SetString("DatTour_TenKhach", model.TenKhach ?? "");
            HttpContext.Session.SetString("DatTour_DienThoai", model.DienThoai ?? "");
            HttpContext.Session.SetString("DatTour_Email", model.Email ?? "");
            HttpContext.Session.SetString("DatTour_DiaChi", model.DiaChi ?? "");
            HttpContext.Session.SetString("DatTour_GhiChu", model.GhiChu ?? "");
            HttpContext.Session.SetInt32("DatTour_SoNguoi", model.SoNguoi);
            HttpContext.Session.SetInt32("DatTour_SoNguoiLon", SoNguoiLon);
            HttpContext.Session.SetInt32("DatTour_SoTreNho", SoTreNho);
            HttpContext.Session.SetString("DatTour_NgayDi", model.NgayDi.ToString("yyyy-MM-dd"));
            HttpContext.Session.SetString("DatTour_HanhKhach", JsonSerializer.Serialize(danhSachHanhKhach));
            // Lưu thông tin tài khoản nếu có, để liên kết với đơn đặt tour khi thanh toán. Nếu không có thông tin tài khoản, xóa session liên quan để đảm bảo rằng đơn đặt tour không bị liên kết với tài khoản nào
            if (model.TaiKhoanId.HasValue)
            {
                HttpContext.Session.SetInt32("DatTour_TaiKhoanId", model.TaiKhoanId.Value);
            }
            else
            {
                HttpContext.Session.Remove("DatTour_TaiKhoanId");
            }

            if (model.LichKhoiHanhTourId > 0)
            {
                HttpContext.Session.SetInt32("DatTour_LichKhoiHanhTourId", model.LichKhoiHanhTourId);
            }
            else
            {
                HttpContext.Session.Remove("DatTour_LichKhoiHanhTourId");
            }
            XoaSessionKhuyenMai();
            HttpContext.Session.Remove(KhoaChoNgoi);

            // Lịch có sơ đồ thì sang bước chọn chỗ ngồi, không có thì sang thanh toán
            return lich?.SoDoChoNgoiId != null
                ? RedirectToAction("ChonChoNgoi")
                : RedirectToAction("ThanhToan");
        }
        // Hàm này sẽ xử lý yêu cầu AJAX để áp dụng mã khuyến mãi cho tour đang đặt, kiểm tra tính hợp lệ của mã khuyến mãi, tính toán số tiền giảm và tổng tiền sau khi áp dụng khuyến mãi, và trả về kết quả dưới dạng JSON để cập nhật giao diện người dùng một cách động mà không cần tải lại trang
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ApDungKhuyenMaiAjax(string maKhuyenMai) // 
        {
            // Kiểm tra xem người dùng đã đăng nhập hay chưa bằng cách lấy thông tin từ session. Nếu chưa đăng nhập, trả về JSON với thông báo yêu cầu đăng nhập để áp dụng khuyến mãi. Nếu có thông tin tour đang đặt trong session, tiếp tục xử lý. Nếu không tìm thấy thông tin tour, trả về JSON với thông báo lỗi. Nếu tour không tồn tại, trả về JSON với thông báo lỗi.
            var user = HttpContext.Session.GetString("user");
            if (string.IsNullOrEmpty(user))
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập để áp dụng khuyến mãi." });
            }
            // Lấy thông tin tour đang đặt từ session để tính toán tổng tiền trước khi áp dụng khuyến mãi. Nếu không tìm thấy thông tin tour, trả về JSON với thông báo lỗi. Nếu tour không tồn tại, trả về JSON với thông báo lỗi.
            var tourId = HttpContext.Session.GetInt32("DatTour_TourId");
            if (!tourId.HasValue)
            {
                return Json(new { success = false, message = "Không tìm thấy thông tin tour đang đặt." });
            }
            // Truy vấn cơ sở dữ liệu để lấy thông tin tour dựa trên tourId từ session. Nếu không tìm thấy tour, trả về JSON với thông báo lỗi.
            var tour = _context.Tours.FirstOrDefault(x => x.Id == tourId.Value);
            if (tour == null)
            {
                return Json(new { success = false, message = "Tour không tồn tại." });
            }
            // Lấy số người lớn và số trẻ nhỏ từ session để tính toán tổng tiền trước khi áp dụng khuyến mãi. Nếu không tìm thấy thông tin số người, sử dụng giá trị mặc định (1 người lớn và 0 trẻ nhỏ) để đảm bảo rằng tính toán vẫn có thể thực hiện được.
            int soNguoiLon = HttpContext.Session.GetInt32("DatTour_SoNguoiLon") ?? 1;
            int soTreNho = HttpContext.Session.GetInt32("DatTour_SoTreNho") ?? 0;
            // Lấy thông tin lịch khởi hành từ session để tính toán tổng tiền trước khi áp dụng khuyến mãi. Nếu không tìm thấy thông tin lịch khởi hành, sử dụng giá của tour để tính tổng tiền. Nếu tìm thấy lịch khởi hành, sử dụng giá người lớn và giá trẻ em của lịch khởi hành để tính tổng tiền.
            var lichId = HttpContext.Session.GetInt32("DatTour_LichKhoiHanhTourId");
            var lich = lichId.HasValue
                ? _context.LichKhoiHanhTours.FirstOrDefault(x => x.Id == lichId.Value)
                : null;
            // Tính tổng tiền trước khi áp dụng khuyến mãi dựa trên giá người lớn và giá trẻ em của tour hoặc lịch khởi hành, và số lượng người lớn và trẻ em. Nếu lịch khởi hành có giá người lớn và giá trẻ em, sử dụng chúng để tính tổng tiền. Nếu không, sử dụng giá của tour để tính tổng tiền, với giả định rằng giá trẻ em bằng một nửa giá người lớn.
            decimal tongTienTruocGiam = TinhTongTienTruocGiam(tour, lich, soNguoiLon, soTreNho);

            if (string.IsNullOrWhiteSpace(maKhuyenMai))
            {
                XoaSessionKhuyenMai();

                return Json(new
                {
                    success = true,
                    message = "Đã bỏ mã khuyến mãi.",
                    maKhuyenMai = "",
                    soTienGiam = FormatTien(0),
                    tongTienTruocGiam = FormatTien(tongTienTruocGiam),
                    tongTien = FormatTien(tongTienTruocGiam)
                });
            }
            // Kiểm tra tính hợp lệ của mã khuyến mãi bằng cách truy vấn cơ sở dữ liệu để tìm khuyến mãi phù hợp với mã đã nhập và đáp ứng các điều kiện về trạng thái, ngày bắt đầu, ngày kết thúc, và số lượt dùng. Nếu không tìm thấy khuyến mãi hợp lệ, xóa session liên quan đến khuyến mãi và trả về JSON với thông báo lỗi. Nếu tìm thấy khuyến mãi hợp lệ, tiếp tục tính toán số tiền giảm và tổng tiền sau khi áp dụng khuyến mãi.
            var khuyenMai = LayKhuyenMaiHopLe(maKhuyenMai);
            if (khuyenMai == null)
            {
                XoaSessionKhuyenMai();
                return Json(new { success = false, message = "Mã khuyến mãi không hợp lệ, hết hạn hoặc đã hết lượt dùng." });
            }
            // Tính số tiền giảm dựa trên kiểu giảm giá của khuyến mãi (theo phần trăm hoặc theo số tiền cố định) và đảm bảo rằng số tiền giảm không vượt quá tổng tiền. Sau đó tính tổng tiền sau khi áp dụng khuyến mãi bằng cách lấy tổng tiền trước khi giảm trừ đi số tiền giảm, và đảm bảo rằng tổng tiền sau khi giảm không âm.
            decimal soTienGiam = TinhTienGiam(khuyenMai, tongTienTruocGiam);
            decimal tongTienSauGiam = Math.Max(0, tongTienTruocGiam - soTienGiam);
            // Lưu thông tin khuyến mãi vào session để áp dụng cho bước thanh toán, bao gồm id khuyến mãi và mã khuyến mãi. Sau đó trả về JSON với thông tin kết quả, bao gồm thông báo thành công, mã khuyến mãi đã áp dụng, số tiền giảm, tổng tiền trước khi giảm, và tổng tiền sau khi giảm, để cập nhật giao diện người dùng một cách động mà không cần tải lại trang.
            HttpContext.Session.SetInt32("DatTour_KhuyenMaiId", khuyenMai.Id);
            HttpContext.Session.SetString("DatTour_MaKhuyenMai", khuyenMai.MaKhuyenMai);

            return Json(new
            {
                success = true,
                message = $"Áp dụng mã {khuyenMai.MaKhuyenMai} thành công.",
                maKhuyenMai = khuyenMai.MaKhuyenMai,
                soTienGiam = FormatTien(soTienGiam),
                tongTienTruocGiam = FormatTien(tongTienTruocGiam),
                tongTien = FormatTien(tongTienSauGiam)
            });
        }
        // ---------- Chọn chỗ ngồi (bước 2 của luồng đặt tour) ----------
        // Chỗ khách chọn chỉ nhớ trong Session, chưa ghi DB; chỗ thật sự thuộc về khách khi bấm
        // "Xác nhận đặt tour" (ghi cùng giao dịch với đơn). Mỗi phần tử ứng với một hành khách theo thứ tự;
        // null nghĩa là để TravelVN xếp.
        private const string KhoaChoNgoi = "DatTour_ChoNgoi";

        private List<int?> DocChoNgoiSession()
        {
            var json = HttpContext.Session.GetString(KhoaChoNgoi);
            return string.IsNullOrEmpty(json) ? new List<int?>() : JsonSerializer.Deserialize<List<int?>>(json) ?? new List<int?>();
        }

        private void LuuChoNgoiSession(List<int?> ds) =>
            HttpContext.Session.SetString(KhoaChoNgoi, JsonSerializer.Serialize(ds));

        private List<HanhKhachDatTour> DocHanhKhachSession()
        {
            var json = HttpContext.Session.GetString("DatTour_HanhKhach");
            return string.IsNullOrEmpty(json)
                ? new List<HanhKhachDatTour>()
                : JsonSerializer.Deserialize<List<HanhKhachDatTour>>(json) ?? new List<HanhKhachDatTour>();
        }

        // Lịch đang đặt trong Session (kèm tour, phương tiện, sơ đồ)
        private LichKhoiHanhTour? LayLichDangDat()
        {
            var lichId = HttpContext.Session.GetInt32("DatTour_LichKhoiHanhTourId");
            if (!lichId.HasValue)
            {
                return null;
            }
            return _context.LichKhoiHanhTours
                .Include(x => x.Tour).ThenInclude(t => t!.PhuongTienInfo)
                .Include(x => x.SoDoChoNgoi)
                .FirstOrDefault(x => x.Id == lichId.Value);
        }

        public IActionResult ChonChoNgoi()
        {
            var user = HttpContext.Session.GetString("user");
            if (string.IsNullOrEmpty(user))
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }
            if (HttpContext.Session.GetString("role") == "admin")
            {
                return RedirectToAction("Index", "TrangChu");
            }

            var lich = LayLichDangDat();
            if (lich == null || lich.Tour == null)
            {
                return RedirectToAction("Index", "TrangChu");
            }
            // Lịch không có sơ đồ (tour chưa có phương tiện): bỏ qua bước này
            if (lich.SoDoChoNgoi == null)
            {
                return RedirectToAction("ThanhToan");
            }

            var dsHanhKhach = DocHanhKhachSession();
            if (!dsHanhKhach.Any())
            {
                return RedirectToAction("DatTour", new { id = lich.TourId, lichId = lich.Id });
            }

            var daCoNguoi = _context.ChoNgoiChuyenDis
                .Where(c => c.LichKhoiHanhTourId == lich.Id)
                .Select(c => c.ViTriChoNgoiId)
                .ToHashSet();
            var chon = DocChoNgoiSession();

            var model = new ChonChoNgoiViewModel
            {
                TenTour = lich.Tour.TenTour,
                NgayDi = lich.NgayKhoiHanh,
                TenSoDo = lich.SoDoChoNgoi.TenSoDo,
                LaMayBay = lich.Tour.PhuongTienInfo?.TenPhuongTien?.StartsWith("Máy bay") == true,
                SoHang = lich.SoDoChoNgoi.SoHang,
                SoCot = lich.SoDoChoNgoi.SoCot,
                DsViTri = _context.ViTriChoNgois
                    .Where(v => v.SoDoChoNgoiId == lich.SoDoChoNgoi.Id)
                    .OrderBy(v => v.Hang).ThenBy(v => v.Cot)
                    .Select(v => new ViTriHienThi { Id = v.Id, MaViTri = v.MaViTri, Hang = v.Hang, Cot = v.Cot })
                    .ToList(),
                DsHanhKhach = dsHanhKhach.Select((h, i) => new HanhKhachChonCho
                {
                    ThuTu = i,
                    HoTen = h.HoTen ?? string.Empty,
                    LoaiHanhKhach = h.LoaiHanhKhach,
                    // Chỗ đã chọn trước đó, bỏ đi nếu vừa có người khác đặt mất
                    ViTriId = i < chon.Count && chon[i].HasValue && !daCoNguoi.Contains(chon[i]!.Value) ? chon[i] : null
                }).ToList()
            };
            var khongMoBan = SucChuaLich.ViTriKhoa(_context, lich.Id);
            foreach (var v in model.DsViTri)
            {
                v.DaCoNguoi = daCoNguoi.Contains(v.Id);
                v.KhongMoBan = khongMoBan.Contains(v.Id);
            }
            foreach (var hk in model.DsHanhKhach.Where(h => h.ViTriId.HasValue && khongMoBan.Contains(h.ViTriId.Value)))
            {
                hk.ViTriId = null; // chỗ vừa bị admin khóa
            }

            ViewBag.QuayLai = Url.Action("DatTour", new { id = lich.TourId, lichId = lich.Id });
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChonChoNgoi(List<int?> viTri, bool tuDong = false)
        {
            var user = HttpContext.Session.GetString("user");
            if (string.IsNullOrEmpty(user))
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            var lich = LayLichDangDat();
            if (lich == null || lich.SoDoChoNgoi == null)
            {
                return RedirectToAction("ThanhToan");
            }

            int soHanhKhach = DocHanhKhachSession().Count;
            if (tuDong)
            {
                LuuChoNgoiSession(Enumerable.Repeat<int?>(null, soHanhKhach).ToList());
                return RedirectToAction("ThanhToan");
            }

            viTri ??= new List<int?>();
            var chon = Enumerable.Range(0, soHanhKhach).Select(i => i < viTri.Count ? viTri[i] : null).ToList();

            // RB-18: chỗ phải thuộc sơ đồ của lịch; chưa có người; không trùng giữa các hành khách
            var maTheoId = _context.ViTriChoNgois
                .Where(v => v.SoDoChoNgoiId == lich.SoDoChoNgoi.Id)
                .ToDictionary(v => v.Id, v => v.MaViTri);
            var daCoNguoi = _context.ChoNgoiChuyenDis
                .Where(c => c.LichKhoiHanhTourId == lich.Id)
                .Select(c => c.ViTriChoNgoiId)
                .ToHashSet();
            var loi = new List<string>();
            var daDung = new HashSet<int>();
            for (int i = 0; i < chon.Count; i++)
            {
                if (!chon[i].HasValue) continue;
                int id = chon[i]!.Value;
                if (!maTheoId.ContainsKey(id) || !daDung.Add(id))
                {
                    chon[i] = null;
                }
                else if (daCoNguoi.Contains(id))
                {
                    loi.Add($"Chỗ {maTheoId[id]} vừa có người đặt.");
                    chon[i] = null;
                }
            }

            LuuChoNgoiSession(chon);
            if (loi.Any())
            {
                TempData["Loi"] = string.Join(" ", loi) + " Bạn chọn chỗ khác nhé.";
                return RedirectToAction("ChonChoNgoi");
            }

            return RedirectToAction("ThanhToan");
        }

        // Hàm này sẽ hiển thị trang thanh toán cho tour đang đặt, kiểm tra quyền truy cập của người dùng, lấy thông tin tour và lịch khởi hành từ session, tính toán tổng tiền trước khi áp dụng khuyến mãi, số tiền giảm, và tổng tiền sau khi áp dụng khuyến mãi dựa trên thông tin tour, lịch khởi hành, số người lớn và trẻ nhỏ, và mã khuyến mãi nếu có. Sau đó chuẩn bị dữ liệu cần thiết để hiển thị trên giao diện người dùng.
        public IActionResult ThanhToan()
        {
            var user = HttpContext.Session.GetString("user");
            var role = HttpContext.Session.GetString("role");

            if (string.IsNullOrEmpty(user))
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            if (role == "admin")
            {
                return RedirectToAction("Index", "TrangChu");
            }
            // Lấy thông tin tour đang đặt từ session để hiển thị trên trang thanh toán. Nếu không tìm thấy thông tin tour, chuyển hướng về trang chủ. Nếu tìm thấy tour, tiếp tục lấy thông tin lịch khởi hành nếu có, và chuẩn bị dữ liệu cần thiết để hiển thị trên giao diện người dùng, bao gồm thông tin tour, lịch khởi hành, thông tin khách hàng, số người, ngày đi, mã chuyến đi, tổng tiền trước khi giảm, số tiền giảm, và tổng tiền sau khi giảm.
            var tourId = HttpContext.Session.GetInt32("DatTour_TourId");

            if (tourId == null)
            {
                return RedirectToAction("Index", "TrangChu");
            }
            // Truy vấn cơ sở dữ liệu để lấy thông tin tour dựa trên tourId từ session, bao gồm các thông tin liên quan như địa điểm, danh mục, phương tiện, loại địa hình, và phong cách. Nếu không tìm thấy tour, trả về lỗi 404 Not Found.
            var tour = LayQueryTourDayDu().FirstOrDefault(x => x.Id == tourId.Value);

            if (tour == null)
            {
                return NotFound();
            }
            // Lấy thông tin lịch khởi hành từ session để hiển thị trên trang thanh toán. Nếu có thông tin lịch khởi hành, truy vấn cơ sở dữ liệu để lấy thông tin lịch khởi hành dựa trên lichId và tourId. Nếu tìm thấy lịch khởi hành, lưu thông tin lịch khởi hành vào ViewBag để hiển thị trên giao diện người dùng.
            var lichId = HttpContext.Session.GetInt32("DatTour_LichKhoiHanhTourId");

            if (lichId.HasValue)
            {
                ViewBag.LichKhoiHanh = _context.LichKhoiHanhTours
                    .FirstOrDefault(x => x.Id == lichId.Value && x.TourId == tour.Id);
            }

            // Chỗ ngồi đã chọn ở bước trước (null: TravelVN xếp khi xác nhận)
            if ((ViewBag.LichKhoiHanh as LichKhoiHanhTour)?.SoDoChoNgoiId != null)
            {
                var chonTT = DocChoNgoiSession();
                var idsTT = chonTT.Where(x => x.HasValue).Select(x => x!.Value).ToList();
                var maTT = _context.ViTriChoNgois.Where(v => idsTT.Contains(v.Id)).ToDictionary(v => v.Id, v => v.MaViTri);
                int soHk = DocHanhKhachSession().Count;
                ViewBag.DsMaCho = Enumerable.Range(0, soHk)
                    .Select(i => i < chonTT.Count && chonTT[i].HasValue && maTT.ContainsKey(chonTT[i]!.Value) ? maTT[chonTT[i]!.Value] : "TravelVN xếp")
                    .ToList();
            }
            // Chuẩn bị dữ liệu cần thiết để hiển thị trên giao diện người dùng, bao gồm thông tin tour, lịch khởi hành, thông tin khách hàng, số người, ngày đi, mã chuyến đi, tổng tiền trước khi giảm, số tiền giảm, và tổng tiền sau khi giảm. Thông tin khách hàng và số người được lấy từ session để đảm bảo tính nhất quán với bước đặt tour trước đó. Mã chuyến đi được tạo mới nếu chưa có trong session để đảm bảo rằng mỗi đơn đặt tour có một mã chuyến đi duy nhất.
            ViewBag.Tour = tour;
            ViewBag.TenKhach = HttpContext.Session.GetString("DatTour_TenKhach");
            ViewBag.DienThoai = HttpContext.Session.GetString("DatTour_DienThoai");
            ViewBag.Email = HttpContext.Session.GetString("DatTour_Email");
            ViewBag.DiaChi = HttpContext.Session.GetString("DatTour_DiaChi");
            ViewBag.GhiChu = HttpContext.Session.GetString("DatTour_GhiChu");
            ViewBag.SoNguoi = HttpContext.Session.GetInt32("DatTour_SoNguoi") ?? 1;
            ViewBag.SoNguoiLon = HttpContext.Session.GetInt32("DatTour_SoNguoiLon") ?? 1;
            ViewBag.SoTreNho = HttpContext.Session.GetInt32("DatTour_SoTreNho") ?? 0;
            // Mã chuyến đi được tạo mới nếu chưa có trong session để đảm bảo rằng mỗi đơn đặt tour có một mã chuyến đi duy nhất. Mã chuyến đi được định dạng theo mẫu "TRAVELVN-{TourId}-{Timestamp}" để đảm bảo tính duy nhất và dễ nhận biết.
            var maChuyenDi = HttpContext.Session.GetString("MaChuyenDi");
            // Tạo mã một lần và giữ trong Session; bước xác nhận dùng lại đúng mã này
            if (string.IsNullOrEmpty(maChuyenDi) || _context.DatTours.Any(x => x.MaChuyenDi == maChuyenDi))
            {
                maChuyenDi = TaoMaChuyenDi();
                HttpContext.Session.SetString("MaChuyenDi", maChuyenDi);
            }
            // Lưu mã chuyến đi vào ViewBag để hiển thị trên giao diện người dùng, giúp khách hàng dễ dàng nhận biết và tham chiếu đến đơn đặt tour của mình trong quá trình thanh toán và sau khi hoàn tất đặt tour.
            ViewBag.MaChuyenDi = maChuyenDi;
            // Lấy ngày đi từ session để hiển thị trên trang thanh toán. Nếu có thông tin ngày đi, phân tích chuỗi ngày đi thành đối tượng DateTime và lưu vào ViewBag để hiển thị trên giao diện người dùng. Nếu không có thông tin ngày đi, ViewBag
            var ngayDiString = HttpContext.Session.GetString("DatTour_NgayDi");
            // Nếu có thông tin ngày đi, phân tích chuỗi ngày đi thành đối tượng DateTime và lưu vào ViewBag để hiển thị trên giao diện người dùng. Nếu không có thông tin ngày đi, ViewBag.NgayDi sẽ không được gán và có thể được xử lý trong giao diện người dùng để hiển thị thông báo hoặc giá trị mặc định.
            if (!string.IsNullOrEmpty(ngayDiString))
            {
                ViewBag.NgayDi = DateTime.Parse(ngayDiString);
            }
            // Lấy số người lớn và số trẻ nhỏ từ session để tính toán tổng tiền trước khi áp dụng khuyến mãi. Nếu không tìm thấy thông tin số người, sử dụng giá trị mặc định (1 người lớn và 0 trẻ nhỏ) để đảm bảo rằng tính toán vẫn có thể thực hiện được.
            var lich = ViewBag.LichKhoiHanh as LichKhoiHanhTour;

            int soNguoiLonThanhToan = HttpContext.Session.GetInt32("DatTour_SoNguoiLon") ?? 1;
            int soTreNhoThanhToan = HttpContext.Session.GetInt32("DatTour_SoTreNho") ?? 0;

            decimal tongTienTruocGiam = TinhTongTienTruocGiam(tour, lich, soNguoiLonThanhToan, soTreNhoThanhToan);
            decimal soTienGiam = 0;
            string maKhuyenMai = "";

            var maKhuyenMaiSession = HttpContext.Session.GetString("DatTour_MaKhuyenMai");
            var khuyenMai = LayKhuyenMaiHopLe(maKhuyenMaiSession);

            if (khuyenMai != null)
            {
                maKhuyenMai = khuyenMai.MaKhuyenMai;
                soTienGiam = TinhTienGiam(khuyenMai, tongTienTruocGiam);
            }
            else
            {
                XoaSessionKhuyenMai();
            }

            ViewBag.TongTienTruocGiam = tongTienTruocGiam;
            ViewBag.SoTienGiam = soTienGiam;
            ViewBag.TongTienSauGiam = Math.Max(0, tongTienTruocGiam - soTienGiam);
            ViewBag.MaKhuyenMai = maKhuyenMai;

            return View();
        }
        // Hàm này sẽ xử lý yêu cầu POST khi người dùng xác nhận thanh toán, kiểm tra tính hợp lệ của dữ liệu, lưu thông tin đặt tour vào cơ sở dữ liệu, cập nhật số chỗ còn lại của lịch khởi hành nếu có, và chuyển hướng đến trang xác nhận sau khi thanh toán thành công. Nếu có lỗi trong quá trình xử lý, thêm lỗi vào ModelState để hiển thị trên giao diện người dùng.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult XacNhanThanhToan(string phuongThucThanhToan)
        {
            var user = HttpContext.Session.GetString("user");
            var role = HttpContext.Session.GetString("role");

            if (string.IsNullOrEmpty(user))
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            if (role == "admin")
            {
                return RedirectToAction("Index", "TrangChu");
            }
            // Lấy thông tin tour đang đặt từ session để xử lý thanh toán. Nếu không tìm thấy thông tin tour, chuyển hướng về trang chủ. Nếu tìm thấy tour, tiếp tục lấy thông tin lịch khởi hành nếu có, và chuẩn bị dữ liệu cần thiết để lưu thông tin đặt tour vào cơ sở dữ liệu, bao gồm thông tin tour, lịch khởi hành, thông tin khách hàng, số người, ngày đi, mã chuyến đi, tổng tiền trước khi giảm, số tiền giảm, tổng tiền sau khi giảm, và phương thức thanh toán.
            var taiKhoanId = LayTaiKhoanIdDangNhap(user);
            // Lấy thông tin tour đang đặt từ session để xử lý thanh toán. Nếu không tìm thấy thông tin tour, chuyển hướng về trang chủ. Nếu tìm thấy tour, tiếp tục lấy thông tin lịch khởi hành nếu có, và chuẩn bị dữ liệu cần thiết để lưu thông tin đặt tour vào cơ sở dữ liệu, bao gồm thông tin tour, lịch khởi hành, thông tin khách hàng, số người, ngày đi, mã chuyến đi, tổng tiền trước khi giảm, số tiền giảm, tổng tiền sau khi giảm, và phương thức thanh toán.
            var tourId = HttpContext.Session.GetInt32("DatTour_TourId");

            if (tourId == null)
            {
                return RedirectToAction("Index", "TrangChu");
            }
            // Truy vấn cơ sở dữ liệu để lấy thông tin tour dựa trên tourId từ session, bao gồm các thông tin liên quan như địa điểm, danh mục, phương tiện, loại địa hình, và phong cách. Nếu không tìm thấy tour, trả về lỗi 404 Not Found.
            var tour = _context.Tours.FirstOrDefault(x => x.Id == tourId.Value);

            if (tour == null)
            {
                return NotFound();
            }
            // Lấy thông tin lịch khởi hành từ session để xử lý thanh toán. Nếu có thông tin lịch khởi hành, truy vấn cơ sở dữ liệu để lấy thông tin lịch khởi hành dựa trên lichId và tourId. Nếu tìm thấy lịch khởi hành, lưu thông tin lịch khởi hành vào biến để sử dụng trong quá trình lưu thông tin đặt tour vào cơ sở dữ liệu và cập nhật số chỗ còn lại của lịch khởi hành nếu có.
            var lichId = HttpContext.Session.GetInt32("DatTour_LichKhoiHanhTourId");
            // Lấy ngày đi từ session để xử lý thanh toán. Nếu có thông tin ngày đi, phân tích chuỗi ngày đi thành đối tượng DateTime để sử dụng trong quá trình lưu thông tin đặt tour vào cơ sở dữ liệu. Nếu không có thông tin ngày đi, sử dụng giá trị mặc định là ngày mai để đảm bảo rằng tính toán vẫn có thể thực hiện được.
            var ngayDiString = HttpContext.Session.GetString("DatTour_NgayDi");
            DateTime ngayDi = GioVN.HomNay.AddDays(1);

            if (!string.IsNullOrEmpty(ngayDiString))
            {
                DateTime.TryParse(ngayDiString, out ngayDi);
            }
            // Lấy số người lớn và số trẻ nhỏ từ session để tính toán tổng tiền trước khi áp dụng khuyến mãi. Nếu không tìm thấy thông tin số người, sử dụng giá trị mặc định (1 người lớn và 0 trẻ nhỏ) để đảm bảo rằng tính toán vẫn có thể thực hiện được.
            int soNguoi = HttpContext.Session.GetInt32("DatTour_SoNguoi") ?? 1;
            int soNguoiLon = HttpContext.Session.GetInt32("DatTour_SoNguoiLon") ?? 1;
            int soTreNho = HttpContext.Session.GetInt32("DatTour_SoTreNho") ?? 0;
            // Lấy thông tin lịch khởi hành từ session để xử lý thanh toán. Nếu có thông tin lịch khởi hành, truy vấn cơ sở dữ liệu để lấy thông tin lịch khởi hành dựa trên lichId và tourId. Nếu tìm thấy lịch khởi hành, lưu thông tin lịch khởi hành vào biến để sử dụng trong quá trình lưu thông tin đặt tour vào cơ sở dữ liệu và cập nhật số chỗ còn lại của lịch khởi hành nếu có.
            LichKhoiHanhTour? lich = null;
            // Nếu người dùng đã chọn lịch khởi hành, truy vấn cơ sở dữ liệu để lấy thông tin lịch khởi hành dựa trên lichId và tourId. Nếu không tìm thấy lịch khởi hành hoặc lịch khởi hành không thuộc tour đang đặt, chuyển hướng về trang chủ. Nếu tìm thấy lịch khởi hành, gán ngày đi từ lịch khởi hành để đảm bảo tính nhất quán, và kiểm tra xem lịch khởi hành còn đủ chỗ cho số người đặt hay không. Nếu không còn đủ chỗ, chuyển hướng về trang đặt tour với thông báo lỗi. Nếu lịch khởi hành hợp lệ và còn đủ chỗ, lưu thông tin lịch khởi hành vào biến để sử dụng trong quá trình lưu thông tin đặt tour vào cơ sở dữ liệu và cập nhật số chỗ còn lại của lịch khởi hành nếu có.
            if (!lichId.HasValue)
            {
                return RedirectToAction("ChiTiet", "DiaDiem", new { id = tour.Id });
            }

            lich = _context.LichKhoiHanhTours
                .Include(x => x.Tour)
                .FirstOrDefault(x => x.Id == lichId.Value && x.TourId == tour.Id);

            if (lich == null)
            {
                return RedirectToAction("Index", "TrangChu");
            }

            // Kiểm tra lại lúc xác nhận: tour còn bán, lịch còn mở bán và chưa tới ngày (lỗi 5, RB-19)
            if (tour.NgungBan || !lich.ChoPhepDatTour)
            {
                TempData["Loi"] = "Lịch khởi hành này không còn nhận đặt tour.";
                return RedirectToAction("ChiTiet", "DiaDiem", new { id = tour.Id });
            }

            if (lich.SoChoCon < soNguoi)
            {
                TempData["Loi"] = "Lịch khởi hành này không còn đủ chỗ.";
                return RedirectToAction("DatTour", new { id = tour.Id, lichId = lich.Id });
            }

            ngayDi = lich.NgayKhoiHanh;
            // Tính tổng tiền trước khi áp dụng khuyến mãi dựa trên giá người lớn và giá trẻ em của tour hoặc lịch khởi hành, và số lượng người lớn và trẻ em. Nếu lịch khởi hành có giá người lớn và giá trẻ em, sử dụng chúng để tính tổng tiền. Nếu không, sử dụng giá của tour để tính tổng tiền, với giả định rằng giá trẻ em bằng một nửa giá người lớn.
            decimal giaNguoiLon = lich.GiaNguoiLon ?? tour.Gia;
            decimal giaTreEm = lich.GiaTreEm ?? giaNguoiLon * 0.5m;
            decimal tongTien = (soNguoiLon * giaNguoiLon) + (soTreNho * giaTreEm);
            decimal tongTienTruocGiam = tongTien;
            decimal soTienGiam = 0;
            KhuyenMai? khuyenMai = null;
            // Lấy mã khuyến mãi từ session để kiểm tra tính hợp lệ của mã khuyến mãi, tính toán số tiền giảm và tổng tiền sau khi áp dụng khuyến mãi. Nếu mã khuyến mãi không hợp lệ, xóa session liên quan đến khuyến mãi để đảm bảo rằng không áp dụng khuyến mãi cho đơn đặt tour.
            var maKhuyenMaiSession = HttpContext.Session.GetString("DatTour_MaKhuyenMai");
            khuyenMai = LayKhuyenMaiHopLe(maKhuyenMaiSession);

            if (khuyenMai != null)
            {
                soTienGiam = TinhTienGiam(khuyenMai, tongTienTruocGiam);
                tongTien = Math.Max(0, tongTienTruocGiam - soTienGiam);
            }
            else
            {
                XoaSessionKhuyenMai();
            }
            // Tạo mới đối tượng DatTours để lưu thông tin đặt tour vào cơ sở dữ liệu, bao gồm thông tin tour, lịch khởi hành, tài khoản người đặt (nếu có), mã chuyến đi, thông tin khách hàng, số người, ngày đi, tổng tiền trước khi giảm, số tiền giảm, tổng tiền sau khi giảm, phương thức thanh toán, và trạng thái thanh toán. Nếu có thông tin hành khách trong session, phân tích chuỗi JSON để lấy danh sách hành khách và lưu vào thuộc tính HanhKhachDatTours của đối tượng DatTours. Sau đó thêm đối tượng DatTours vào DbContext và lưu thay đổi vào cơ sở dữ liệu.
            // Dùng đúng mã đã hiện ở trang thanh toán; nếu mất Session hoặc mã đã bị dùng thì tạo mã mới
            var maChuyenDiDon = HttpContext.Session.GetString("MaChuyenDi");
            if (string.IsNullOrEmpty(maChuyenDiDon) || _context.DatTours.Any(x => x.MaChuyenDi == maChuyenDiDon))
            {
                maChuyenDiDon = TaoMaChuyenDi();
            }

            var model = new DatTours
            {
                LichKhoiHanhTourId = lich.Id,
                TaiKhoanId = taiKhoanId,
                MaChuyenDi = maChuyenDiDon,
                TenKhach = HttpContext.Session.GetString("DatTour_TenKhach") ?? user,
                DienThoai = HttpContext.Session.GetString("DatTour_DienThoai") ?? "",
                Email = HttpContext.Session.GetString("DatTour_Email") ?? "",
                DiaChi = HttpContext.Session.GetString("DatTour_DiaChi") ?? "",
                GhiChu = HttpContext.Session.GetString("DatTour_GhiChu") ?? "",
                SoTienGiam = soTienGiam,
                KhuyenMaiId = khuyenMai?.Id,
                // Đơn cá nhân chờ admin xác nhận đã nhận tiền
                LoaiDon = "CaNhan",
                TrangThai = "GiuCho",
                NgayDat = GioVN.BayGio,
                // Quá hạn mà chưa trả tiền mặt thì đơn tự hủy (RB-38); muộn nhất cuối ngày trước ngày đi
                HanGiuCho = XuLyDon.TinhHanGiuCho(GioVN.BayGio, SoGioGiuCho, lich.NgayKhoiHanh)
            };
            // Lấy danh sách hành khách từ session, nếu có, phân tích chuỗi JSON để lấy danh sách hành khách và lưu vào thuộc tính HanhKhachDatTours của đối tượng DatTours. Nếu không có thông tin hành khách trong session, thuộc tính HanhKhachDatTours sẽ là một danh sách rỗng.
            var hanhKhachJson = HttpContext.Session.GetString("DatTour_HanhKhach");
            // Nếu có thông tin hành khách trong session, phân tích chuỗi JSON để lấy danh sách hành khách và lưu vào thuộc tính HanhKhachDatTours của đối tượng DatTours. Nếu không có thông tin hành khách trong session, thuộc tính HanhKhachDatTours sẽ là một danh sách rỗng.
            if (!string.IsNullOrEmpty(hanhKhachJson))
            {
                var danhSachHanhKhach = JsonSerializer.Deserialize<List<HanhKhachDatTour>>(hanhKhachJson)
                    ?? new List<HanhKhachDatTour>();

                foreach (var hanhKhach in danhSachHanhKhach)
                {
                    model.HanhKhachDatTours.Add(new HanhKhachDatTour
                    {
                        HoTen = hanhKhach.HoTen,
                        LoaiGiayTo = hanhKhach.LoaiGiayTo,
                        SoGiayTo = hanhKhach.SoGiayTo,
                        LoaiHanhKhach = hanhKhach.LoaiHanhKhach,
                        TaiKhoanId = null,
                        GiaLucDat = GiaGhe(hanhKhach.LoaiHanhKhach, giaNguoiLon, giaTreEm)
                    });
                }
            }
            // Thêm đối tượng DatTours vào DbContext và lưu thay đổi vào cơ sở dữ liệu. Nếu có khuyến mãi được áp dụng, tăng số lượt đã dùng của khuyến mãi lên 1. Nếu có lịch khởi hành được chọn, giảm số chỗ còn lại của lịch khởi hành đi số người đặt, và đảm bảo rằng số chỗ còn lại không âm. Sau khi lưu thay đổi vào cơ sở dữ liệu, xóa các session liên quan đến đặt tour để dọn dẹp dữ liệu tạm thời, và chuyển hướng đến trang xác nhận sau khi thanh toán thành công.
            // Số ghế phải khớp số người đã khai ở bước đặt tour
            // và tổng giá các ghế phải bằng đúng số tiền đã hiện ở trang thanh toán
            if (model.HanhKhachDatTours.Count != soNguoi
                || model.HanhKhachDatTours.Sum(h => h.GiaLucDat) != tongTienTruocGiam)
            {
                TempData["Loi"] = "Thông tin hành khách không khớp, vui lòng nhập lại.";
                return RedirectToAction("DatTour", new { id = tour.Id, lichId = lich.Id });
            }

            // Trừ chỗ, tạo đơn, ghế và giao dịch trong MỘT giao dịch CSDL. Câu UPDATE chỉ trừ khi còn đủ chỗ
            // và lịch còn mở bán, nên hai người đặt cùng lúc không làm vượt số chỗ (lỗi 4, RB-12).
            using (var giaoDichCsdl = _context.Database.BeginTransaction())
            {
                int soDongDaTru = _context.Database.ExecuteSqlInterpolated(
                    $"UPDATE LichKhoiHanhTours SET SoChoCon = SoChoCon - {soNguoi} WHERE Id = {lich.Id} AND SoChoCon >= {soNguoi} AND TrangThai = {TrangThaiLich.MoBan}");

                if (soDongDaTru == 0)
                {
                    giaoDichCsdl.Rollback();
                    TempData["Loi"] = "Lịch khởi hành này vừa hết chỗ, vui lòng chọn lịch khác hoặc giảm số người.";
                    return RedirectToAction("DatTour", new { id = tour.Id, lichId = lich.Id });
                }

                _context.DatTours.Add(model);
                _context.SaveChanges();

                if (lich.SoDoChoNgoiId.HasValue)
                {
                    var chon = DocChoNgoiSession();
                    var dsGhe = model.HanhKhachDatTours.ToList(); // cùng thứ tự với danh sách hành khách
                    var chonTay = chon.Where(x => x.HasValue).Select(x => x!.Value).ToList();
                    // RB-18, RB-37: chỉ nhận chỗ thuộc sơ đồ của lịch và đang mở bán
                    var hopLe = XepChoNgoi.ViTriChonDuoc(_context, lich.Id, lich.SoDoChoNgoiId);
                    var conTrong = XepChoNgoi.ViTriTrong(_context, lich.Id, lich.SoDoChoNgoiId.Value, chonTay);
                    int k = 0;
                    for (int i = 0; i < dsGhe.Count; i++)
                    {
                        int? viTri = i < chon.Count && chon[i].HasValue && hopLe.Contains(chon[i]!.Value) ? chon[i] : null;
                        if (!viTri.HasValue && k < conTrong.Count)
                        {
                            viTri = conTrong[k++]; // TravelVN xếp: chỗ trống đầu tiên theo hàng
                        }
                        if (viTri.HasValue)
                        {
                            _context.ChoNgoiChuyenDis.Add(new ChoNgoiChuyenDi
                            {
                                LichKhoiHanhTourId = lich.Id,
                                ViTriChoNgoiId = viTri.Value,
                                HanhKhachDatTourId = dsGhe[i].Id
                            });
                        }
                    }

                    try
                    {
                        _context.SaveChanges();
                    }
                    catch (DbUpdateException)
                    {
                        // Khóa chính (lịch, vị trí) chặn: có người vừa lấy một chỗ. Hủy cả đơn vừa tạo,
                        // bỏ các chỗ đã bị lấy khỏi lựa chọn rồi cho khách chọn lại.
                        giaoDichCsdl.Rollback();
                        var daCoNguoi = _context.ChoNgoiChuyenDis.AsNoTracking()
                            .Where(c => c.LichKhoiHanhTourId == lich.Id)
                            .Select(c => c.ViTriChoNgoiId)
                            .ToHashSet();
                        LuuChoNgoiSession(chon.Select(x => x.HasValue && daCoNguoi.Contains(x.Value) ? null : x).ToList());
                        TempData["Loi"] = "Có chỗ bạn chọn vừa được người khác đặt. Bạn chọn lại chỗ khác nhé.";
                        return RedirectToAction("ChonChoNgoi");
                    }
                }

                // Ghi một giao dịch chờ admin xác nhận (kể cả tiền mặt, để biết khách chọn cách trả nào).
                // Tiền mặt chờ xác nhận hiển thị là "Chưa thanh toán", chuyển khoản hoặc ví là "Đang kiểm tra".
                if (tongTien > 0)
                {
                    _context.GiaoDichThanhToans.Add(new GiaoDichThanhToan
                    {
                        DatTourId = model.Id,
                        SoTien = tongTien,
                        PhuongThuc = phuongThucThanhToan ?? string.Empty,
                        TrangThai = "ChoXacNhan",
                        NgayTao = GioVN.BayGio
                    });
                    _context.SaveChanges();
                }

                giaoDichCsdl.Commit();
            }

            HttpContext.Session.Remove("DatTour_TourId");
            HttpContext.Session.Remove("DatTour_LichKhoiHanhTourId");
            HttpContext.Session.Remove("DatTour_TaiKhoanId");
            HttpContext.Session.Remove("DatTour_TenKhach");
            HttpContext.Session.Remove("DatTour_DienThoai");
            HttpContext.Session.Remove("DatTour_Email");
            HttpContext.Session.Remove("DatTour_DiaChi");
            HttpContext.Session.Remove("DatTour_GhiChu");
            HttpContext.Session.Remove("DatTour_SoNguoi");
            HttpContext.Session.Remove("DatTour_SoNguoiLon");
            HttpContext.Session.Remove("DatTour_SoTreNho");
            HttpContext.Session.Remove("DatTour_NgayDi");
            HttpContext.Session.Remove("DatTour_HanhKhach");
            HttpContext.Session.Remove("MaChuyenDi");
            HttpContext.Session.Remove(KhoaChoNgoi);
            XoaSessionKhuyenMai();

            var maChoDaGhi = _context.ChoNgoiChuyenDis
                .Where(c => c.HanhKhachDatTour!.DatTourId == model.Id)
                .Select(c => c.ViTriChoNgoi!.MaViTri)
                .ToList();
            if (maChoDaGhi.Any())
            {
                TempData["DsCho"] = string.Join(", ", maChoDaGhi.OrderBy(x => x.Length).ThenBy(x => x));
            }

            TempData["PhuongThucThanhToan"] = phuongThucThanhToan;
            TempData["TenTour"] = tour.TenTour;
            TempData["MaChuyenDi"] = model.MaChuyenDi;

            return RedirectToAction("ThanhCong");
        }
        // Hàm này sẽ hiển thị trang xác nhận sau khi thanh toán thành công, kiểm tra quyền truy cập của người dùng, và dọn dẹp các session liên quan đến đặt tour để đảm bảo rằng dữ liệu tạm thời không còn tồn tại sau khi hoàn tất quá trình đặt tour.
        public IActionResult ThanhCong()
        {
            var user = HttpContext.Session.GetString("user");
            var role = HttpContext.Session.GetString("role");

            if (string.IsNullOrEmpty(user))
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            if (role == "admin")
            {
                return RedirectToAction("Index", "TrangChu");
            }
            XoaSessionKhuyenMai();

            return View();
        }
        // Hàm này sẽ hiển thị trang xác nhận sau khi thanh toán thành công, kiểm tra quyền truy cập của người dùng, và dọn dẹp các session liên quan đến đặt tour để đảm bảo rằng dữ liệu tạm thời không còn tồn tại sau khi hoàn tất quá trình đặt tour.
// ---------- Đổi chỗ ngồi sau khi đặt (từ Lịch sử) ----------
        // Chủ đơn chọn hoặc đổi chỗ cho các ghế còn trong đơn, khi đơn chưa hủy, lịch có sơ đồ và chưa tới ngày đi.
        private DatTours? LayDonDuocDoiCho(int id, out string? loi)
        {
            loi = null;
            var taiKhoanId = LayTaiKhoanIdDangNhap(HttpContext.Session.GetString("user"));
            var don = _context.DatTours
                .Include(x => x.HanhKhachDatTours)
                .Include(x => x.LichKhoiHanhTour).ThenInclude(l => l!.Tour).ThenInclude(t => t!.PhuongTienInfo)
                .Include(x => x.LichKhoiHanhTour).ThenInclude(l => l!.SoDoChoNgoi)
                .FirstOrDefault(x => x.Id == id);

            if (don == null || !taiKhoanId.HasValue || don.TaiKhoanId != taiKhoanId.Value)
            {
                loi = "Không tìm thấy đơn của bạn.";
            }
            else if (don.LoaiDon == "Nhom")
            {
                // Đơn nhóm đổi chỗ theo từng ghế ở trang đơn nhóm (DonNhom/ChoNgoiGhe)
                loi = "Đơn nhóm đổi chỗ theo từng ghế ở trang đơn nhóm.";
            }
            else if (don.TrangThai == "DaHuy")
            {
                loi = "Đơn đã hủy, không chọn chỗ được.";
            }
            else if (don.LichKhoiHanhTour?.SoDoChoNgoi == null)
            {
                loi = "Chuyến này không có sơ đồ chỗ ngồi.";
            }
            else if (don.LichKhoiHanhTour.NgayKhoiHanh.Date <= GioVN.HomNay || don.LichKhoiHanhTour.TrangThai == TrangThaiLich.DaHuy)
            {
                loi = "Chuyến đã tới ngày hoặc đã hủy, không đổi chỗ được nữa.";
            }
            return loi == null ? don : null;
        }

        public IActionResult DoiChoNgoi(int id)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("user")))
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            var don = LayDonDuocDoiCho(id, out var loi);
            if (don == null)
            {
                TempData["Loi"] = loi;
                return RedirectToAction("LichSu");
            }

            var lich = don.LichKhoiHanhTour!;
            var dsGhe = don.HanhKhachDatTours.Where(h => h.NgayNha == null).OrderBy(h => h.Id).ToList();
            var dsGheId = dsGhe.Select(h => h.Id).ToList();
            var choHienTai = _context.ChoNgoiChuyenDis
                .Where(c => dsGheId.Contains(c.HanhKhachDatTourId))
                .ToDictionary(c => c.HanhKhachDatTourId, c => c.ViTriChoNgoiId);
            // Chỗ của chính đơn này vẫn hiện là chọn được (để đổi qua lại giữa các hành khách)
            var cuaNguoiKhac = _context.ChoNgoiChuyenDis
                .Where(c => c.LichKhoiHanhTourId == lich.Id && !dsGheId.Contains(c.HanhKhachDatTourId))
                .Select(c => c.ViTriChoNgoiId)
                .ToHashSet();

            var model = new ChonChoNgoiViewModel
            {
                TenTour = lich.Tour?.TenTour ?? string.Empty,
                NgayDi = lich.NgayKhoiHanh,
                TenSoDo = lich.SoDoChoNgoi!.TenSoDo,
                LaMayBay = lich.Tour?.PhuongTienInfo?.TenPhuongTien?.StartsWith("Máy bay") == true,
                SoHang = lich.SoDoChoNgoi.SoHang,
                SoCot = lich.SoDoChoNgoi.SoCot,
                DsViTri = _context.ViTriChoNgois
                    .Where(v => v.SoDoChoNgoiId == lich.SoDoChoNgoi.Id)
                    .OrderBy(v => v.Hang).ThenBy(v => v.Cot)
                    .Select(v => new ViTriHienThi { Id = v.Id, MaViTri = v.MaViTri, Hang = v.Hang, Cot = v.Cot })
                    .ToList(),
                DsHanhKhach = dsGhe.Select((h, i) => new HanhKhachChonCho
                {
                    ThuTu = i,
                    HoTen = h.HoTen ?? string.Empty,
                    LoaiHanhKhach = h.LoaiHanhKhach,
                    ViTriId = choHienTai.TryGetValue(h.Id, out var vt) ? vt : null
                }).ToList(),
                ActionLuu = "DoiChoNgoi",
                DatTourId = don.Id,
                ChoTuDongXep = false
            };
            var khongMoBan = SucChuaLich.ViTriKhoa(_context, lich.Id);
            foreach (var v in model.DsViTri)
            {
                v.DaCoNguoi = cuaNguoiKhac.Contains(v.Id);
                v.KhongMoBan = khongMoBan.Contains(v.Id);
            }

            ViewBag.MaChuyenDi = don.MaChuyenDi;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DoiChoNgoi(int id, List<int?> viTri)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("user")))
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            var don = LayDonDuocDoiCho(id, out var loi);
            if (don == null)
            {
                TempData["Loi"] = loi;
                return RedirectToAction("LichSu");
            }

            var lich = don.LichKhoiHanhTour!;
            var dsGhe = don.HanhKhachDatTours.Where(h => h.NgayNha == null).OrderBy(h => h.Id).ToList();
            var dsGheId = dsGhe.Select(h => h.Id).ToList();
            var choCu = _context.ChoNgoiChuyenDis.Where(c => dsGheId.Contains(c.HanhKhachDatTourId)).ToList();
            // RB-18, RB-37: chỗ mới phải thuộc sơ đồ của lịch và đang mở bán; chỗ đang ngồi thì giữ được
            var hopLe = XepChoNgoi.ViTriChonDuoc(_context, lich.Id, lich.SoDoChoNgoiId);
            hopLe.UnionWith(choCu.Select(c => c.ViTriChoNgoiId));

            // Ô để trống nghĩa là giữ chỗ hiện tại; chỗ không trùng nhau
            viTri ??= new List<int?>();
            var moi = new Dictionary<int, int>();
            var daDung = new HashSet<int>();
            for (int i = 0; i < dsGhe.Count; i++)
            {
                int? chon = i < viTri.Count ? viTri[i] : null;
                chon ??= choCu.FirstOrDefault(c => c.HanhKhachDatTourId == dsGhe[i].Id)?.ViTriChoNgoiId;
                if (chon.HasValue && hopLe.Contains(chon.Value) && daDung.Add(chon.Value))
                {
                    moi[dsGhe[i].Id] = chon.Value;
                }
            }

            using (var giaoDichCsdl = _context.Database.BeginTransaction())
            {
                // Bỏ chỗ cũ rồi ghi chỗ mới trong một giao dịch; chỗ vừa bị người khác lấy thì khóa chính chặn
                _context.ChoNgoiChuyenDis.RemoveRange(choCu);
                _context.SaveChanges();
                foreach (var cap in moi)
                {
                    _context.ChoNgoiChuyenDis.Add(new ChoNgoiChuyenDi
                    {
                        LichKhoiHanhTourId = lich.Id,
                        ViTriChoNgoiId = cap.Value,
                        HanhKhachDatTourId = cap.Key
                    });
                }
                try
                {
                    _context.SaveChanges();
                    giaoDichCsdl.Commit();
                }
                catch (DbUpdateException)
                {
                    giaoDichCsdl.Rollback();
                    TempData["Loi"] = "Có chỗ bạn chọn vừa được người khác đặt, chỗ cũ của bạn vẫn giữ nguyên. Bạn chọn lại nhé.";
                    return RedirectToAction("DoiChoNgoi", new { id });
                }
            }

            TempData["ThanhCong"] = $"Đã lưu chỗ ngồi cho đơn {don.MaChuyenDi}.";
            return RedirectToAction("LichSu");
        }

        public IActionResult LichSu()
        {
            var user = HttpContext.Session.GetString("user");
            var role = HttpContext.Session.GetString("role");

            if (string.IsNullOrEmpty(user))
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            if (role == "admin")
            {
                return RedirectToAction("Index", "TrangChu");
            }
            // Lấy thông tin tài khoản của người dùng đang đăng nhập để lọc lịch sử đặt tour. Nếu có thông tin tài khoản, lọc lịch sử đặt tour dựa trên tài khoản hoặc tên khách hàng. Nếu không có thông tin tài khoản, lọc lịch sử đặt tour dựa trên tên khách hàng. Sau đó sắp xếp lịch sử đặt tour theo ngày đi giảm dần và trả về danh sách để hiển thị trên giao diện người dùng.
            var taiKhoanId = LayTaiKhoanIdDangNhap(user);

            if (!taiKhoanId.HasValue)
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            // Chỉ đơn của tài khoản đang đăng nhập; số người, tiền, trạng thái tính từ ghế và giao dịch
            var ds = _context.DatTours
                .AsNoTracking()
                .Where(x => x.TaiKhoanId == taiKhoanId.Value)
                .ChonDon(_context)
                .OrderByDescending(x => x.NgayDi)
                .ThenByDescending(x => x.Id)
                .ToList();

            // Mã chỗ ngồi của từng đơn (ghế chưa nhả)
            var dsDonId = ds.Select(x => x.Id).ToList();
            ViewBag.ChoTheoDon = _context.ChoNgoiChuyenDis
                .Where(c => dsDonId.Contains(c.HanhKhachDatTour!.DatTourId) && c.HanhKhachDatTour.NgayNha == null)
                .Select(c => new { c.HanhKhachDatTour!.DatTourId, c.ViTriChoNgoi!.MaViTri })
                .AsEnumerable()
                .GroupBy(x => x.DatTourId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.MaViTri).OrderBy(m => m.Length).ThenBy(m => m).ToList());

            // Ghế mình nhận trong đơn nhóm của người khác (đơn đứng tên trưởng nhóm nên không có trong ds ở trên).
            // Lấy cả ghế đã nhả (chốt đơn, bớt ghế, hủy đơn) để thành viên biết chuyện gì xảy ra với ghế của mình.
            var dsGheNhom = _context.HanhKhachDatTours.AsNoTracking()
                .Where(h => h.TaiKhoanId == taiKhoanId.Value
                    && h.DatTour!.LoaiDon == "Nhom" && h.DatTour.TaiKhoanId != taiKhoanId.Value)
                .Select(h => new
                {
                    Ghe = h,
                    h.DatTour!.MaChuyenDi,
                    TenTruongNhom = h.DatTour.TenKhach,
                    DonTrangThai = h.DatTour.TrangThai,
                    h.DatTour.LichKhoiHanhTour!.NgayKhoiHanh,
                    h.DatTour.LichKhoiHanhTour.GioKhoiHanh,
                    TenTour = h.DatTour.LichKhoiHanhTour.Tour!.TenTour,
                    MaCho = _context.ChoNgoiChuyenDis.Where(c => c.HanhKhachDatTourId == h.Id).Select(c => c.ViTriChoNgoi!.MaViTri).FirstOrDefault()
                })
                .ToList();
            var dsGheNhomId = dsGheNhom.Select(x => x.Ghe.Id).ToList();
            var giaoDichGheNhom = _context.GiaoDichThanhToans.AsNoTracking()
                .Where(g => g.HanhKhachDatTourId != null && dsGheNhomId.Contains(g.HanhKhachDatTourId.Value))
                .ToList();
            ViewBag.GheNhom = dsGheNhom
                .OrderByDescending(x => x.NgayKhoiHanh)
                .Select(x => new GheNhomTrongLichSu
                {
                    GheId = x.Ghe.Id,
                    TenTour = x.TenTour,
                    MaChuyenDi = x.MaChuyenDi,
                    TenTruongNhom = x.TenTruongNhom,
                    NgayDi = x.NgayKhoiHanh,
                    GioKhoiHanh = x.GioKhoiHanh,
                    LoaiHanhKhach = x.Ghe.LoaiHanhKhach,
                    GiaLucDat = x.Ghe.GiaLucDat,
                    TienGiam = x.Ghe.TienGiam,
                    MaCho = x.MaCho,
                    TrangThai = TrangThaiGhe.Tinh(x.Ghe, giaoDichGheNhom),
                    DonDaHuy = x.DonTrangThai == "DaHuy",
                    BiTuChoi = TrangThaiGhe.LucBiTuChoi(x.Ghe.Id, giaoDichGheNhom).HasValue
                })
                .ToList();

            return View(ds);
        }
        // Hàm này sẽ xử lý yêu cầu POST khi người dùng muốn hủy đơn đặt tour, kiểm tra quyền truy cập của người dùng, xác minh rằng người dùng có quyền hủy đơn đặt tour này, cập nhật trạng thái thanh toán của đơn đặt tour thành "Đã hủy", và nếu có lịch khởi hành liên quan, tăng số chỗ còn lại của lịch khởi hành lên số người đã đặt. Sau khi cập nhật thông tin, lưu thay đổi vào cơ sở dữ liệu và chuyển hướng về trang lịch sử với thông báo kết quả.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult HuyDatTour(int id)
        {
            var user = HttpContext.Session.GetString("user");
            var role = HttpContext.Session.GetString("role");

            if (string.IsNullOrEmpty(user))
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            if (role == "admin")
            {
                return RedirectToAction("Index", "Admin");
            }

            var taiKhoanId = LayTaiKhoanIdDangNhap(user);

            var datTour = _context.DatTours
                .Include(x => x.LichKhoiHanhTour)
                .Include(x => x.HanhKhachDatTours)
                .FirstOrDefault(x => x.Id == id);

            if (datTour == null)
            {
                TempData["Loi"] = "Không tìm thấy đơn đặt tour.";
                return RedirectToAction("LichSu");
            }

            bool dungChuDon = taiKhoanId.HasValue && datTour.TaiKhoanId == taiKhoanId.Value;

            if (!dungChuDon)
            {
                TempData["Loi"] = "Bạn không có quyền hủy đơn đặt tour này.";
                return RedirectToAction("LichSu");
            }

            if (datTour.TrangThai == "DaHuy")
            {
                TempData["Loi"] = "Đơn này đã được hủy trước đó.";
                return RedirectToAction("LichSu");
            }

            // Đơn nhóm hủy ở trang đơn nhóm (kiểm ghế đã trả, giao dịch theo ghế), không hủy theo kiểu đơn cá nhân
            if (datTour.LoaiDon == "Nhom")
            {
                TempData["Loi"] = "Đơn nhóm hủy ở trang đơn nhóm.";
                return RedirectToAction("ChiTiet", "DonNhom", new { id });
            }

            // RB-39: đã trả, đang chờ kiểm tra chuyển khoản, hoặc đã tới ngày đi thì khách không tự hủy được
            var doc = _context.DatTours.AsNoTracking().Where(x => x.Id == id).ChonDon(_context).First();
            if (!doc.KhachTuHuyDuoc)
            {
                TempData["Loi"] = doc.NgayDi.Date <= GioVN.HomNay
                    ? "Chuyến đã tới ngày khởi hành nên không hủy được."
                    : doc.DaThanhToan
                        ? "Đơn đã thanh toán. Để hủy và hoàn tiền, bạn liên hệ TravelVN."
                        : "Đơn đang được kiểm tra chuyển khoản. Để hủy và hoàn tiền, bạn liên hệ TravelVN.";
                return RedirectToAction("LichSu");
            }

            XuLyDon.HuyDon(_context, datTour);

            TempData["ThanhCong"] = "Hủy đơn đặt tour thành công.";
            return RedirectToAction("LichSu");
        }
    }
}