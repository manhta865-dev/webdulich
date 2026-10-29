using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using webdulich.Models;

namespace webdulich.Controllers
{
    public class DatTourController : Controller
    {
        private readonly DuLichDbContext _context; // Biến để truy cập cơ sở dữ liệu thông qua Entity Framework Core

        public DatTourController(DuLichDbContext context)
        {
            _context = context;
        }
        
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
            // Đầu tiên, cố gắng lấy taiKhoanId từ session
            var taiKhoanId = HttpContext.Session.GetInt32("taiKhoanId");
            // Nếu taiKhoanId tồn tại trong session, trả về nó
            if (taiKhoanId.HasValue)
            {
                return taiKhoanId.Value;
            }

            if (string.IsNullOrWhiteSpace(user))
            {
                return null;
            }
            // Nếu không có taiKhoanId trong session, cố gắng tìm tài khoản dựa trên tên đăng nhập (user)
            var taiKhoan = _context.TaiKhoans.FirstOrDefault(x => x.HoTen == user);
            return taiKhoan?.Id;
        }
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
        private KhuyenMai? LayKhuyenMaiHopLe(string? maKhuyenMai)
        {
            if (string.IsNullOrWhiteSpace(maKhuyenMai))
            {
                return null;
            }
            // Chuẩn hóa mã khuyến mãi bằng cách loại bỏ khoảng trắng và chuyển thành chữ hoa để so sánh một cách nhất quán với dữ liệu trong cơ sở dữ liệu 
            string ma = maKhuyenMai.Trim().ToUpper();
            var homNay = DateTime.Today;
            // Truy vấn cơ sở dữ liệu để tìm khuyến mãi phù hợp với mã đã chuẩn hóa và đáp ứng các điều kiện về trạng thái, ngày bắt đầu, ngày kết thúc, và số lượt dùng
            return _context.KhuyenMais.FirstOrDefault(x =>
                x.MaKhuyenMai == ma &&
                x.TrangThai == TrangThaiKhuyenMai.HoatDong &&
                x.NgayBatDau <= homNay &&
                x.NgayKetThuc >= homNay &&
                // Lượt đã dùng = số đơn chưa hủy dùng mã (hủy đơn tự trả lượt)
                (!x.SoLuotDungToiDa.HasValue || x.DatTours.Count(d => d.TrangThai != "DaHuy" && d.TrangThaiThanhToan != "Đã hủy") < x.SoLuotDungToiDa.Value));
        }
        // Hàm này sẽ tính số tiền giảm dựa trên kiểu giảm giá của khuyến mãi (theo phần trăm hoặc theo số tiền cố định) và đảm bảo rằng số tiền giảm không vượt quá tổng tiền
        private decimal TinhTienGiam(KhuyenMai khuyenMai, decimal tongTien)
        {
            // Nếu kiểu giảm giá là theo phần trăm, tính số tiền giảm bằng cách lấy tổng tiền nhân với giá trị giảm chia cho 100, và đảm bảo rằng số tiền giảm không vượt quá tổng tiền
            if (khuyenMai.KieuGiamGia == "PhanTram")
            {
                return Math.Min(tongTien, tongTien * khuyenMai.GiaTriGiam / 100m);
            }

            return Math.Min(tongTien, khuyenMai.GiaTriGiam);
        }
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
            if (tour.NgungBan || lich.TrangThai != TrangThaiLich.MoBan || lich.NgayKhoiHanh.Date <= DateTime.Today)
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
            if (model.LichKhoiHanhTourId.HasValue)
            {
                // Truy vấn cơ sở dữ liệu để lấy thông tin lịch khởi hành dựa trên model.LichKhoiHanhTourId và model.TourId. Nếu không tìm thấy lịch khởi hành hoặc lịch khởi hành không thuộc tour đang đặt, thêm lỗi vào ModelState để thông báo rằng lịch khởi hành không hợp lệ. Nếu tìm thấy lịch khởi hành, gán ngày đi từ lịch khởi hành và kiểm tra xem lịch khởi hành còn đủ chỗ cho số người đặt hay không. Nếu không còn đủ chỗ, thêm lỗi vào ModelState để thông báo rằng lịch này không còn đủ chỗ. Nếu lịch khởi hành hợp lệ và còn đủ chỗ, lưu thông tin lịch khởi hành vào ViewBag để hiển thị trên giao diện người dùng
                lich = _context.LichKhoiHanhTours
                    .FirstOrDefault(x => x.Id == model.LichKhoiHanhTourId.Value && x.TourId == model.TourId);
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

            if (model.NgayDi < DateTime.Today) // Kiểm tra nếu ngày đi nhỏ hơn ngày hiện tại, nếu có, thêm lỗi vào ModelState để thông báo rằng ngày đi phải từ hôm nay trở đi
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

            if (model.LichKhoiHanhTourId.HasValue)
            {
                HttpContext.Session.SetInt32("DatTour_LichKhoiHanhTourId", model.LichKhoiHanhTourId.Value);
            }
            else
            {
                HttpContext.Session.Remove("DatTour_LichKhoiHanhTourId");
            }
            XoaSessionKhuyenMai();

            return RedirectToAction("ThanhToan");
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
            // Nếu mã chuyến đi chưa tồn tại trong session, tạo mới mã chuyến đi dựa trên id của tour và timestamp hiện tại để đảm bảo tính duy nhất, sau đó lưu mã chuyến đi vào session để sử dụng cho các bước tiếp theo trong quá trình đặt tour và thanh toán.
            if (string.IsNullOrEmpty(maChuyenDi))
            {
                maChuyenDi = $"TRAVELVN-{tour.Id}-{DateTime.Now:yyyyMMddHHmmss}";
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
            var taiKhoanId = HttpContext.Session.GetInt32("DatTour_TaiKhoanId")
                ?? LayTaiKhoanIdDangNhap(user);
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
            DateTime ngayDi = DateTime.Today.AddDays(1);

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
            if (lichId.HasValue)
            {
                lich = _context.LichKhoiHanhTours
                    .FirstOrDefault(x => x.Id == lichId.Value && x.TourId == tour.Id);

                if (lich == null)
                {
                    return RedirectToAction("Index", "TrangChu");
                }

                if (lich.SoChoCon < soNguoi)
                {
                    TempData["Loi"] = "Lịch khởi hành này không còn đủ chỗ.";
                    return RedirectToAction("DatTour", new { id = tour.Id, lichId = lich.Id });
                }

                ngayDi = lich.NgayKhoiHanh;
            }
            // Tính tổng tiền trước khi áp dụng khuyến mãi dựa trên giá người lớn và giá trẻ em của tour hoặc lịch khởi hành, và số lượng người lớn và trẻ em. Nếu lịch khởi hành có giá người lớn và giá trẻ em, sử dụng chúng để tính tổng tiền. Nếu không, sử dụng giá của tour để tính tổng tiền, với giả định rằng giá trẻ em bằng một nửa giá người lớn.
            decimal giaNguoiLon = lich?.GiaNguoiLon ?? tour.Gia;
            decimal giaTreEm = lich?.GiaTreEm ?? giaNguoiLon * 0.5m;
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
            var model = new DatTours

            {
                TourId = tourId.Value,
                LichKhoiHanhTourId = lichId,
                TaiKhoanId = taiKhoanId,
                MaChuyenDi = $"TRAVELVN-{tour.Id}-{DateTime.Now:yyyyMMddHHmmss}",
                TenKhach = HttpContext.Session.GetString("DatTour_TenKhach") ?? user,
                DienThoai = HttpContext.Session.GetString("DatTour_DienThoai") ?? "",
                Email = HttpContext.Session.GetString("DatTour_Email") ?? "",
                DiaChi = HttpContext.Session.GetString("DatTour_DiaChi") ?? "",
                GhiChu = HttpContext.Session.GetString("DatTour_GhiChu") ?? "",
                SoNguoi = soNguoi,
                NgayDi = ngayDi,
                TongTienTruocGiam = tongTienTruocGiam,
                SoTienGiam = soTienGiam,
                KhuyenMaiId = khuyenMai?.Id,
                MaKhuyenMai = khuyenMai?.MaKhuyenMai,
                TongTien = tongTien,
                PhuongThucThanhToan = phuongThucThanhToan,
                TrangThaiThanhToan = phuongThucThanhToan == "Tiền mặt" ? "Chưa thanh toán" : "Đang kiểm tra"
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
                        LoaiHanhKhach = hanhKhach.LoaiHanhKhach
                    });
                }
            }
            // Thêm đối tượng DatTours vào DbContext và lưu thay đổi vào cơ sở dữ liệu. Nếu có khuyến mãi được áp dụng, tăng số lượt đã dùng của khuyến mãi lên 1. Nếu có lịch khởi hành được chọn, giảm số chỗ còn lại của lịch khởi hành đi số người đặt, và đảm bảo rằng số chỗ còn lại không âm. Sau khi lưu thay đổi vào cơ sở dữ liệu, xóa các session liên quan đến đặt tour để dọn dẹp dữ liệu tạm thời, và chuyển hướng đến trang xác nhận sau khi thanh toán thành công.
            _context.DatTours.Add(model);
            // Nếu có lịch khởi hành được chọn, giảm số chỗ còn lại của lịch khởi hành đi số người đặt, và đảm bảo rằng số chỗ còn lại không âm. Sau khi lưu thay đổi vào cơ sở dữ liệu, xóa các session liên quan đến đặt tour để dọn dẹp dữ liệu tạm thời, và chuyển hướng đến trang xác nhận sau khi thanh toán thành công.
            if (lich != null)
            {
                lich.SoChoCon -= soNguoi;

                if (lich.SoChoCon < 0)
                {
                    lich.SoChoCon = 0;
                }
            }
            // Lưu thay đổi vào cơ sở dữ liệu để cập nhật thông tin đặt tour, khuyến mãi, và lịch khởi hành nếu có. Sau khi lưu thay đổi, xóa các session liên quan đến đặt tour để dọn dẹp dữ liệu tạm thời, và chuyển hướng đến trang xác nhận sau khi thanh toán thành công.
            _context.SaveChanges();

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
            XoaSessionKhuyenMai();

            TempData["PhuongThucThanhToan"] = phuongThucThanhToan;
            TempData["TenTour"] = tour.TenTour;

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

            var query = _context.DatTours
                .Include(x => x.Tour)
                .Include(x => x.LichKhoiHanhTour)
                .Include(x => x.HanhKhachDatTours)
                .AsQueryable();
            // Nếu có thông tin tài khoản, lọc lịch sử đặt tour dựa trên tài khoản hoặc tên khách hàng. Nếu không có thông tin tài khoản, lọc lịch sử đặt tour dựa trên tên khách hàng. Sau đó sắp xếp lịch sử đặt tour theo ngày đi giảm dần và trả về danh sách để hiển thị trên giao diện người dùng.
            if (taiKhoanId.HasValue)
            {
                query = query.Where(x =>
                    x.TaiKhoanId == taiKhoanId.Value ||
                    (x.TaiKhoanId == null && x.TenKhach == user)
                );
            }
            else
            {
                query = query.Where(x => x.TenKhach == user);
            }
            // Sắp xếp lịch sử đặt tour theo ngày đi giảm dần và trả về danh sách để hiển thị trên giao diện người dùng.
            var ds = query
                .OrderByDescending(x => x.NgayDi)
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
                .FirstOrDefault(x => x.Id == id);

            if (datTour == null)
            {
                TempData["Loi"] = "Không tìm thấy đơn đặt tour.";
                return RedirectToAction("LichSu");
            }

            bool dungChuDon = taiKhoanId.HasValue
                ? datTour.TaiKhoanId == taiKhoanId.Value || (datTour.TaiKhoanId == null && datTour.TenKhach == user)
                : datTour.TenKhach == user;

            if (!dungChuDon)
            {
                TempData["Loi"] = "Bạn không có quyền hủy đơn đặt tour này.";
                return RedirectToAction("LichSu");
            }

            if (datTour.TrangThaiThanhToan == "Đã hủy")
            {
                TempData["Loi"] = "Đơn này đã được hủy trước đó.";
                return RedirectToAction("LichSu");
            }

            if (datTour.LichKhoiHanhTour != null && datTour.SoNguoi > 0)
            {
                datTour.LichKhoiHanhTour.SoChoCon += datTour.SoNguoi;
            }

            datTour.TrangThaiThanhToan = "Đã hủy";

            _context.SaveChanges();

            TempData["ThanhCong"] = "Hủy đơn đặt tour thành công.";
            return RedirectToAction("LichSu");
        }
    }
}