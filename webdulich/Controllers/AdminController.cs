using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webdulich.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;
using System.Text;

namespace webdulich.Controllers
{
    public class AdminController : Controller // Controller dành cho quản trị viên để quản lý tour và đơn đặt tour,...
    {
        private readonly DuLichDbContext _context; // Biến _context đại diện cho database. Thông qua _context, chương trình có thể truy vấn, thêm, sửa, xóa dữ liệu trong các bảng như Tours, DanhMucs, DiaDiems, PhuongTiens.

        public AdminController(DuLichDbContext context) // Đây là constructor. Khi AdminController được tạo, ASP.NET Core tự truyền DuLichDbContext vào. Cách này gọi là Dependency Injection. Nhờ đó controller có thể dùng _context để thao tác với database.
        {
            _context = context; // Gán giá trị context được truyền vào cho biến _context của lớp, để các phương thức khác trong controller có thể sử dụng nó.
        }
        //kta quyền admin , Hàm LaAdmin() dùng để bảo vệ các chức năng quản trị, chỉ cho phép tài khoản có vai trò admin truy cập.
        private bool LaAdmin() // Hàm này kiểm tra xem người dùng hiện tại có phải là admin hay không bằng cách kiểm tra giá trị "role" trong session. Nếu giá trị này là "admin", hàm trả về true, ngược lại trả về false.
        {
            return HttpContext.Session.GetString("role") == "admin";
        }
        //Hàm này dùng để nạp dữ liệu cho các ô chọn trong form thêm/sửa tour.
        private void NapDuLieuDropdown() //Hàm này dùng để chuẩn bị dữ liệu cho các ô chọn trong form thêm hoặc sửa tour. Ví dụ khi admin thêm tour, form cần có danh sách loại tour, địa điểm, loại địa hình, phong cách, phương tiện và số ngày.
        {
            ViewBag.DanhMucList = _context.DanhMucs //lấy dữ liệu từ bảng DanhMucs.
                .OrderBy(x => x.TenDanhMuc)//.OrderBy(x => x.TenDanhMuc)
                .Select(x => new SelectListItem //chuyển đổi dữ liệu thành dạng SelectListItem để dễ hiển thị trong dropdown.
                {
                    Value = x.MaDanhMuc.ToString(), //giá trị thật gửi về Controller là mã danh mục.
                    Text = x.TenDanhMuc //chữ hiển thị cho người dùng thấy là tên danh mục.
                })
                .ToList();

            ViewBag.DiaDiemList = _context.DiaDiems //Lấy danh sách địa điểm từ bảng DiaDiems.
                .OrderBy(x => x.TenDiaDiem)
                .Select(x => new SelectListItem 
                {
                    Value = x.MaDiaDiem.ToString(),
                    Text = x.TenDiaDiem
                })
                .ToList();

            ViewBag.LoaiDiaHinhList = _context.LoaiDiaHinhs
                .OrderBy(x => x.TenLoaiDiaHinh)
                .Select(x => new SelectListItem
                {
                    Value = x.TenLoaiDiaHinh,
                    Text = x.TenLoaiDiaHinh
                })
                .ToList();

            ViewBag.PhongCachList = _context.PhongCachs
                .OrderBy(x => x.TenPhongCach)
                .Select(x => new SelectListItem
                {
                    Value = x.TenPhongCach,
                    Text = x.TenPhongCach
                })
                .ToList();

            ViewBag.PhuongTienList = _context.PhuongTiens
                .OrderBy(x => x.TenPhuongTien)
                .Select(x => new SelectListItem
                {
                    Value = x.TenPhuongTien,
                    Text = x.TenPhuongTien
                })
                .ToList();

            ViewBag.SoNgayList = new List<SelectListItem>
            {
                new SelectListItem { Value = "1", Text = "1 ngày" }, //Tạo một danh sách số ngày cho tour, từ 1 đến 5 ngày. Mỗi mục có giá trị là số ngày (dùng để lưu vào database) và văn bản hiển thị cho người dùng.
                new SelectListItem { Value = "2", Text = "2 ngày" },
                new SelectListItem { Value = "3", Text = "3 ngày" },
                new SelectListItem { Value = "4", Text = "4 ngày" },
                new SelectListItem { Value = "5", Text = "5 ngày" }
            };
        }

        //Hàm này dùng để xử lý dữ liệu tour trước khi lưu vào database.
        // khi admin nhập form thêm/sửa tour, dữ liệu gửi về có thể chưa đầy đủ hoặc cần chuyển đổi. Hàm này sẽ kiểm tra danh mục, địa điểm, phương tiện, loại địa hình, phong cách rồi gán lại cho đúng.
        private void DongBoDuLieuTour(Tour tour)
        {
            ModelState.Remove("DiaDiem"); // Loại bỏ lỗi liên quan đến trường DiaDiem vì chúng ta sẽ xử lý nó thủ công.
            ModelState.Remove("DiaDiemId"); // Loại bỏ lỗi liên quan đến trường DiaDiemId vì chúng ta sẽ xử lý nó thủ công.
            ModelState.Remove("DiaDiemInfo"); // Loại bỏ lỗi liên quan đến trường DiaDiemInfo vì chúng ta sẽ xử lý nó thủ công.
            ModelState.Remove("LoaiTour"); //
            ModelState.Remove("DanhMuc"); //
            ModelState.Remove("PhuongTienInfo"); // 
            ModelState.Remove("LoaiDiaHinh");
            ModelState.Remove("PhongCach");

            // Đoạn này kiểm tra admin có chọn danh mục chưa. Nếu có DanhMucId, chương trình tìm danh mục tương ứng trong bảng DanhMucs.
            var danhMuc = tour.DanhMucId.HasValue // Nếu tour có DanhMucId, tìm danh mục tương ứng trong database. Nếu không có DanhMucId, gán danhMuc là null.
                ? _context.DanhMucs.FirstOrDefault(x => x.MaDanhMuc == tour.DanhMucId.Value)
                : null;

            var diaDiemDaCo = tour.DiaDiemId.HasValue
                ? _context.DiaDiems.FirstOrDefault(x => x.MaDiaDiem == tour.DiaDiemId.Value)
                : null;

            var phuongTien = !string.IsNullOrWhiteSpace(tour.PhuongTien)
                ? _context.PhuongTiens.FirstOrDefault(x => x.TenPhuongTien == tour.PhuongTien)
                : null;

            // Nếu không tìm thấy danh mục, thêm lỗi vào ModelState. Nếu tìm thấy, gán tên danh mục cho tour.LoaiTour để hiển thị.
            // Nếu chưa chọn danh mục thì báo lỗi. Nếu có thì gán tên danh mục vào tour.LoaiTour.
            if (danhMuc == null)
            {
                ModelState.AddModelError("DanhMucId", "Vui lòng chọn loại tour");
            }
            else
            {
                tour.LoaiTour = danhMuc.TenDanhMuc;
            }
            // Tương tự với địa điểm, nếu có DiaDiemId thì tìm địa điểm tương ứng. Nếu có thì gán tên địa điểm cho tour.DiaDiem. Nếu không có DiaDiemId nhưng admin nhập tên địa điểm thủ công, thì gán tên đó sau khi đã trim. Nếu không có cả hai, báo lỗi.
            //Nếu chọn địa điểm có sẵn thì lấy tên địa điểm từ database. Nếu không chọn nhưng có nhập tay thì cắt khoảng trắng thừa. Nếu không có gì thì báo lỗi.
            if (diaDiemDaCo != null)
            {
                tour.DiaDiem = diaDiemDaCo.TenDiaDiem;
            }
            else if (!string.IsNullOrWhiteSpace(tour.DiaDiem))
            {
                tour.DiaDiem = tour.DiaDiem.Trim();
            }
            else
            {
                ModelState.AddModelError("DiaDiem", "Vui lòng nhập địa điểm");
            }

            if (phuongTien == null)
            {
                ModelState.AddModelError("PhuongTien", "Vui lòng chọn phương tiện di chuyển");
            }
            else
            {
                tour.PhuongTien = phuongTien.TenPhuongTien;
                tour.PhuongTienId = phuongTien.Id;
            }

            // xử lý nhiều lựa chọn:Nếu admin chọn nhiều loại địa hình, ví dụ “Biển” và “Núi”, hệ thống gộp thành chuỗi
            tour.LoaiDiaHinh = tour.LoaiDiaHinhDaChon != null && tour.LoaiDiaHinhDaChon.Any()
                ? string.Join(",", tour.LoaiDiaHinhDaChon)
                : "";

            tour.PhongCach = tour.PhongCachDaChon != null && tour.PhongCachDaChon.Any()
                ? string.Join(",", tour.PhongCachDaChon)
                : "";
        }

        // Hàm này dùng để gán địa điểm cho tour. Nếu tour đã có DiaDiemId thì không làm gì. Nếu tour chưa có DiaDiemId nhưng có tên địa điểm, chương trình sẽ tìm trong bảng DiaDiems xem đã có địa điểm đó chưa. Nếu có rồi thì gán DiaDiemId và tên cho tour. Nếu chưa có thì tạo một địa điểm mới, lưu vào database để có MaDiaDiem mới, rồi gán cho tour.
        private void GanDiaDiemChoTour(Tour tour) //
        {
            if (tour.DiaDiemId.HasValue) // Nếu tour đã có DiaDiemId thì không cần gán gì nữa, vì đã có địa điểm rồi.
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(tour.DiaDiem)) // Nếu tour chưa có DiaDiemId nhưng cũng không có tên địa điểm, thì không thể gán được địa điểm nào cho tour, nên bỏ qua.
            {
                return;
            }

            string tenDiaDiem = tour.DiaDiem.Trim(); // Cắt bỏ khoảng trắng thừa ở đầu và cuối tên địa điểm để tránh lỗi khi tìm kiếm trong database.

            var diaDiem = _context.DiaDiems // Tìm trong bảng DiaDiems xem đã có địa điểm nào có tên giống như tên địa điểm mà admin nhập (sau khi đã trim và chuyển về chữ thường để so sánh) chưa. Nếu có rồi thì lấy địa điểm đó để gán cho tour.
                .FirstOrDefault(x => x.TenDiaDiem.ToLower() == tenDiaDiem.ToLower());// So sánh tên địa điểm trong database với tên địa điểm mà admin nhập, sau khi đã cắt khoảng trắng và chuyển về chữ thường để tránh lỗi do khác biệt về cách viết hoa hay khoảng trắng. Nếu tìm thấy địa điểm nào có tên giống như vậy, thì gán cho biến diaDiem. Nếu không tìm thấy thì diaDiem sẽ là null.

            if (diaDiem == null) // Nếu chưa có địa điểm nào trong database có tên giống như tên địa điểm mà admin nhập (sau khi đã trim và chuyển về chữ thường để so sánh), thì tạo một địa điểm mới với tên đó. Các trường khác của địa điểm mới có thể để mặc định hoặc lấy từ tour.
            {
                diaDiem = new DiaDiem // Tạo một đối tượng DiaDiem mới với tên là tên địa điểm mà admin nhập. Các trường khác như MoTa, DiaChi, HinhAnh, GiaTour có thể để mặc định hoặc lấy từ tour.
                {
                    TenDiaDiem = tenDiaDiem, // Tên địa điểm mới tạo sẽ là tên địa điểm mà admin nhập, sau khi đã trim.
                    MoTa = "Chưa cập nhật mô tả", // Mô tả mặc định cho địa điểm mới tạo, vì admin chỉ nhập tên địa điểm mà không nhập mô tả. Sau này có thể cho admin chỉnh sửa lại mô tả này.
                    DiaChi = tenDiaDiem, // 
                    HinhAnh = tour.HinhAnh ?? "",  //
                    GiaTour = tour.Gia // 
                };

                _context.DiaDiems.Add(diaDiem); // Thêm địa điểm mới vào database nếu chưa tồn tại. Sau đó gọi SaveChanges để lưu lại và có được MaDiaDiem mới.
                _context.SaveChanges();
            }

            tour.DiaDiemId = diaDiem.MaDiaDiem; // Gán MaDiaDiem của địa điểm tìm được hoặc mới tạo cho tour.DiaDiemId để liên kết tour với địa điểm đó.
            tour.DiaDiem = diaDiem.TenDiaDiem; // Gán tên địa điểm cho tour.DiaDiem để hiển thị. Mặc dù trường này có thể không cần thiết nếu đã có DiaDiemId, nhưng nó giúp hiển thị tên địa điểm dễ dàng hơn trong các view mà không cần phải truy vấn thêm.
        }

        private const long AnhTourToiDaMb = 10;

        // Kiểm tra ảnh đại diện và ảnh phụ trước khi lưu tour; ảnh sai thì báo ngay trên form, chưa ghi gì xuống đĩa hay CSDL
        private void KiemTraAnhTour(Tour tour)
        {
            if (tour.HinhFile != null && !webdulich.Services.KiemTraTepAnh.HopLe(tour.HinhFile, AnhTourToiDaMb, out _, out var loiAnhChinh))
            {
                ModelState.AddModelError(nameof(tour.HinhFile), loiAnhChinh);
            }

            foreach (var tep in tour.HinhPhuFiles?.Where(x => x != null && x.Length > 0) ?? Enumerable.Empty<IFormFile>())
            {
                if (!webdulich.Services.KiemTraTepAnh.HopLe(tep, AnhTourToiDaMb, out _, out var loiAnhPhu))
                {
                    ModelState.AddModelError(nameof(tour.HinhPhuFiles), loiAnhPhu);
                    break;
                }
            }
        }

        // Hàm này dùng để lưu hình ảnh tour lên server. Khi admin chọn một file hình ảnh để làm hình đại diện cho tour, hàm này sẽ được gọi để lưu file đó vào thư mục wwwroot/images/dulich trên server. Hàm sẽ tạo tên file mới bằng cách kết hợp một GUID ngẫu nhiên với tên gốc của file để tránh trùng lặp. Sau khi lưu file, hàm trả về tên file đã lưu để có thể lưu vào database.
        private async Task<string> LuuHinhTourAsync(IFormFile file) // Hàm này nhận vào một file hình ảnh (IFormFile) mà admin đã chọn để làm hình đại diện cho tour. Hàm sẽ lưu file đó vào thư mục wwwroot/images/dulich trên server và trả về tên file đã lưu.
        {
            string folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "dulich"); // Xác định đường dẫn đến thư mục lưu hình ảnh tour trên server. Thư mục này nằm trong wwwroot/images/dulich, và được xây dựng bằng cách kết hợp đường dẫn hiện tại của ứng dụng với các thư mục con.

            if (!Directory.Exists(folder)) // Kiểm tra xem thư mục đã tồn tại chưa. Nếu chưa tồn tại, tạo thư mục đó để đảm bảo có nơi lưu file hình ảnh.
            {
                Directory.CreateDirectory(folder);
            }

            // Đuôi tệp lấy từ nội dung thật, tên tệp chỉ gồm Guid: không dùng lại tên người dùng gửi lên
            if (!webdulich.Services.KiemTraTepAnh.HopLe(file, AnhTourToiDaMb, out var extension, out var loiAnh))
            {
                throw new InvalidOperationException(loiAnh);
            }
            string fileName = Guid.NewGuid().ToString("N") + extension;
            string filePath = Path.Combine(folder, fileName); // Tạo đường dẫn đầy đủ đến file ảnh.

            // Mở luồng ghi file và copy nội dung ảnh upload vào thư mục server.
            using (var stream = new FileStream(filePath, FileMode.Create)) 
            {
                await file.CopyToAsync(stream);
            }

            return fileName; // Trả về tên file đã lưu. Tên này sẽ được lưu vào database, còn file thật nằm trong thư mục wwwroot/images/dulich.
        }

        // Hàm này dùng để lưu các hình ảnh phụ của tour lên server. Khi admin thêm hoặc sửa tour, có thể chọn nhiều file hình ảnh phụ để lưu cùng với tour. Hàm này sẽ nhận vào Id của tour và danh sách các file hình ảnh phụ, sau đó lưu từng file vào thư mục trên server giống như hàm LuuHinhTourAsync, nhưng sẽ gán thứ tự cho từng hình phụ dựa trên số lượng hình đã có của tour đó trong database. Sau khi lưu xong, hàm sẽ thêm bản ghi mới vào bảng TourHinhAnhs để liên kết hình phụ với tour.
        private async Task LuuHinhPhuTourAsync(int tourId, IEnumerable<IFormFile>? files)
        {
            if (files == null) // Nếu không có file nào được upload thì dừng.

            {
                return;
            }
            // Lọc ra danh sách file hợp lệ, loại bỏ các file null hoặc có kích thước bằng 0.
            var danhSachFile = files.Where(x => x != null && x.Length > 0).ToList();
            // Nếu sau khi lọc không còn file nào hợp lệ thì dừng.
            if (!danhSachFile.Any())
            {
                return;
            }
            // Đoạn này lấy thứ tự ảnh phụ lớn nhất hiện tại của tour. Nếu tour đã có 3 hình phụ thì thứ tự lớn nhất là 3. Khi thêm hình phụ mới, thứ tự sẽ được tăng lên từ giá trị lớn nhất này để đảm bảo thứ tự của hình phụ luôn tăng dần và không bị trùng lặp.
            int thuTu = _context.TourHinhAnhs
                .Where(x => x.TourId == tourId)
                .Select(x => (int?)x.ThuTu)
                .Max() ?? 0;

            // Duyệt từng ảnh phụ được upload. 
            foreach (var file in danhSachFile)
            {
                thuTu++; // Tăng thứ tự ảnh lên 1. 
                var fileName = await LuuHinhTourAsync(file); // Gọi lại hàm LuuHinhTourAsync() để lưu file ảnh vào thư mục. 

                // Thêm thông tin ảnh phụ vào bảng TourHinhAnhs. Mỗi bản ghi sẽ liên kết với tour thông qua TourId, lưu tên file ảnh phụ và thứ tự của ảnh phụ đó.
                _context.TourHinhAnhs.Add(new TourHinhAnh
                {
                    TourId = tourId,
                    TenFile = fileName,
                    ThuTu = thuTu
                });
            }
        }

        // Hàm này hiển thị danh sách tour trong trang quản trị. Admin có thể tìm kiếm tour theo tên, địa điểm, phương tiện, loại tour, loại địa hình hoặc phong cách. Admin cũng có thể lọc tour theo loại tour.
        public IActionResult Index(string search, string loaiTour) // Action này hiển thị danh sách tour trong trang quản trị. Hai tham số search: từ khóa tìm kiếm. loaiTour: loại tour cần lọc, ví dụ “Trong nước” hoặc “Ngoài nước”.
        {
            if (!LaAdmin()) // Nếu người dùng hiện tại không phải là admin, chuyển hướng họ đến trang đăng nhập để bảo vệ chức năng quản trị.
            {
                return RedirectToAction("DangNhap", "TaiKhoan"); // Chuyển hướng người dùng đến action DangNhap trong controller TaiKhoan để yêu cầu họ đăng nhập trước khi truy cập trang quản trị.
            }
            // Đoạn này truy vấn danh sách tour từ database, bao gồm các thông tin liên quan như địa điểm, danh mục, phương tiện, loại địa hình và phong cách. Sau đó áp dụng các bộ lọc tìm kiếm nếu admin nhập vào ô tìm kiếm hoặc chọn loại tour.
            var ds = _context.Tours // Truy vấn tất cả tour từ database.
         // Sử dụng Include để lấy thông tin liên quan từ các bảng khác như DiaDiem, DanhMuc, PhuongTien, LoaiDiaHinh và PhongCach. Điều này giúp tránh lỗi khi hiển thị thông tin liên quan trong view mà không cần phải truy vấn thêm.
                .Include(x => x.DiaDiemInfo) 
                .Include(x => x.DanhMuc)
                .Include(x => x.PhuongTienInfo)
                //Đầu tiên tải bảng trung gian TourLoaiDiaHinhs, sau đó ThenInclude() tải thông tin từ bảng LoaiDiaHinhs.
                .Include(x => x.TourLoaiDiaHinhs)
                    .ThenInclude(x => x.LoaiDiaHinh)
                .Include(x => x.TourPhongCachs)
                    .ThenInclude(x => x.PhongCach)
                .AsQueryable(); // AsQueryable() giúp xây dựng truy vấn theo từng điều kiện. Câu SQL chỉ được thực thi khi gọi ToList() ở cuối.

            // Chỉ tìm kiếm khi người dùng có nhập từ khóa hợp lệ.
            if (!string.IsNullOrWhiteSpace(search))
            {
                ds = ds.Where(x =>
                    x.TenTour.Contains(search) || // Tìm từ khóa trong tên tour.
                    (x.DiaDiemInfo != null && x.DiaDiemInfo.TenDiaDiem.Contains(search)) || // Tìm trong tên địa điểm. Việc kiểm tra != null giúp tránh lỗi khi tour chưa có địa điểm.
                    (x.PhuongTienInfo != null && x.PhuongTienInfo.TenPhuongTien.Contains(search)) ||
                    (x.DanhMuc != null && x.DanhMuc.TenDanhMuc.Contains(search)) ||
                    x.TourLoaiDiaHinhs.Any(ldh =>
                        ldh.LoaiDiaHinh != null &&
                        ldh.LoaiDiaHinh.TenLoaiDiaHinh.Contains(search)) ||
                    x.TourPhongCachs.Any(pc => // Any() kiểm tra trong các loại địa hình của tour có loại nào chứa từ khóa không.Các điều kiện dùng toán tử ||, nghĩa là chỉ cần khớp một trong các trường.
                        pc.PhongCach != null &&
                        pc.PhongCach.TenPhongCach.Contains(search))
                );
            }
            // Nếu người dùng chọn loại tour, hệ thống chỉ lấy những tour thuộc danh mục đó.
            if (!string.IsNullOrWhiteSpace(loaiTour))
            {
                ds = ds.Where(x =>
                    x.DanhMuc != null &&
                    x.DanhMuc.TenDanhMuc == loaiTour
                );
            }
            // Lưu lại từ khóa và loại tour đã chọn để giao diện có thể hiển thị lại sau khi tìm kiếm.
            ViewBag.Search = search;
            ViewBag.LoaiTour = loaiTour;
            // ToList() thực thi truy vấn và chuyển kết quả thành danh sách tour để gửi sang View Index.cshtml.
            return View(ds.ToList());
        }

        // Đây là action mở trang thêm tour.
        public IActionResult Create()
        {
            // Chỉ admin mới được mở trang thêm tour.
            if (!LaAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            NapDuLieuDropdown(); // Nạp danh mục, địa điểm, phương tiện, địa hình, phong cách và số ngày cho các ô chọn trên form.
            return View(); // Trả về giao diện Create.cshtml.
        }

        // Action này nhận dữ liệu khi admin nhấn nút lưu tour.
        [HttpPost] // action nhận dữ liệu được gửi từ form.
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Tour tour) // Tour tour: ASP.NET tự ánh xạ dữ liệu form vào đối tượng Tour. async Task<IActionResult>: action có thao tác bất đồng bộ như lưu ảnh và lưu database.
        {
            if (!LaAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            NapDuLieuDropdown(); // Nạp lại dữ liệu cho dropdown để nếu có lỗi thì form vẫn hiển thị đúng các ô chọn.
            DongBoDuLieuTour(tour); // DongBoDuLieuTour() kiểm tra, chuẩn hóa danh mục, địa điểm, phương tiện, địa hình và phong cách.

            // Kiểm tra xem admin đã chọn ít nhất một loại địa hình và một phong cách chưa. Nếu chưa chọn thì thêm lỗi vào ModelState để hiển thị thông báo lỗi trên form.
            if (tour.LoaiDiaHinhDaChon == null || !tour.LoaiDiaHinhDaChon.Any())
            {
                ModelState.AddModelError("LoaiDiaHinh", "Vui lòng chọn địa hình");
            }
            // Tương tự với phong cách, nếu admin chưa chọn phong cách nào thì thêm lỗi vào ModelState.
            if (tour.PhongCachDaChon == null || !tour.PhongCachDaChon.Any())
            {
                ModelState.AddModelError("PhongCach", "Vui lòng chọn phong cách");
            }
            // Nếu admin không nhập mô tả cho tour, tự động gán mô tả mặc định và loại bỏ lỗi liên quan đến mô tả để form có thể được gửi đi.
            if (string.IsNullOrWhiteSpace(tour.MoTa))
            {
                tour.MoTa = "Chưa cập nhật mô tả";
                ModelState.Remove("MoTa");
            }
            // Kiểm tra xem admin đã chọn hình đại diện cho tour chưa. Nếu chưa chọn thì thêm lỗi vào ModelState để yêu cầu admin chọn hình ảnh.
            if (tour.HinhFile == null)
            {
                ModelState.AddModelError("HinhFile", "Vui lòng chọn hình ảnh");
            }
            KiemTraAnhTour(tour);
            // Nếu có bất kỳ lỗi nào, hệ thống không lưu database mà trả lại form cùng dữ liệu đã nhập và thông báo lỗi.
            if (!ModelState.IsValid)
            {
                return View(tour);
            }
            // Lưu hình đại diện của tour lên server và lấy tên file để lưu vào database.
            tour.HinhAnh = await LuuHinhTourAsync(tour.HinhFile!);
            // Nếu địa điểm đã tồn tại thì gán DiaDiemId. Nếu chưa có thì tạo địa điểm mới rồi liên kết với tour.
            GanDiaDiemChoTour(tour);
            // Thêm tour vào bảng Tours và lưu database.
            _context.Tours.Add(tour);
            await _context.SaveChangesAsync();
            // Lưu các hình phụ của tour lên server và liên kết với tour trong bảng TourHinhAnhs.
            await LuuHinhPhuTourAsync(tour.Id, tour.HinhPhuFiles);
            // Lưu các loại địa hình và phong cách đã chọn vào bảng trung gian TourLoaiDiaHinhs và TourPhongCachs để liên kết với tour.
            if (tour.LoaiDiaHinhDaChon != null && tour.LoaiDiaHinhDaChon.Any())
            {
                var loaiDiaHinhIds = _context.LoaiDiaHinhs
                    .Where(x => tour.LoaiDiaHinhDaChon.Contains(x.TenLoaiDiaHinh))
                    .Select(x => x.Id)
                    .ToList();
                // Duyệt qua các Id loại địa hình đã chọn và thêm bản ghi vào bảng TourLoaiDiaHinhs để liên kết với tour.
                foreach (var id in loaiDiaHinhIds)
                {
                    _context.TourLoaiDiaHinhs.Add(new TourLoaiDiaHinh
                    {
                        TourId = tour.Id,
                        LoaiDiaHinhId = id
                    });
                }
            }
            
            if (tour.PhongCachDaChon != null && tour.PhongCachDaChon.Any())
            {
                var phongCachIds = _context.PhongCachs
                    .Where(x => tour.PhongCachDaChon.Contains(x.TenPhongCach))
                    .Select(x => x.Id)
                    .ToList();
                // Duyệt qua các Id phong cách đã chọn và thêm bản ghi vào bảng TourPhongCachs để liên kết với tour.
                foreach (var id in phongCachIds)
                {
                    _context.TourPhongCachs.Add(new TourPhongCach
                    {
                        TourId = tour.Id,
                        PhongCachId = id
                    });
                }
            }
            // Lưu tất cả thay đổi vào database sau khi đã thêm tour, hình phụ, loại địa hình và phong cách.
            await _context.SaveChangesAsync();
            // Sau khi lưu xong, chuyển hướng admin về trang danh sách tour để thấy tour mới vừa thêm.
            return RedirectToAction("Index");
        }

        // Đây là action mở trang sửa tour. Khi admin nhấn nút sửa ở một tour nào đó, action này sẽ được gọi với Id của tour đó để lấy thông tin chi tiết và hiển thị trên form sửa.
        public IActionResult Edit(int id) // Action này nhận vào Id của tour cần sửa. Đầu tiên kiểm tra quyền admin, nếu không phải admin thì chuyển hướng đến trang đăng nhập. Nếu là admin, truy vấn tour từ database theo Id, bao gồm các thông tin liên quan như địa điểm, danh mục, phương tiện, loại địa hình và phong cách. Nếu không tìm thấy tour thì trả về lỗi NotFound. Nếu tìm thấy tour, hệ thống sẽ chuẩn bị dữ liệu cho form sửa bằng cách nạp danh mục, địa điểm, phương tiện, loại địa hình và phong cách vào dropdown. Cuối cùng trả về view "Sua.cshtml" cùng với dữ liệu tour để hiển thị trên form sửa.
        {
            if (!LaAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            var tour = _context.Tours // _context.Tours.Include(...) lấy tour cùng các dữ liệu liên quan:
                .Include(x => x.DiaDiemInfo)
                .Include(x => x.DanhMuc)
                .Include(x => x.PhuongTienInfo)
                .Include(x => x.TourHinhAnhs)
                .FirstOrDefault(x => x.Id == id); // Tìm tour theo Id. Nếu không tìm thấy thì tour sẽ là null.
            if (tour == null)
            {
                return NotFound();
            }

            // Đoạn này lấy danh sách loại địa hình và phong cách đã chọn của tour từ các bảng trung gian TourLoaiDiaHinhs và TourPhongCachs. Nếu tour đã có liên kết với loại địa hình hoặc phong cách nào thì gán vào LoaiDiaHinhDaChon và PhongCachDaChon để hiển thị trên form sửa. Nếu tour chưa có liên kết nào nhưng có dữ liệu trong trường LoaiDiaHinh hoặc PhongCach (dạng chuỗi), thì tách chuỗi đó ra thành danh sách để hiển thị.
            tour.LoaiDiaHinhDaChon = _context.TourLoaiDiaHinhs
                .Include(x => x.LoaiDiaHinh)
                .Where(x => x.TourId == id)
                .Select(x => x.LoaiDiaHinh != null ? x.LoaiDiaHinh.TenLoaiDiaHinh : "")
                .Where(x => x != "")
                .ToList();

            tour.PhongCachDaChon = _context.TourPhongCachs
                .Include(x => x.PhongCach)
                .Where(x => x.TourId == id)
                .Select(x => x.PhongCach != null ? x.PhongCach.TenPhongCach : "")
                .Where(x => x != "")
                .ToList();
            // Nếu tour chưa có liên kết nào với loại địa hình nhưng trường LoaiDiaHinh có dữ liệu dạng chuỗi, thì tách chuỗi đó ra thành danh sách để hiển thị. Tương tự với phong cách.
            if (!tour.LoaiDiaHinhDaChon.Any() && !string.IsNullOrEmpty(tour.LoaiDiaHinh))
            {
                tour.LoaiDiaHinhDaChon = tour.LoaiDiaHinh
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .ToList();
            }

            if (!tour.PhongCachDaChon.Any() && !string.IsNullOrEmpty(tour.PhongCach))
            {
                tour.PhongCachDaChon = tour.PhongCach
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .ToList();
            }
            // Gán tên danh mục, địa điểm và phương tiện cho tour để hiển thị trên form sửa. Nếu tour đã có liên kết với danh mục, địa điểm hoặc phương tiện nào thì gán tên đó. Nếu tour chưa có liên kết nào nhưng có dữ liệu trong trường LoaiTour, DiaDiem hoặc PhuongTien, thì gán dữ liệu đó sau khi đã trim.
            if (tour.PhuongTienInfo != null)
            {
                tour.PhuongTien = tour.PhuongTienInfo.TenPhuongTien;
            }
            // Nạp các danh sách lựa chọn và gửi dữ liệu tour sang Sua.cshtml.
            NapDuLieuDropdown();
            return View("Sua", tour);
        }

        // Hàm nhận dữ liệu người dùng gửi từ form sửa tour. Khi admin chỉnh sửa thông tin tour và nhấn nút lưu, action này sẽ được gọi với dữ liệu tour đã được bind từ form. Hàm sẽ kiểm tra quyền admin, sau đó nạp lại dữ liệu cho dropdown và đồng bộ dữ liệu tour như trong hàm Create. Sau đó kiểm tra các lỗi tương tự như trong Create, nếu có lỗi thì trả về form sửa cùng với dữ liệu đã nhập và thông báo lỗi. Nếu không có lỗi, hàm sẽ tìm tour cũ trong database theo Id, cập nhật các trường thông tin của tour cũ bằng dữ liệu mới từ form, xử lý lại các loại địa hình và phong cách đã chọn bằng cách xóa các liên kết cũ và thêm các liên kết mới vào bảng trung gian, lưu hình đại diện mới nếu có, lưu các hình phụ mới nếu có, rồi lưu tất cả thay đổi vào database. Cuối cùng chuyển hướng admin về trang danh sách tour.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Tour tour)
        {
            if (!LaAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }
            // Nạp lại dropdown nếu có lỗi và phải trả về form.
            // Đồng bộ danh mục, địa điểm, phương tiện, địa hình và phong cách.
            NapDuLieuDropdown();
            DongBoDuLieuTour(tour);
            // Kiểm tra lỗi tương tự như trong Create. Nếu có lỗi thì trả về form sửa cùng với dữ liệu đã nhập và thông báo lỗi.
            if (tour.LoaiDiaHinhDaChon == null || !tour.LoaiDiaHinhDaChon.Any())
            {
                ModelState.AddModelError("LoaiDiaHinh", "Vui lòng chọn địa hình");
            }
            // Nếu admin chưa chọn phong cách nào thì thêm lỗi vào ModelState.
            if (tour.PhongCachDaChon == null || !tour.PhongCachDaChon.Any())
            {
                ModelState.AddModelError("PhongCach", "Vui lòng chọn phong cách");
            }
            // Nếu admin không nhập mô tả cho tour, tự động gán mô tả mặc định và loại bỏ lỗi liên quan đến mô tả để form có thể được gửi đi.
            if (string.IsNullOrWhiteSpace(tour.MoTa))
            {
                tour.MoTa = "Chưa cập nhật mô tả";
                ModelState.Remove("MoTa");
            }
            KiemTraAnhTour(tour);
            if (!ModelState.IsValid)
            {
                return View("Sua", tour);
            }
            // Tìm tour cũ trong database theo Id để cập nhật thông tin. Nếu không tìm thấy tour thì trả về lỗi NotFound.
            var tourCu = _context.Tours.FirstOrDefault(x => x.Id == tour.Id);
            // Nếu tour cũ không tồn tại trong database, trả về lỗi NotFound.
            if (tourCu == null)
            {
                return NotFound();
            }
            // Cập nhật các trường thông tin của tour cũ bằng dữ liệu mới từ form. Các trường như TenTour, DiaDiemId, DanhMucId, PhuongTienId, DiaDiem, LoaiTour, PhuongTien, Gia, MoTa, LoaiDiaHinh và PhongCach sẽ được cập nhật theo dữ liệu mới mà admin đã nhập trên form sửa.
            tourCu.TenTour = tour.TenTour;
            tourCu.DiaDiemId = tour.DiaDiemId;
            tourCu.DanhMucId = tour.DanhMucId;
            tourCu.PhuongTienId = tour.PhuongTienId;

            tourCu.DiaDiem = tour.DiaDiem;
            tourCu.LoaiTour = tour.LoaiTour;
            tourCu.PhuongTien = tour.PhuongTien;

            tourCu.Gia = tour.Gia;
            tourCu.MoTa = tour.MoTa;
            tourCu.LoaiDiaHinh = tour.LoaiDiaHinh;
            tourCu.PhongCach = tour.PhongCach;
            tourCu.SoNgay = tour.SoNgay;
            // Xử lý lại các loại địa hình đã chọn. Đầu tiên, hệ thống sẽ xóa các liên kết cũ giữa tour và loại địa hình trong bảng trung gian TourLoaiDiaHinhs để chuẩn bị cho việc thêm các liên kết mới dựa trên lựa chọn hiện tại của admin. Sau đó, nếu admin đã chọn loại địa hình nào đó, hệ thống sẽ tìm Id của các loại địa hình đó trong bảng LoaiDiaHinhs dựa trên tên loại địa hình mà admin đã chọn. Cuối cùng, hệ thống sẽ duyệt qua các Id loại địa hình đã tìm được và thêm bản ghi mới vào bảng TourLoaiDiaHinhs để liên kết tour với các loại địa hình mới mà admin đã chọn.
            var loaiDiaHinhCu = _context.TourLoaiDiaHinhs
                .Where(x => x.TourId == tourCu.Id)
                .ToList();

            _context.TourLoaiDiaHinhs.RemoveRange(loaiDiaHinhCu);// Xóa các liên kết cũ giữa tour và loại địa hình trong bảng trung gian TourLoaiDiaHinhs để chuẩn bị cho việc thêm các liên kết mới dựa trên lựa chọn hiện tại của admin.
            // Nếu admin đã chọn loại địa hình nào đó, hệ thống sẽ tìm Id của các loại địa hình đó trong bảng LoaiDiaHinhs dựa trên tên loại địa hình mà admin đã chọn. Sau đó, hệ thống sẽ duyệt qua các Id loại địa hình đã tìm được và thêm bản ghi mới vào bảng TourLoaiDiaHinhs để liên kết tour với các loại địa hình mới mà admin đã chọn.
            if (tour.LoaiDiaHinhDaChon != null && tour.LoaiDiaHinhDaChon.Any()) // Kiểm tra xem admin đã chọn loại địa hình nào đó chưa. Nếu đã chọn thì tiếp tục xử lý. Nếu chưa chọn thì không cần thêm liên kết nào và tour sẽ không có loại địa hình nào liên kết.
            {
                // Tìm Id của các loại địa hình mà admin đã chọn trong bảng LoaiDiaHinhs dựa trên tên loại địa hình. Nếu có loại địa hình nào trong database có tên giống như tên loại địa hình mà admin chọn, thì lấy Id của loại địa hình đó để liên kết với tour. Nếu không tìm thấy loại địa hình nào có tên giống như vậy, thì sẽ không có Id nào được lấy và tour sẽ không được liên kết với loại địa hình đó.
                var loaiDiaHinhIds = _context.LoaiDiaHinhs
                    .Where(x => tour.LoaiDiaHinhDaChon.Contains(x.TenLoaiDiaHinh))
                    .Select(x => x.Id)
                    .ToList();
                // Duyệt qua các Id loại địa hình đã tìm được và thêm bản ghi mới vào bảng TourLoaiDiaHinhs để liên kết tour với các loại địa hình mới mà admin đã chọn. Mỗi bản ghi sẽ chứa TourId để liên kết với tour và LoaiDiaHinhId để liên kết với loại địa hình.
                foreach (var id in loaiDiaHinhIds)
                {
                    _context.TourLoaiDiaHinhs.Add(new TourLoaiDiaHinh// Thêm bản ghi mới vào bảng TourLoaiDiaHinhs để liên kết tour với loại địa hình mới mà admin đã chọn. Mỗi bản ghi sẽ chứa TourId để liên kết với tour và LoaiDiaHinhId để liên kết với loại địa hình.
                    {
                        TourId = tourCu.Id,
                        LoaiDiaHinhId = id
                    });
                }
            }
            // Tương tự với phong cách, hệ thống sẽ xóa các liên kết cũ giữa tour và phong cách trong bảng trung gian TourPhongCachs, rồi thêm các liên kết mới dựa trên lựa chọn hiện tại của admin.
            var phongCachCu = _context.TourPhongCachs
                .Where(x => x.TourId == tourCu.Id)
                .ToList();

            _context.TourPhongCachs.RemoveRange(phongCachCu);

            if (tour.PhongCachDaChon != null && tour.PhongCachDaChon.Any())
            {
                var phongCachIds = _context.PhongCachs
                    .Where(x => tour.PhongCachDaChon.Contains(x.TenPhongCach))
                    .Select(x => x.Id)
                    .ToList();

                foreach (var id in phongCachIds)
                {
                    _context.TourPhongCachs.Add(new TourPhongCach
                    {
                        TourId = tourCu.Id,
                        PhongCachId = id
                    });
                }
            }
            // Nếu admin đã chọn một hình đại diện mới cho tour, hệ thống sẽ lưu hình đó lên server và cập nhật tên file hình mới vào trường HinhAnh của tour. Nếu admin không chọn hình đại diện mới thì giữ nguyên hình đại diện cũ.
            if (tour.HinhFile != null && tour.HinhFile.Length > 0)
            {
                tourCu.HinhAnh = await LuuHinhTourAsync(tour.HinhFile); // 
            }

            await LuuHinhPhuTourAsync(tourCu.Id, tour.HinhPhuFiles); // Các ảnh phụ mới được thêm vào, còn ảnh phụ cũ vẫn được giữ.
           // Lưu tất cả thay đổi vào database sau khi đã cập nhật thông tin tour, loại địa hình, phong cách và hình ảnh.
            _context.Tours.Update(tourCu);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index"); // Sau khi lưu xong, chuyển hướng admin về trang danh sách tour để thấy các thay đổi đã cập nhật.
        }
        // Hàm này dùng để xóa một hình phụ của tour. Khi admin nhấn nút xóa ở một hình phụ nào đó trong trang sửa tour, action này sẽ được gọi với Id của hình phụ đó và Id của tour để xác định chính xác hình phụ cần xóa. Hàm sẽ kiểm tra quyền admin, sau đó tìm hình phụ trong database theo Id và TourId. Nếu không tìm thấy hình phụ thì trả về lỗi NotFound. Nếu tìm thấy hình phụ, hệ thống sẽ xóa bản ghi của hình phụ đó khỏi bảng TourHinhAnhs, sau đó kiểm tra xem file hình phụ đó có tồn tại trên server không. Nếu tồn tại thì xóa file đó khỏi thư mục wwwroot/images/dulich để giải phóng dung lượng lưu trữ. Cuối cùng chuyển hướng admin về trang sửa tour để thấy rằng hình phụ đã bị xóa.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XoaHinhTour(int id, int tourId)
        {
            if (!LaAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }
            
            var hinh = _context.TourHinhAnhs.FirstOrDefault(x => x.Id == id && x.TourId == tourId); // Tìm hình phụ trong database theo Id và TourId để đảm bảo rằng hình phụ đó thuộc về tour đang được sửa. Nếu không tìm thấy hình phụ nào thỏa mãn điều kiện này thì trả về lỗi NotFound.

            if (hinh == null)
            {
                return NotFound();
            }
            // Xóa bản ghi của hình phụ đó khỏi bảng TourHinhAnhs để loại bỏ liên kết giữa tour và hình phụ đó trong database.
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "dulich", hinh.TenFile);
            // Kiểm tra xem file hình phụ đó có tồn tại trên server không. Nếu tồn tại thì xóa file đó khỏi thư mục wwwroot/images/dulich để giải phóng dung lượng lưu trữ. Việc kiểm tra tồn tại trước khi xóa giúp tránh lỗi khi cố gắng xóa một file không còn tồn tại.
            _context.TourHinhAnhs.Remove(hinh);
            await _context.SaveChangesAsync();
            // Sau khi xóa bản ghi khỏi database, kiểm tra xem file hình phụ đó có tồn tại trên server không. Nếu tồn tại thì xóa file đó khỏi thư mục wwwroot/images/dulich để giải phóng dung lượng lưu trữ. Việc kiểm tra tồn tại trước khi xóa giúp tránh lỗi khi cố gắng xóa một file không còn tồn tại.
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
            // Sau khi xóa xong, chuyển hướng admin về trang sửa tour để thấy rằng hình phụ đã bị xóa.
            return RedirectToAction("Edit", new { id = tourId });
        }

        // Xóa hẳn tour: chỉ cho khi tour chưa có lịch khởi hành và chưa có đơn nào.
        // Trước đây là liên kết GET và xóa luôn cả đơn đặt của tour (xóa dây chuyền).
        // Tour đã có lịch hoặc đơn thì dùng Ngừng bán (DoiNgungBan).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            if (!LaAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }
            var tour = _context.Tours.Find(id);

            if (tour == null)
            {
                return NotFound();
            }

            // Đơn luôn gắn với một lịch, nên tour không có lịch thì cũng không có đơn
            bool coDuLieu = _context.LichKhoiHanhTours.Any(x => x.TourId == id);
            if (coDuLieu)
            {
                TempData["error"] = "Tour đã có lịch khởi hành hoặc đơn đặt, không xóa được. Hãy chọn Ngừng bán.";
                return RedirectToAction("Index");
            }

            _context.Tours.Remove(tour);
            _context.SaveChanges();

            TempData["success"] = "Đã xóa tour.";
            return RedirectToAction("Index");
        }

        // Ngừng bán hoặc mở bán lại một tour. Tour ngừng bán không hiện với khách và không đặt được,
        // nhưng đơn cũ, lịch và báo cáo vẫn giữ nguyên.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DoiNgungBan(int id)
        {
            if (!LaAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }
            var tour = _context.Tours.Find(id);

            if (tour == null)
            {
                return NotFound();
            }

            tour.NgungBan = !tour.NgungBan;
            _context.SaveChanges();

            TempData["success"] = tour.NgungBan
                ? $"Đã ngừng bán tour {tour.TenTour}."
                : $"Đã mở bán lại tour {tour.TenTour}.";
            return RedirectToAction("Index");
        }

        // Danh sách đơn chưa thanh toán (đơn đã thanh toán xem ở Doanh thu). Mọi giá trị đọc qua TruyVanDon.ChonDon:
        // tour và ngày đi từ lịch, số người và tiền từ ghế, trạng thái thanh toán từ giao dịch.
        public IActionResult DonDatTour(string search)
        {
            if (!LaAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            var ds = _context.DatTours.AsNoTracking().ChonDon(_context)
                .Where(x => x.TrangThaiThanhToan != TrangThaiThanhToanDon.DaThanhToan);

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                // Đơn có hành khách trùng tên, số giấy tờ, hoặc nội dung chuyển khoản của ghế (TV-XXXXXX-3, đọc từ sao kê)
                var donTheoHanhKhach = _context.HanhKhachDatTours
                    .Where(h => (h.HoTen != null && h.HoTen.Contains(search))
                        || (h.SoGiayTo != null && h.SoGiayTo.Contains(search))
                        || (h.MaThanhToan != null && h.MaThanhToan.Contains(search)))
                    .Select(h => h.DatTourId);

                ds = ds.Where(x =>
                    x.TenKhach.Contains(search) ||
                    x.DienThoai.Contains(search) ||
                    x.Email.Contains(search) ||
                    x.MaChuyenDi.Contains(search) ||
                    x.TrangThaiThanhToan.Contains(search) ||
                    x.PhuongThucThanhToan.Contains(search) ||
                    x.TenTour.Contains(search) ||
                    donTheoHanhKhach.Contains(x.Id));
            }

            ViewBag.Search = search;
            var list = ds.OrderByDescending(x => x.Id).ToList();
            NapTienDoNhom(list);
            return View(list);
        }

        // Đơn nhóm trong danh sách: số ghế đã trả trên số ghế còn hiệu lực, để admin thấy tiến độ ngay (N6)
        private void NapTienDoNhom(List<DonDatTourDoc> list)
        {
            var dsNhomId = list.Where(x => x.LoaiDon == "Nhom").Select(x => x.Id).ToList();
            ViewBag.TienDoNhom = _context.HanhKhachDatTours.AsNoTracking()
                .Where(h => dsNhomId.Contains(h.DatTourId) && h.NgayNha == null)
                .Select(h => new { h.DatTourId, DaTra = _context.GiaoDichThanhToans.Any(g => g.HanhKhachDatTourId == h.Id && g.TrangThai == "ThanhCong") })
                .AsEnumerable()
                .GroupBy(x => x.DatTourId)
                .ToDictionary(g => g.Key, g => (DaTra: g.Count(x => x.DaTra), Tong: g.Count()));
        }

        // Đơn chưa thanh toán của một tour (nút "Khách đặt" ở Quản lý tour)
        public IActionResult DonDatTourTheoTour(int id)
        {
            if (!LaAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            var list = _context.DatTours.AsNoTracking()
                .Where(x => x.LichKhoiHanhTour != null && x.LichKhoiHanhTour.TourId == id)
                .ChonDon(_context)
                .Where(x => x.TrangThaiThanhToan != TrangThaiThanhToanDon.DaThanhToan)
                .OrderByDescending(x => x.Id)
                .ToList();

            NapTienDoNhom(list);
            return View("DonDatTour", list);
        }

        // Trang chi tiết đơn (giữ tên action SuaDonDatTour để các liên kết cũ vẫn chạy)
        public IActionResult SuaDonDatTour(int id)
        {
            if (!LaAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            var don = _context.DatTours.AsNoTracking().Where(x => x.Id == id).ChonDon(_context).FirstOrDefault();
            if (don == null)
            {
                return NotFound();
            }

            var lich = don.LichKhoiHanhTourId.HasValue
                ? _context.LichKhoiHanhTours.AsNoTracking().Include(x => x.Tour).FirstOrDefault(x => x.Id == don.LichKhoiHanhTourId.Value)
                : null;

            var model = new ChiTietDonViewModel
            {
                Don = don,
                DsGhe = _context.HanhKhachDatTours.AsNoTracking().Where(h => h.DatTourId == id).OrderBy(h => h.Id).ToList(),
                // Đơn cá nhân: giao dịch gắn với đơn. Đơn nhóm: giao dịch gắn với từng ghế (N4).
                DsGiaoDich = _context.GiaoDichThanhToans.AsNoTracking().Include(g => g.NguoiTra)
                    .Where(g => g.DatTourId == id || (g.HanhKhachDatTour != null && g.HanhKhachDatTour.DatTourId == id))
                    .OrderBy(g => g.Id).ToList(),
                GiaNguoiLon = lich?.GiaNguoiLonHienThi ?? 0,
                GiaTreEm = lich?.GiaTreEmHienThi ?? 0,
                SoChoCon = lich?.SoChoCon ?? 0,
                DuocDoiSoNguoi = lich != null && !don.DaHuy && !don.DaThanhToan && lich.ChoPhepDatTour && don.LoaiDon != "Nhom",
                ChoTheoGhe = _context.ChoNgoiChuyenDis.AsNoTracking()
                    .Where(c => c.HanhKhachDatTour!.DatTourId == id)
                    .Select(c => new { c.HanhKhachDatTourId, c.ViTriChoNgoi!.MaViTri })
                    .ToDictionary(c => c.HanhKhachDatTourId, c => c.MaViTri)
            };

            if (don.LoaiDon == "Nhom")
            {
                // Đơn nhóm quá hạn thì xử lý trước (gia hạn, nhả, chốt, hủy) để admin thấy đúng trạng thái (N5)
                int soGioGiaHan = HttpContext.RequestServices.GetRequiredService<IConfiguration>().GetValue("DonNhom:SoGioGiaHan", 6);
                if (XuLyDon.XuLyDonQuaHan(_context, id, soGioGiaHan) != XuLyDon.KetQuaHetHan.KhongDoi)
                {
                    return RedirectToAction("SuaDonDatTour", new { id });
                }
                var dt = _context.DatTours.AsNoTracking().First(x => x.Id == id);
                model.HanGiuCho = dt.TrangThai == "GiuCho" ? dt.HanGiuCho : null;
                model.DaGiaHan = dt.DaGiaHan;
                model.TrangThaiTheoGhe = model.DsGhe.ToDictionary(h => h.Id, h => TrangThaiGhe.Tinh(h, model.DsGiaoDich));
            }

            return View(model);
        }

        // Admin chỉ sửa thông tin liên hệ của đơn. Lịch đi không đổi; số người đổi ở DoiSoNguoiDon.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SuaDonDatTour(SuaLienHeDonModel model)
        {
            if (!LaAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            var datTour = _context.DatTours.FirstOrDefault(x => x.Id == model.Id);
            if (datTour == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(model.TenKhach) || string.IsNullOrWhiteSpace(model.DienThoai))
            {
                TempData["error"] = "Cần có tên người liên hệ và số điện thoại.";
                return RedirectToAction("SuaDonDatTour", new { id = model.Id });
            }

            datTour.TenKhach = model.TenKhach.Trim();
            datTour.DienThoai = model.DienThoai.Trim();
            datTour.Email = model.Email?.Trim() ?? string.Empty;
            datTour.DiaChi = model.DiaChi?.Trim() ?? string.Empty;
            datTour.GhiChu = model.GhiChu;
            _context.SaveChanges();

            TempData["success"] = "Đã lưu thông tin liên hệ.";
            return RedirectToAction("SuaDonDatTour", new { id = model.Id });
        }

        // Admin xác nhận đã nhận đủ tiền của đơn: giao dịch chờ thành ThanhCong (hoặc tạo giao dịch tiền mặt
        // nếu đơn cũ chưa có), đơn thành DaXacNhan. Chỉ một giao dịch thành công cho mỗi đơn (RB-05).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult XacNhanDaNhanTien(int id)
        {
            if (!LaAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            var don = _context.DatTours.AsNoTracking().Where(x => x.Id == id).ChonDon(_context).FirstOrDefault();
            var datTour = _context.DatTours.FirstOrDefault(x => x.Id == id);
            if (don == null || datTour == null)
            {
                return NotFound();
            }

            if (don.DaHuy || don.DaThanhToan)
            {
                TempData["error"] = don.DaHuy ? "Đơn đã hủy." : "Đơn này đã được xác nhận thanh toán.";
                return RedirectToAction("SuaDonDatTour", new { id });
            }
            // Đơn nhóm xác nhận theo từng ghế (XacNhanGiaoDichGhe), không xác nhận cả đơn một lần
            if (don.LoaiDon == "Nhom")
            {
                TempData["error"] = "Đơn nhóm xác nhận tiền theo từng ghế trong bảng Thanh toán.";
                return RedirectToAction("SuaDonDatTour", new { id });
            }

            var bayGio = GioVN.BayGio;
            var nguoiXacNhan = HttpContext.Session.GetInt32("taiKhoanId");
            var gdCho = _context.GiaoDichThanhToans
                .Where(g => g.DatTourId == id && g.TrangThai == "ChoXacNhan")
                .OrderByDescending(g => g.Id)
                .FirstOrDefault();

            if (gdCho != null)
            {
                gdCho.TrangThai = "ThanhCong";
                gdCho.SoTien = don.TongTien > 0 ? don.TongTien : gdCho.SoTien;
                gdCho.NgayXacNhan = bayGio;
                gdCho.NguoiXacNhanId = nguoiXacNhan;
            }
            else if (don.TongTien > 0)
            {
                _context.GiaoDichThanhToans.Add(new GiaoDichThanhToan
                {
                    DatTourId = id,
                    SoTien = don.TongTien,
                    PhuongThuc = "Tiền mặt",
                    TrangThai = "ThanhCong",
                    NgayTao = bayGio,
                    NgayXacNhan = bayGio,
                    NguoiXacNhanId = nguoiXacNhan
                });
            }

            datTour.TrangThai = "DaXacNhan";
            _context.SaveChanges();

            TempData["success"] = $"Đã xác nhận nhận {don.TongTien:N0} VNĐ cho đơn {don.MaChuyenDi}.";
            return RedirectToAction("SuaDonDatTour", new { id });
        }

        // Đơn nhóm: admin đối chiếu sao kê rồi xác nhận hoặc từ chối từng giao dịch của một ghế (N4).
        // Câu UPDATE có điều kiện "đang chờ" nên hai admin bấm cùng lúc không ghi đè nhau;
        // chỉ mục duy nhất có lọc chặn ghế có hai giao dịch thành công (RB-05).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult XacNhanGiaoDichGhe(int id, bool dongY)
        {
            if (!LaAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            var gd = _context.GiaoDichThanhToans.AsNoTracking().Include(g => g.HanhKhachDatTour)
                .FirstOrDefault(g => g.Id == id && g.HanhKhachDatTourId != null);
            if (gd?.HanhKhachDatTour == null)
            {
                return NotFound();
            }
            int donId = gd.HanhKhachDatTour.DatTourId;

            // Ghế đã nhả (hủy đơn, quá hạn) thì không nhận tiền vào ghế đó nữa, chỉ được từ chối
            if (dongY && gd.HanhKhachDatTour.NgayNha.HasValue)
            {
                TempData["error"] = "Ghế này đã bị nhả, không xác nhận được. Hãy từ chối và hoàn tiền ngoài hệ thống.";
                return RedirectToAction("SuaDonDatTour", new { id = donId });
            }

            int soDong;
            try
            {
                soDong = _context.Database.ExecuteSqlInterpolated($@"
                    UPDATE GiaoDichThanhToans
                    SET TrangThai = {(dongY ? "ThanhCong" : "ThatBai")}, NgayXacNhan = {GioVN.BayGio}, NguoiXacNhanId = {HttpContext.Session.GetInt32("taiKhoanId")}
                    WHERE Id = {id} AND TrangThai = 'ChoXacNhan'");
            }
            // Chỉ bắt lỗi trùng chỉ mục duy nhất (ghế đã có giao dịch thành công khác); lỗi khác để hiện ra
            catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number is 2601 or 2627)
            {
                TempData["error"] = "Ghế này đã có một giao dịch thành công khác. Hãy từ chối giao dịch này.";
                return RedirectToAction("SuaDonDatTour", new { id = donId });
            }

            if (soDong == 0)
            {
                TempData["error"] = "Giao dịch này đã được xử lý trước đó.";
            }
            else if (dongY)
            {
                TempData["success"] = $"Đã xác nhận nhận {gd.SoTien:N0} VNĐ cho ghế {gd.HanhKhachDatTour.MaThanhToan}.";
            }
            else
            {
                // Đơn đã quá hạn thì ghế vừa bị từ chối được xử lý ngay theo luật hết hạn, báo admin đúng kết quả
                int soGioGiaHan = HttpContext.RequestServices.GetRequiredService<IConfiguration>().GetValue("DonNhom:SoGioGiaHan", 6);
                var ketQua = XuLyDon.XuLyDonQuaHan(_context, donId, soGioGiaHan);
                TempData["success"] = $"Đã từ chối giao dịch của ghế {gd.HanhKhachDatTour.MaThanhToan}. " + (ketQua switch
                {
                    XuLyDon.KetQuaHetHan.NhaGheChoAdmin or XuLyDon.KetQuaHetHan.Chot or XuLyDon.KetQuaHetHan.Huy
                        => "Đơn đã hết hạn giữ chỗ nên ghế đã được nhả.",
                    XuLyDon.KetQuaHetHan.GiaHan => "Đơn vừa được gia hạn, người trả có thể báo chuyển khoản lại.",
                    _ => "Người trả có thể báo chuyển khoản lại."
                });
            }
            return RedirectToAction("SuaDonDatTour", new { id = donId });
        }

        // Admin hủy đơn: cùng cách xử lý với khách hủy (XuLyDon.HuyDon). Thay cho xóa đơn trước đây.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult HuyDonDatTour(int id)
        {
            if (!LaAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            var datTour = _context.DatTours.Include(x => x.HanhKhachDatTours).FirstOrDefault(x => x.Id == id);
            if (datTour == null)
            {
                return NotFound();
            }

            if (datTour.TrangThai == "DaHuy")
            {
                TempData["error"] = "Đơn này đã hủy trước đó.";
                return RedirectToAction("SuaDonDatTour", new { id });
            }

            XuLyDon.HuyDon(_context, datTour);

            TempData["success"] = "Đã hủy đơn, ghế được nhả và chỗ đã trả lại cho lịch.";
            return RedirectToAction("SuaDonDatTour", new { id });
        }

        // Đổi số người của đơn chưa thanh toán. Thêm ghế: trừ chỗ có điều kiện, ghế mới lấy giá hiện tại của lịch.
        // Bớt ghế: nhả các ghế thêm sau cùng của loại đó và trả chỗ. Tất cả trong một giao dịch CSDL.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DoiSoNguoiDon(int id, int soNguoiLon, int soTreNho)
        {
            if (!LaAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            var don = _context.DatTours.AsNoTracking().Where(x => x.Id == id).ChonDon(_context).FirstOrDefault();
            var datTour = _context.DatTours.Include(x => x.HanhKhachDatTours).FirstOrDefault(x => x.Id == id);
            if (don == null || datTour == null)
            {
                return NotFound();
            }

            // Đơn nhóm đổi ghế qua trưởng nhóm (bớt ghế) hoặc hủy đơn; thêm ghế kiểu đơn cá nhân sẽ thiếu mã thanh toán của ghế
            if (datTour.LoaiDon == "Nhom")
            {
                TempData["error"] = "Đơn nhóm không đổi số người ở đây. Trưởng nhóm bớt ghế ở trang đơn nhóm.";
                return RedirectToAction("SuaDonDatTour", new { id });
            }

            var lich = _context.LichKhoiHanhTours.Include(x => x.Tour).First(x => x.Id == datTour.LichKhoiHanhTourId);
            if (don.DaHuy || don.DaThanhToan || !lich.ChoPhepDatTour)
            {
                TempData["error"] = "Chỉ đổi được số người khi đơn chưa thanh toán và lịch còn mở bán.";
                return RedirectToAction("SuaDonDatTour", new { id });
            }

            if (soNguoiLon < 1 || soTreNho < 0)
            {
                TempData["error"] = "Cần ít nhất 1 người lớn, số trẻ nhỏ không âm.";
                return RedirectToAction("SuaDonDatTour", new { id });
            }

            var gheConLai = datTour.HanhKhachDatTours.Where(h => h.NgayNha == null).ToList();
            var gheLon = gheConLai.Where(h => h.LoaiHanhKhach != "Trẻ nhỏ").OrderByDescending(h => h.Id).ToList();
            var gheTre = gheConLai.Where(h => h.LoaiHanhKhach == "Trẻ nhỏ").OrderByDescending(h => h.Id).ToList();
            int themLon = soNguoiLon - gheLon.Count, themTre = soTreNho - gheTre.Count;
            int chenhLech = themLon + themTre;

            if (themLon == 0 && themTre == 0)
            {
                TempData["error"] = "Số người không thay đổi.";
                return RedirectToAction("SuaDonDatTour", new { id });
            }

            using (var giaoDichCsdl = _context.Database.BeginTransaction())
            {
                if (chenhLech > 0)
                {
                    int soDong = _context.Database.ExecuteSqlInterpolated(
                        $"UPDATE LichKhoiHanhTours SET SoChoCon = SoChoCon - {chenhLech} WHERE Id = {lich.Id} AND SoChoCon >= {chenhLech}");
                    if (soDong == 0)
                    {
                        giaoDichCsdl.Rollback();
                        TempData["error"] = $"Lịch chỉ còn {lich.SoChoCon} chỗ, không thêm được {chenhLech} người.";
                        return RedirectToAction("SuaDonDatTour", new { id });
                    }
                }

                var bayGio = GioVN.BayGio;
                void ThemGhe(int soLuong, string loai, decimal gia)
                {
                    for (int i = 0; i < soLuong; i++)
                    {
                        datTour.HanhKhachDatTours.Add(new HanhKhachDatTour
                        {
                            HoTen = string.Empty,
                            LoaiGiayTo = string.Empty,
                            SoGiayTo = string.Empty,
                            LoaiHanhKhach = loai,
                            GiaLucDat = gia
                        });
                    }
                }
                void NhaGhe(List<HanhKhachDatTour> ds, int soLuong)
                {
                    foreach (var ghe in ds.Take(soLuong))
                    {
                        ghe.NgayNha = bayGio;
                    }
                }

                if (themLon > 0) ThemGhe(themLon, "Người lớn", lich.GiaNguoiLonHienThi); else NhaGhe(gheLon, -themLon);
                if (themTre > 0) ThemGhe(themTre, "Trẻ nhỏ", lich.GiaTreEmHienThi); else NhaGhe(gheTre, -themTre);

                var dsNhaId = datTour.HanhKhachDatTours.Where(h => h.NgayNha == bayGio).Select(h => h.Id).ToList();
                _context.ChoNgoiChuyenDis.RemoveRange(_context.ChoNgoiChuyenDis.Where(c => dsNhaId.Contains(c.HanhKhachDatTourId)));

                // Tính lại tiền; khuyến mãi giữ số tiền đã giảm nhưng không vượt tổng mới
                decimal truocGiam = datTour.HanhKhachDatTours.Where(h => h.NgayNha == null).Sum(h => h.GiaLucDat);
                datTour.SoTienGiam = Math.Min(datTour.SoTienGiam, truocGiam);
                decimal phaiTra = truocGiam - datTour.SoTienGiam;

                foreach (var gd in _context.GiaoDichThanhToans.Where(g => g.DatTourId == id && g.TrangThai == "ChoXacNhan"))
                {
                    gd.SoTien = phaiTra > 0 ? phaiTra : gd.SoTien;
                }

                _context.SaveChanges();

                // Ghế vừa thêm được xếp chỗ trống trên xe (RB-36)
                if (chenhLech > 0 && XepChoNgoi.XepChoConThieu(_context, lich.Id) > 0)
                {
                    try
                    {
                        _context.SaveChanges();
                    }
                    catch (DbUpdateException)
                    {
                        giaoDichCsdl.Rollback();
                        TempData["error"] = "Có khách vừa chọn chỗ trên xe cùng lúc, chưa thêm được người. Bạn thử lại nhé.";
                        return RedirectToAction("SuaDonDatTour", new { id });
                    }
                }

                if (chenhLech < 0)
                {
                    _context.Database.ExecuteSqlInterpolated(
                        $"UPDATE LichKhoiHanhTours SET SoChoCon = SoChoCon + {-chenhLech} WHERE Id = {lich.Id}");
                }

                giaoDichCsdl.Commit();
            }

            TempData["success"] = $"Đã cập nhật đơn thành {soNguoiLon} người lớn, {soTreNho} trẻ nhỏ.";
            return RedirectToAction("SuaDonDatTour", new { id });
        }

        // Đây là action hiển thị danh sách tài khoản người dùng trong trang quản trị. Admin có thể tìm kiếm tài khoản theo họ tên, email hoặc số điện thoại đăng nhập, vai trò. Admin cũng có thể chỉnh sửa hoặc xóa tài khoản người dùng từ trang này.
        public IActionResult TaiKhoan(string search)
        {
            if (!LaAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            var ds = _context.TaiKhoans.AsQueryable(); // Truy vấn danh sách tài khoản từ database và cho phép áp dụng bộ lọc tìm kiếm nếu admin nhập vào ô tìm kiếm. Nếu admin nhập từ khóa tìm kiếm nào đó thì hệ thống sẽ lọc danh sách tài khoản để chỉ hiển thị những tài khoản có họ tên, email hoặc số điện thoại đăng nhập, vai trò chứa từ khóa đó. Nếu admin không nhập gì vào ô tìm kiếm thì sẽ hiển thị tất cả tài khoản.
            // Chỉ tìm kiếm khi người dùng có nhập từ khóa hợp lệ.
            if (!string.IsNullOrWhiteSpace(search))
            {
                ds = ds.Where(x =>
                    x.HoTen.Contains(search) ||
                    (x.Email != null && x.Email.Contains(search)) ||
                    (x.SoDienThoai != null && x.SoDienThoai.Contains(search)) ||
                    x.VaiTro.Contains(search)
                );
            }

            ViewBag.Search = search; // Lưu lại từ khóa đã nhập để giao diện có thể hiển thị lại sau khi tìm kiếm.

            return View(ds.ToList());
        }

        // Đây là tính năng sửa tk người dùng ( chưa sử dụng tới vì mục đích lý do bảo mật và logic của sản phầm nên chức năng này chưa sử dụng đến ) ( để đến khi qua đồ án chuyên ngành có thể sẽ dùng lại )
        public IActionResult SuaTaiKhoan(int id)
        {
            if (!LaAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            var tk = _context.TaiKhoans.FirstOrDefault(x => x.Id == id);

            if (tk == null)
            {
                return NotFound();
            }

            // Không đưa mật khẩu (kể cả bản băm) ra form
            return View(new SuaTaiKhoanViewModel
            {
                Id = tk.Id,
                HoTen = tk.HoTen,
                Email = tk.Email,
                SoDienThoai = tk.SoDienThoai,
                VaiTro = tk.VaiTro
            });
        }

        private static readonly string[] CacVaiTro = { "user", "admin", "huongdanvien", "taixe" };

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SuaTaiKhoan(SuaTaiKhoanViewModel model)
        {
            if (!LaAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            var tk = _context.TaiKhoans.FirstOrDefault(x => x.Id == model.Id);

            if (tk == null)
            {
                return NotFound();
            }

            string? email = DinhDanhTaiKhoan.ChuanHoa(model.Email);
            string? soDienThoai = DinhDanhTaiKhoan.ChuanHoa(model.SoDienThoai);

            if (email == null && soDienThoai == null)
            {
                ModelState.AddModelError("Email", "Cần ít nhất email hoặc số điện thoại để đăng nhập.");
            }
            if (email != null && !DinhDanhTaiKhoan.LaEmail(email))
            {
                ModelState.AddModelError("Email", "Email chưa đúng định dạng.");
            }
            if (soDienThoai != null && !DinhDanhTaiKhoan.LaSoDienThoai(soDienThoai))
            {
                ModelState.AddModelError("SoDienThoai", "Số điện thoại gồm 10 chữ số, bắt đầu bằng 0.");
            }
            if (email != null && _context.TaiKhoans.DaDuocDung(email, null, tk.Id))
            {
                ModelState.AddModelError("Email", "Email này đã thuộc tài khoản khác.");
            }
            if (soDienThoai != null && _context.TaiKhoans.DaDuocDung(null, soDienThoai, tk.Id))
            {
                ModelState.AddModelError("SoDienThoai", "Số điện thoại này đã thuộc tài khoản khác.");
            }
            if (!string.IsNullOrWhiteSpace(model.MatKhauMoi) && model.MatKhauMoi.Trim().Length < 6)
            {
                ModelState.AddModelError("MatKhauMoi", "Mật khẩu mới cần ít nhất 6 ký tự.");
            }
            if (!CacVaiTro.Contains(model.VaiTro))
            {
                ModelState.AddModelError("VaiTro", "Vai trò không hợp lệ.");
            }

            if (!ModelState.IsValid)
            {
                model.MatKhauMoi = null;
                return View(model);
            }

            tk.HoTen = model.HoTen.Trim();
            tk.Email = email;
            tk.SoDienThoai = soDienThoai;
            // Trước đây mật khẩu được lưu thẳng chuỗi nhập vào (không băm); giờ băm như đăng ký
            if (!string.IsNullOrWhiteSpace(model.MatKhauMoi))
            {
                tk.MatKhau = BCrypt.Net.BCrypt.HashPassword(model.MatKhauMoi.Trim());
            }
            tk.VaiTro = model.VaiTro;

            _context.SaveChanges();

            TempData["success"] = "Đã lưu tài khoản.";
            return RedirectToAction("TaiKhoan");
        }

        // Đây là action dùng để xóa một tài khoản người dùng khỏi database. Khi admin nhấn nút xóa ở một tài khoản nào đó trong trang danh sách tài khoản, action này sẽ được gọi với Id của tài khoản đó để xác định chính xác tài khoản cần xóa. Hàm sẽ kiểm tra quyền admin, sau đó tìm tài khoản trong database theo Id. Nếu không tìm thấy tài khoản thì trả về lỗi NotFound. Nếu tìm thấy tài khoản, hệ thống sẽ kiểm tra xem tài khoản đó có phải là tài khoản đang đăng nhập hay không. Nếu là tài khoản đang đăng nhập thì hệ thống sẽ không cho phép xóa và hiển thị thông báo lỗi. Nếu không phải là tài khoản đang đăng nhập thì hệ thống sẽ tiếp tục kiểm tra xem tài khoản đó có liên quan đến nhân viên nào trong bảng NhanViens hay không. Nếu có liên quan đến nhân viên nào đó mà nhân viên đó đã được phân công vào lịch tour nào đó thì hệ thống sẽ không cho phép xóa và hiển thị thông báo lỗi yêu cầu chuyển nhân viên sang trạng thái tạm nghỉ thay vì xóa. Nếu không có liên quan đến nhân viên nào hoặc có liên quan nhưng nhân viên đó chưa được phân công vào lịch tour nào thì hệ thống sẽ tiếp tục kiểm tra xem tài khoản đó có liên quan đến đơn đặt tour nào trong bảng DatTours hay không. Nếu có liên quan đến đơn đặt tour nào đó thì hệ thống sẽ không cho phép xóa và hiển thị thông báo lỗi yêu cầu giữ lại tài khoản vì đã có đơn đặt tour liên quan. Nếu không có liên quan đến đơn đặt tour nào thì hệ thống sẽ xóa bản ghi của tài khoản đó khỏi bảng TaiKhoans, đồng thời cũng sẽ xóa tất cả các liên kết và dữ liệu liên quan đến tài khoản đó trong các bảng khác như NhanViens nếu có để đảm bảo tính toàn vẹn dữ liệu. Cuối cùng chuyển hướng admin về trang danh sách tài khoản để thấy rằng tài khoản đã bị xóa hoặc hiển thị thông báo lỗi nếu không thể xóa.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult XoaTaiKhoan(int id)
        {
            if (!LaAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }
            // 
            var taiKhoan = _context.TaiKhoans
                .FirstOrDefault(x => x.Id == id);

            if (taiKhoan == null)
            {
                return NotFound();
            }

            // Không cho admin xóa chính tài khoản đang đăng nhập.
            var taiKhoanDangNhapId = HttpContext.Session.GetInt32("taiKhoanId");

            if (taiKhoanDangNhapId == id)
            {
                TempData["error"] = "Bạn không thể xóa tài khoản đang đăng nhập!";
                return RedirectToAction("TaiKhoan");
            }
            // Kiểm tra xem tài khoản này có liên quan đến nhân viên nào trong bảng NhanViens hay không. Nếu có liên quan đến nhân viên nào đó mà nhân viên đó đã được phân công vào lịch tour nào đó thì hệ thống sẽ không cho phép xóa và hiển thị thông báo lỗi yêu cầu chuyển nhân viên sang trạng thái tạm nghỉ thay vì xóa. Nếu không có liên quan đến nhân viên nào hoặc có liên quan nhưng nhân viên đó chưa được phân công vào lịch tour nào thì hệ thống sẽ tiếp tục kiểm tra xem tài khoản đó có liên quan đến đơn đặt tour nào trong bảng DatTours hay không. Nếu có liên quan đến đơn đặt tour nào đó thì hệ thống sẽ không cho phép xóa và hiển thị thông báo lỗi yêu cầu giữ lại tài khoản vì đã có đơn đặt tour liên quan. Nếu không có liên quan đến đơn đặt tour nào thì hệ thống sẽ xóa bản ghi của tài khoản đó khỏi bảng TaiKhoans, đồng thời cũng sẽ xóa tất cả các liên kết và dữ liệu liên quan đến tài khoản đó trong các bảng khác như NhanViens nếu có để đảm bảo tính toàn vẹn dữ liệu. Cuối cùng chuyển hướng admin về trang danh sách tài khoản để thấy rằng tài khoản đã bị xóa hoặc hiển thị thông báo lỗi nếu không thể xóa.
            var nhanVien = _context.NhanViens
                .FirstOrDefault(x => x.TaiKhoanId == id);
            // Nếu tài khoản này có liên quan đến một nhân viên nào đó trong bảng NhanViens thì sẽ kiểm tra xem nhân viên đó đã được phân công vào lịch tour nào đó trong bảng PhanCongTours hay chưa. Nếu đã được phân công vào lịch tour nào đó thì sẽ không cho phép xóa tài khoản này và hiển thị thông báo lỗi yêu cầu chuyển nhân viên sang trạng thái tạm nghỉ thay vì xóa. Nếu chưa được phân công vào lịch tour nào đó thì sẽ cho phép xóa tài khoản này và cũng sẽ xóa luôn bản ghi của nhân viên đó khỏi bảng NhanViens để đảm bảo tính toàn vẹn dữ liệu.
            if (nhanVien != null)
            {
                bool daDuocPhanCong = _context.PhanCongTours.Any(x =>
                    x.HuongDanVienId == nhanVien.Id ||
                    x.TaiXeId == nhanVien.Id);
                // Nếu nhân viên đó đã được phân công vào lịch tour nào đó thì sẽ không cho phép xóa tài khoản này và hiển thị thông báo lỗi yêu cầu chuyển nhân viên sang trạng thái tạm nghỉ thay vì xóa. Nếu chưa được phân công vào lịch tour nào đó thì sẽ cho phép xóa tài khoản này và cũng sẽ xóa luôn bản ghi của nhân viên đó khỏi bảng NhanViens để đảm bảo tính toàn vẹn dữ liệu.
                if (daDuocPhanCong)
                {
                    TempData["error"] =
                        "Không thể xóa nhân sự này vì đang có lịch phân công tour. Hãy chuyển nhân sự sang trạng thái tạm nghỉ.";
                    return RedirectToAction("TaiKhoan");
                }

                _context.NhanViens.Remove(nhanVien);
            }
            // Kiểm tra xem tài khoản này có liên quan đến đơn đặt tour nào trong bảng DatTours hay không. Nếu có liên quan đến đơn đặt tour nào đó thì hệ thống sẽ không cho phép xóa và hiển thị thông báo lỗi yêu cầu giữ lại tài khoản vì đã có đơn đặt tour liên quan. Nếu không có liên quan đến đơn đặt tour nào thì hệ thống sẽ xóa bản ghi của tài khoản đó khỏi bảng TaiKhoans, đồng thời cũng sẽ xóa tất cả các liên kết và dữ liệu liên quan đến tài khoản đó trong các bảng khác như NhanViens nếu có để đảm bảo tính toàn vẹn dữ liệu. Cuối cùng chuyển hướng admin về trang danh sách tài khoản để thấy rằng tài khoản đã bị xóa hoặc hiển thị thông báo lỗi nếu không thể xóa.
            // Tài khoản đã có đơn, đang giữ ghế trong đơn của người khác, hoặc đã xác nhận giao dịch
            // thì giữ lại (dữ liệu tài chính và giấy tờ không được mất chủ)
            bool coDonDatTour = _context.DatTours.Any(x => x.TaiKhoanId == id)
                || _context.HanhKhachDatTours.Any(x => x.TaiKhoanId == id)
                || _context.GiaoDichThanhToans.Any(x => x.NguoiXacNhanId == id || x.NguoiTraId == id)
                || _context.BaiViets.Any(x => x.TacGiaId == id);
            // Nếu có liên quan đến đơn đặt tour nào đó thì hệ thống sẽ không cho phép xóa và hiển thị thông báo lỗi yêu cầu giữ lại tài khoản vì đã có đơn đặt tour liên quan. Nếu không có liên quan đến đơn đặt tour nào thì hệ thống sẽ xóa bản ghi của tài khoản đó khỏi bảng TaiKhoans, đồng thời cũng sẽ xóa tất cả các liên kết và dữ liệu liên quan đến tài khoản đó trong các bảng khác như NhanViens nếu có để đảm bảo tính toàn vẹn dữ liệu. Cuối cùng chuyển hướng admin về trang danh sách tài khoản để thấy rằng tài khoản đã bị xóa hoặc hiển thị thông báo lỗi nếu không thể xóa.
            if (coDonDatTour)
            {
                TempData["error"] =
                    "Không thể xóa tài khoản vì tài khoản này đã có đơn đặt tour, ghế, giao dịch thanh toán hoặc bài viết.";
                return RedirectToAction("TaiKhoan");
            }
            // Nếu không có liên quan đến đơn đặt tour nào đó thì hệ thống sẽ xóa bản ghi của tài khoản đó khỏi bảng TaiKhoans, đồng thời cũng sẽ xóa tất cả các liên kết và dữ liệu liên quan đến tài khoản đó trong các bảng khác như NhanViens nếu có để đảm bảo tính toàn vẹn dữ liệu. Cuối cùng chuyển hướng admin về trang danh sách tài khoản để thấy rằng tài khoản đã bị xóa hoặc hiển thị thông báo lỗi nếu không thể xóa.
            try
            {
                _context.TaiKhoans.Remove(taiKhoan); // Xóa bản ghi của tài khoản đó khỏi bảng TaiKhoans để loại bỏ tài khoản đó khỏi database. Việc xóa này sẽ giúp giải phóng tài nguyên và đảm bảo rằng tài khoản đó không còn tồn tại trong hệ thống nữa.
                _context.SaveChanges();

                TempData["success"] = "Đã xóa tài khoản thành công!";
            }
            catch (DbUpdateException)
            {
                TempData["error"] =
                    "Không thể xóa tài khoản vì vẫn còn dữ liệu liên quan.";
            }

            return RedirectToAction("TaiKhoan");
        }
        // Trang Tổng quan của admin: các số liệu chính và việc cần xử lý. Chỉ đọc dữ liệu, không ghi gì xuống database.
        // Dùng truy vấn gọn (Select, Count, AsNoTracking, không Include) để tránh làm SQL Server phải xin nhiều bộ nhớ.
        public IActionResult TongQuan()
        {
            if (!LaAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            var homNay = GioVN.HomNay;
            var ngayMai = homNay.AddDays(1);
            var sau30Ngay = homNay.AddDays(30);

            // Doanh thu: cùng cách tính với trang Doanh thu (đơn đã thanh toán, theo ngày đi)
            var dsDonDaTra = _context.DatTours.AsNoTracking().ChonDon(_context)
                .Where(x => x.TrangThaiThanhToan == TrangThaiThanhToanDon.DaThanhToan && x.NgayDi.Year == homNay.Year)
                .Select(x => new { x.NgayDi, x.TongTien })
                .ToList();
            var doanhThuTheoThang = dsDonDaTra
                .GroupBy(x => x.NgayDi.Month)
                .Select(g => new { Thang = g.Key, DoanhThu = g.Sum(d => d.TongTien), SoDon = g.Count() })
                .ToList();

            var thangNay = doanhThuTheoThang.FirstOrDefault(x => x.Thang == homNay.Month);

            // Tháng trước (có thể thuộc năm trước nên truy vấn riêng)
            var dauThangTruoc = new DateTime(homNay.Year, homNay.Month, 1).AddMonths(-1);
            var dauThangNay = dauThangTruoc.AddMonths(1);
            decimal doanhThuThangTruoc = _context.DatTours.AsNoTracking().ChonDon(_context)
                .Where(x => x.TrangThaiThanhToan == TrangThaiThanhToanDon.DaThanhToan && x.NgayDi >= dauThangTruoc && x.NgayDi < dauThangNay)
                .Select(x => x.TongTien)
                .ToList()
                .Sum();

            // Đơn đặt trong 30 ngày qua, đếm theo trạng thái thanh toán
            var tu30NgayTruoc = homNay.AddDays(-30);
            var demTrangThai = _context.DatTours.AsNoTracking().ChonDon(_context)
                .Where(x => x.NgayDat >= tu30NgayTruoc)
                .Select(x => x.TrangThaiThanhToan)
                .ToList()
                .GroupBy(x => x)
                .Select(g => new { TrangThai = g.Key, SoDon = g.Count() })
                .ToList();
            int Dem(string trangThai) => demTrangThai.FirstOrDefault(x => x.TrangThai == trangThai)?.SoDon ?? 0;

            var donDangKiemTra = _context.DatTours.AsNoTracking().ChonDon(_context)
                .Where(x => x.TrangThaiThanhToan == TrangThaiThanhToanDon.DangKiemTra);
            var donChoXacNhan = donDangKiemTra
                .OrderByDescending(x => x.Id)
                .Select(x => new DonChoXacNhanItem
                {
                    Id = x.Id,
                    TenKhach = x.TenKhach,
                    TenTour = x.TenTour,
                    NgayDi = x.NgayDi,
                    TongTien = x.TongTien,
                    PhuongThucThanhToan = x.PhuongThucThanhToan
                })
                .Take(5)
                .ToList();

            var lichSapKhoiHanhQuery = _context.LichKhoiHanhTours
                .AsNoTracking()
                .Where(x => x.TrangThai != TrangThaiLich.DaHuy && x.NgayKhoiHanh >= homNay && x.NgayKhoiHanh <= sau30Ngay);

            var tourThieuLichQuery = _context.Tours
                .AsNoTracking()
                .Where(t => !_context.LichKhoiHanhTours.Any(l =>
                    l.TourId == t.Id && l.TrangThai != TrangThaiLich.DaHuy && l.NgayKhoiHanh >= ngayMai));

            var model = new TongQuanAdminViewModel
            {
                Thang = homNay.Month,
                Nam = homNay.Year,
                DoanhThuThang = thangNay?.DoanhThu ?? 0,
                SoDonDaThanhToanThang = thangNay?.SoDon ?? 0,
                DoanhThuThangTruoc = doanhThuThangTruoc,
                DonDaThanhToan30Ngay = Dem(TrangThaiThanhToanDon.DaThanhToan),
                DonDangKiemTra30Ngay = Dem(TrangThaiThanhToanDon.DangKiemTra),
                DonChuaThanhToan30Ngay = Dem(TrangThaiThanhToanDon.ChuaThanhToan),
                DonDaHuy30Ngay = Dem(TrangThaiThanhToanDon.DaHuy),
                SoDonChoXacNhan = donDangKiemTra.Count(),
                SoLichSapKhoiHanh = lichSapKhoiHanhQuery.Count(),
                SoTour = _context.Tours.Count(),
                TinNhanChuaDoc = _context.TinNhanLienHes.Count(x =>
                    x.NguoiGui != "admin" && !x.DaDoc),
                DoanhThu12Thang = Enumerable.Range(1, 12)
                    .Select(m => doanhThuTheoThang.FirstOrDefault(x => x.Thang == m)?.DoanhThu ?? 0)
                    .ToList(),
                DonChoXacNhan = donChoXacNhan,
                LichSapKhoiHanh = lichSapKhoiHanhQuery
                    .OrderBy(x => x.NgayKhoiHanh)
                    .Select(x => new LichSapKhoiHanhItem
                    {
                        Id = x.Id,
                        TourId = x.TourId,
                        TenTour = x.Tour != null ? x.Tour.TenTour : "Chưa cập nhật",
                        NgayKhoiHanh = x.NgayKhoiHanh,
                        SoChoCon = x.SoChoCon
                    })
                    .Take(5)
                    .ToList(),
                SoTourThieuLich = tourThieuLichQuery.Count(),
                TourThieuLich = tourThieuLichQuery
                    .OrderBy(t => t.Id)
                    .Select(t => new TourThieuLichItem { Id = t.Id, TenTour = t.TenTour })
                    .Take(6)
                    .ToList()
            };

            // Số chỗ mở bán của các lịch sắp đi để hiện "đã đặt X/Y" (RB-37)
            var moBan = SucChuaLich.SoChoMoBan(_context, model.LichSapKhoiHanh.Select(l => l.Id));
            foreach (var l in model.LichSapKhoiHanh)
            {
                l.SoChoMoBan = moBan.TryGetValue(l.Id, out var mb) ? mb : 0;
            }

            return View(model);
        }
        // Đây là action hiển thị báo cáo doanh thu trong trang quản trị. Admin có thể xem tổng doanh thu, số đơn đã thanh toán, trung bình đơn, doanh thu theo năm và tour bán chạy nhất trong tháng. Admin cũng có thể lọc báo cáo theo tháng và năm để xem doanh thu của tháng đó hoặc năm đó.
        public IActionResult DoanhThu(int? thang, int? nam)
        {
            if (!LaAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }
            int thangChon = thang ?? GioVN.HomNay.Month;
            int namChon = nam ?? GioVN.HomNay.Year;
            // Đơn đã thanh toán (có giao dịch thành công), tính theo ngày đi của lịch
            var query = _context.DatTours.AsNoTracking().ChonDon(_context)
                .Where(x => x.TrangThaiThanhToan == TrangThaiThanhToanDon.DaThanhToan);
            var danhSachDon = query
                .Where(x => x.NgayDi.Month == thangChon &&
                            x.NgayDi.Year == namChon)
                .OrderByDescending(x => x.NgayDi)
                .ToList();
            var namOptions = query
                .Select(x => x.NgayDi.Year)
                .Distinct()
                .OrderByDescending(x => x)
                .ToList();
            if (!namOptions.Contains(namChon))
            {
                namOptions.Add(namChon);
                namOptions = namOptions
                    .OrderByDescending(x => x)
                    .ToList();
            }
            decimal tongDoanhThu = danhSachDon.Sum(x => x.TongTien);
            int soDonDaThanhToan = danhSachDon.Count;
            decimal trungBinhDon = soDonDaThanhToan > 0
                ? tongDoanhThu / soDonDaThanhToan
                : 0;
            // TongTien là tổng tính từ ghế, SQL Server không SUM lồng được nên cộng sau khi lấy về
            decimal doanhThuNam = query
                .Where(x => x.NgayDi.Year == namChon)
                .Select(x => x.TongTien)
                .AsEnumerable()
                .Sum();
            var tourBanChay = danhSachDon
                .GroupBy(x => x.TenTour)
                .Select(x => new
                {
                    TenTour = x.Key,
                    SoDon = x.Count()
                })
                .OrderByDescending(x => x.SoDon)
                .FirstOrDefault();
            var thongKeTheoThang = query
                .Where(x => x.NgayDi.Year == namChon)
                .Select(x => new { x.NgayDi, x.TongTien })
                .AsEnumerable()
                .GroupBy(x => x.NgayDi.Month)
                .Select(x => new
                {
                    Thang = x.Key,
                    DoanhThu = x.Sum(d => d.TongTien),
                    SoDon = x.Count()
                })
                .ToList();
            var doanhThuTheoThang = thongKeTheoThang
                .ToDictionary(x => x.Thang, x => x.DoanhThu);
            var soDonTheoThang = thongKeTheoThang
                .ToDictionary(x => x.Thang, x => x.SoDon);
            ViewBag.ThangChon = thangChon;
            ViewBag.NamChon = namChon;
            ViewBag.NamOptions = namOptions;
            ViewBag.TongDoanhThu = tongDoanhThu;
            ViewBag.SoDonDaThanhToan = soDonDaThanhToan;
            ViewBag.TrungBinhDon = trungBinhDon;
            ViewBag.DoanhThuNam = doanhThuNam;
            ViewBag.TourBanChay = tourBanChay != null
                ? $"{tourBanChay.TenTour} ({tourBanChay.SoDon} đơn)"
                : "Chưa có dữ liệu";
            ViewBag.BieuDoNhan = Enumerable.Range(1, 12)
                .Select(x => $"T{x}")
                .ToList();
            ViewBag.BieuDoDoanhThu = Enumerable.Range(1, 12)
                .Select(x => doanhThuTheoThang.ContainsKey(x)
                    ? doanhThuTheoThang[x]
                    : 0)
                .ToList();
            ViewBag.BieuDoSoDon = Enumerable.Range(1, 12)
                .Select(x => soDonTheoThang.ContainsKey(x)
                    ? soDonTheoThang[x]
                    : 0)
                .ToList();
            return View(danhSachDon);
        }
        // Đây là action dùng để xuất báo cáo doanh thu ra file Excel (dạng CSV) trong trang quản trị. Khi admin nhấn nút xuất Excel trong trang báo cáo doanh thu, action này sẽ được gọi với tháng và năm đã chọn để lọc dữ liệu. Hàm sẽ kiểm tra quyền admin, sau đó truy vấn danh sách đơn đặt tour đã thanh toán từ database, bao gồm các thông tin liên quan như tour, lịch khởi hành và phương tiện. Sau đó áp dụng bộ lọc theo tháng và năm mà admin chọn để chỉ lấy những đơn đặt tour có ngày đi thuộc tháng và năm đó. Tiếp theo, hệ thống sẽ tạo nội dung file CSV bằng cách xây dựng một chuỗi với các dòng dữ liệu được phân tách bằng dấu phẩy. Mỗi dòng sẽ chứa các thông tin như STT, khách hàng, tour, ngày đi, giờ đi, phương tiện, phương thức thanh toán, trạng thái và doanh thu của từng đơn đặt tour. Cuối cùng hệ thống sẽ trả về file CSV dưới dạng tệp đính kèm để admin có thể tải về máy tính của mình.
        public IActionResult XuatDoanhThuExcel(int? thang, int? nam)
        {
            if (!LaAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }
            // Đoạn này truy vấn danh sách đơn đặt tour đã thanh toán từ database, bao gồm các thông tin liên quan như tour, lịch khởi hành và phương tiện. Sau đó áp dụng bộ lọc theo tháng và năm mà admin chọn để chỉ lấy những đơn đặt tour có ngày đi thuộc tháng và năm đó. Tiếp theo, hệ thống sẽ tạo nội dung file CSV bằng cách xây dựng một chuỗi với các dòng dữ liệu được phân tách bằng dấu phẩy. Mỗi dòng sẽ chứa các thông tin như STT, khách hàng, tour, ngày đi, giờ đi, phương tiện, phương thức thanh toán, trạng thái và doanh thu của từng đơn đặt tour. Cuối cùng hệ thống sẽ trả về file CSV dưới dạng tệp đính kèm để admin có thể tải về máy tính của mình.
            int thangChon = thang ?? GioVN.HomNay.Month;
            int namChon = nam ?? GioVN.HomNay.Year;
            // 
            var danhSachDon = _context.DatTours.AsNoTracking().ChonDon(_context)
                .Where(x =>
                    x.TrangThaiThanhToan == TrangThaiThanhToanDon.DaThanhToan &&
                    x.NgayDi.Month == thangChon &&
                    x.NgayDi.Year == namChon)
                .OrderByDescending(x => x.NgayDi)// Sắp xếp theo ngày đi giảm dần để những đơn đặt tour mới nhất sẽ hiển thị ở đầu danh sách và cũng sẽ xuất ra file Excel ở đầu tiên. Điều này giúp admin dễ dàng xem và phân tích các đơn đặt tour mới nhất trong tháng đó.
                .ToList();
            // Hàm phụ để xử lý việc đóng gói giá trị thành định dạng CSV, bao gồm việc thêm dấu ngoặc kép và xử lý các ký tự đặc biệt như dấu phẩy hoặc dấu ngoặc kép trong dữ liệu.
            string Csv(string? value)
            {
                return "\"" + (value ?? "").Replace("\"", "\"\"") + "\"";
            }

            var sb = new StringBuilder();// Tạo một StringBuilder để xây dựng nội dung của file CSV. StringBuilder giúp tối ưu hiệu suất khi phải nối nhiều chuỗi lại với nhau, đặc biệt là khi có nhiều dòng dữ liệu cần thêm vào file CSV.
            // Thêm dòng tiêu đề vào file CSV để mô tả các cột dữ liệu. Dòng tiêu đề này sẽ giúp người dùng hiểu được ý nghĩa của từng cột khi mở file CSV trong Excel hoặc trình xem văn bản.
            sb.AppendLine("STT,Khách hàng,Tour,Ngày đi,Giờ đi,Phương tiện,Phương thức thanh toán,Trạng thái,Doanh thu");
            // Biến đếm để đánh số thứ tự cho từng đơn đặt tour trong file CSV. Biến này sẽ được tăng lên sau mỗi lần thêm một dòng dữ liệu mới vào file CSV, giúp tạo ra cột STT (Số thứ tự) cho mỗi đơn đặt tour.
            int stt = 1;
            // Duyệt qua từng đơn đặt tour trong danh sách đã được lọc theo tháng và năm, sau đó xây dựng một dòng dữ liệu cho mỗi đơn đặt tour với các thông tin như STT, khách hàng, tour, ngày đi, giờ đi, phương tiện, phương thức thanh toán, trạng thái và doanh thu. Mỗi dòng dữ liệu sẽ được phân tách bằng dấu phẩy để tạo thành định dạng CSV. Các giá trị sẽ được xử lý qua hàm Csv để đảm bảo rằng các ký tự đặc biệt được xử lý đúng cách trong file CSV.
            foreach (var item in danhSachDon)
            {
                string gioDi = item.GioKhoiHanh.HasValue
                    ? item.GioKhoiHanh.Value.ToString(@"hh\:mm")
                    : "Chưa cập nhật";

                sb.AppendLine(string.Join(",", new[]
                {
            stt.ToString(),
            Csv(item.TenKhach),
            Csv(item.TenTour),
            Csv(item.NgayDi.ToString("dd/MM/yyyy")),
            Csv(gioDi),
            Csv(item.PhuongTien),
            Csv(item.PhuongThucThanhToan),
            Csv(item.TrangThaiThanhToan),
            item.TongTien.ToString("N0")
        }));

                stt++;
            }
            // Để đảm bảo rằng file CSV được mã hóa đúng cách khi mở trong Excel, đặc biệt là với các ký tự tiếng Việt có dấu, hệ thống sẽ thêm một preamble UTF-8 vào đầu nội dung file CSV. Preamble này sẽ giúp Excel nhận biết rằng file CSV đang sử dụng mã hóa UTF-8 và hiển thị đúng các ký tự đặc biệt trong dữ liệu.
            var preamble = Encoding.UTF8.GetPreamble();
            var content = Encoding.UTF8.GetBytes(sb.ToString());
            var bytes = preamble.Concat(content).ToArray();

            string fileName = $"BaoCaoDoanhThu_{thangChon}_{namChon}.csv";

            return File(bytes, "text/csv; charset=utf-8", fileName);
        }

        public IActionResult BaoCaoDoanhThuPdf(int? thang, int? nam)
        {
            if (!LaAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            int thangChon = thang ?? GioVN.HomNay.Month;
            int namChon = nam ?? GioVN.HomNay.Year;

            var danhSachDon = _context.DatTours.AsNoTracking().ChonDon(_context)
                .Where(x =>
                    x.TrangThaiThanhToan == TrangThaiThanhToanDon.DaThanhToan &&
                    x.NgayDi.Month == thangChon &&
                    x.NgayDi.Year == namChon)
                .OrderByDescending(x => x.NgayDi)
                .ToList();

            ViewBag.ThangChon = thangChon;
            ViewBag.NamChon = namChon;
            ViewBag.TongDoanhThu = danhSachDon.Sum(x => x.TongTien);
            ViewBag.SoDonDaThanhToan = danhSachDon.Count;

            return View(danhSachDon);
        }

        // Đây là action hiển thị trang quản lý lịch khởi hành của các tour trong trang quản trị. Admin có thể xem danh sách các tour cùng với số lượng lịch khởi hành của từng tour. Admin cũng có thể tìm kiếm tour theo tên hoặc địa điểm để dễ dàng quản lý lịch khởi hành của các tour đó.
        public IActionResult QuanLyLichKhoiHanh(string search)
        {
            if (!LaAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }
            // Đoạn này truy vấn danh sách các tour từ database, bao gồm các thông tin liên quan như địa điểm và phương tiện. Sau đó áp dụng bộ lọc tìm kiếm theo tên tour hoặc tên địa điểm nếu admin nhập vào ô tìm kiếm. Tiếp theo, hệ thống sẽ tính toán số lượng lịch khởi hành của từng tour bằng cách nhóm các lịch khởi hành theo TourId và đếm số lượng lịch khởi hành trong mỗi nhóm. Cuối cùng trả về view QuanLyLichKhoiHanh.cshtml cùng với danh sách tour đã được lọc và số lượng lịch khởi hành của từng tour để hiển thị trên trang quản lý lịch khởi hành.
            var ds = _context.Tours
                .Include(x => x.DiaDiemInfo)
                .Include(x => x.PhuongTienInfo)
                .AsQueryable();
            // Chỉ tìm kiếm khi người dùng có nhập từ khóa hợp lệ.
            if (!string.IsNullOrWhiteSpace(search))
            {
                ds = ds.Where(x =>
                    x.TenTour.Contains(search) ||
                    (x.DiaDiemInfo != null && x.DiaDiemInfo.TenDiaDiem.Contains(search)));
            }
            // Lấy số lượng lịch khởi hành của từng tour bằng cách nhóm các lịch khởi hành theo TourId và đếm số lượng lịch khởi hành trong mỗi nhóm. Kết quả sẽ được lưu vào ViewBag.SoLichTheoTour dưới dạng một dictionary có key là TourId và value là số lượng lịch khởi hành của tour đó. Điều này sẽ giúp giao diện dễ dàng hiển thị số lượng lịch khởi hành tương ứng với từng tour trong danh sách.
            ViewBag.Search = search;

            ViewBag.SoLichTheoTour = _context.LichKhoiHanhTours
                .GroupBy(x => x.TourId)
                .ToDictionary(x => x.Key, x => x.Count());

            return View(ds.OrderBy(x => x.TenTour).ToList());
        }
        // bên ngoài 
        // Đây là action hiển thị trang quản lý lịch trình của một tour cụ thể trong trang quản trị. Admin có thể xem danh sách các lịch trình của tour đó, bao gồm ngày thứ, tiêu đề, nội dung và bữa ăn. Admin cũng có thể tìm kiếm lịch trình theo tiêu đề hoặc tên địa điểm để dễ dàng quản lý các lịch trình của tour đó.
        public IActionResult QuanLyLichTrinh(string search)
        {
            if (!LaAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }
            // Đoạn này truy vấn danh sách các tour từ database, bao gồm các thông tin liên quan như địa điểm và phương tiện. Sau đó áp dụng bộ lọc tìm kiếm theo tên tour hoặc tên địa điểm nếu admin nhập vào ô tìm kiếm. Tiếp theo, hệ thống sẽ tính toán số lượng lịch trình của từng tour bằng cách nhóm các lịch trình theo TourId và đếm số lượng lịch trình trong mỗi nhóm. Cuối cùng trả về view QuanLyLichTrinh.cshtml cùng với danh sách tour đã được lọc và số lượng lịch trình của từng tour để hiển thị trên trang quản lý lịch trình.
            var ds = _context.Tours
                .Include(x => x.DiaDiemInfo)
                .Include(x => x.PhuongTienInfo)
                .AsQueryable();
            // Chỉ tìm kiếm khi người dùng có nhập từ khóa hợp lệ.
            if (!string.IsNullOrWhiteSpace(search))
            {
                ds = ds.Where(x =>
                    x.TenTour.Contains(search) ||
                    (x.DiaDiemInfo != null && x.DiaDiemInfo.TenDiaDiem.Contains(search)));
            }

            ViewBag.Search = search;

            ViewBag.SoLichTrinhTheoTour = _context.LichTrinhTours
                .GroupBy(x => x.TourId)
                .ToDictionary(x => x.Key, x => x.Count());

            return View(ds.OrderBy(x => x.TenTour).ToList());
        }
        // bên trong 
        // Đây là action hiển thị danh sách các lịch trình của một tour cụ thể trong trang quản trị. Admin có thể xem chi tiết các lịch trình của tour đó, bao gồm ngày thứ, tiêu đề, nội dung và bữa ăn. Admin cũng có thể thêm mới hoặc xóa các lịch trình của tour đó từ trang này.
        public IActionResult LichTrinh(int id)
        {
            if (!LaAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            var tour = _context.Tours
                .Include(x => x.DiaDiemInfo)
                .FirstOrDefault(x => x.Id == id);

            if (tour == null)
            {
                return NotFound();
            }

            ViewBag.Tour = tour;

            var ds = _context.LichTrinhTours
                .Where(x => x.TourId == id)
                .OrderBy(x => x.NgayThu)
                .ToList();

            return View(ds);
        }
        // Đây là action dùng để thêm mới một lịch trình cho một tour cụ thể trong trang quản trị. Khi admin điền thông tin ngày thứ, tiêu đề, nội dung và bữa ăn vào form thêm lịch trình và nhấn nút lưu, action này sẽ được gọi với các thông tin đó cùng với Id của tour để xác định tour cần thêm lịch trình. Hàm sẽ kiểm tra quyền admin, sau đó sẽ tạo một đối tượng LichTrinhTour mới với các thông tin đã nhận được từ form. Đối tượng này sẽ được thêm vào database và lưu lại để đảm bảo rằng lịch trình mới đã được tạo thành công. Cuối cùng chuyển hướng admin về trang danh sách lịch trình của tour đó để thấy rằng lịch trình mới đã xuất hiện trong danh sách.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ThemLichTrinh(int tourId, int ngayThu, string tieuDe, string? buaAn, string? noiDung, string? toaDo)
        {
            if (!LaAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            if (ngayThu <= 0 || string.IsNullOrWhiteSpace(tieuDe))
            {
                TempData["error"] = "Vui lòng nhập đầy đủ ngày thứ và tiêu đề lịch trình.";
                return RedirectToAction("LichTrinh", new { id = tourId });
            }

            if (!LichTrinhTour.DocToaDo(toaDo, out var viDo, out var kinhDo))
            {
                TempData["error"] = ThongBaoToaDoSai;
                return RedirectToAction("LichTrinh", new { id = tourId });
            }
            // Tạo một đối tượng LichTrinhTour mới với các thông tin đã nhận được từ form. Đối tượng này sẽ được thêm vào database và lưu lại để đảm bảo rằng lịch trình mới đã được tạo thành công. Cuối cùng chuyển hướng admin về trang danh sách lịch trình của tour đó để thấy rằng lịch trình mới đã xuất hiện trong danh sách.
            var lichTrinh = new LichTrinhTour
            {
                TourId = tourId,
                NgayThu = ngayThu,
                TieuDe = tieuDe.Trim(),
                BuaAn = buaAn,
                NoiDung = noiDung,
                ViDo = viDo,
                KinhDo = kinhDo
            };
            // Thêm đối tượng LichTrinhTour mới vào database và lưu lại để đảm bảo rằng lịch trình mới đã được tạo thành công. Cuối cùng chuyển hướng admin về trang danh sách lịch trình của tour đó để thấy rằng lịch trình mới đã xuất hiện trong danh sách.
            _context.LichTrinhTours.Add(lichTrinh);
            XoaDuongDiNgaySau(tourId, ngayThu);
            _context.SaveChanges();
            await LamMoiDuongDi(tourId);

            return RedirectToAction("LichTrinh", new { id = tourId }); // Chuyển hướng admin về trang danh sách lịch trình của tour đó để thấy rằng lịch trình mới đã xuất hiện trong danh sách.
        }

        private const string ThongBaoToaDoSai = "Tọa độ chưa đúng. Hãy dán đúng dạng vĩ độ, kinh độ như 11.9404, 108.4583, điểm phải ở trong Việt Nam.";

        // Ngày sau đi tiếp từ điểm của ngày này, nên điểm đổi thì đường của ngày sau cũng phải lấy lại
        private void XoaDuongDiNgaySau(int tourId, int ngayThu)
        {
            var ngaySau = _context.LichTrinhTours
                .Where(x => x.TourId == tourId && x.NgayThu > ngayThu)
                .OrderBy(x => x.NgayThu)
                .FirstOrDefault();
            if (ngaySau != null)
            {
                ngaySau.DuongDi = null;
            }
        }

        // Lấy đường xe chạy cho các ngày còn thiếu. Lỗi dịch vụ không chặn việc lưu tọa độ, chỉ báo cho admin biết.
        private async Task LamMoiDuongDi(int tourId)
        {
            var tour = _context.Tours.FirstOrDefault(x => x.Id == tourId);
            if (tour == null)
            {
                return;
            }

            var dsNgay = _context.LichTrinhTours.Where(x => x.TourId == tourId).OrderBy(x => x.NgayThu).ToList();
            decimal? viTruoc = tour.ViDoXuatPhat, kinhTruoc = tour.KinhDoXuatPhat;
            int thatBai = 0, daGoi = 0;

            foreach (var ngay in dsNgay)
            {
                bool coHaiDau = viTruoc.HasValue && kinhTruoc.HasValue && ngay.ViDo.HasValue && ngay.KinhDo.HasValue;
                bool cungCho = coHaiDau && viTruoc == ngay.ViDo && kinhTruoc == ngay.KinhDo;
                if (coHaiDau && !cungCho && string.IsNullOrEmpty(ngay.DuongDi))
                {
                    // Máy chủ công khai: gọi cách nhau một chút để không làm phiền
                    if (daGoi++ > 0)
                    {
                        await Task.Delay(500);
                    }
                    ngay.DuongDi = await webdulich.Services.DuongXeChay.LayAsync(kinhTruoc!.Value, viTruoc!.Value, ngay.KinhDo!.Value, ngay.ViDo!.Value);
                    if (ngay.DuongDi == null)
                    {
                        thatBai++;
                    }
                }
                viTruoc = ngay.ViDo;
                kinhTruoc = ngay.KinhDo;
            }

            _context.SaveChanges();
            if (thatBai > 0)
            {
                TempData["error"] = $"Chưa lấy được đường xe chạy cho {thatBai} ngày, bản đồ tạm nối bằng nét đứt. Bấm Lưu lại ở ngày đó để thử lấy lại.";
            }
        }

        // Lưu hoặc xóa điểm xuất phát của tour
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CapNhatDiemXuatPhat(int id, string? toaDo)
        {
            if (!LaAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            var tour = _context.Tours.FirstOrDefault(x => x.Id == id);
            if (tour == null)
            {
                return NotFound();
            }

            if (!LichTrinhTour.DocToaDo(toaDo, out var viDo, out var kinhDo))
            {
                TempData["error"] = ThongBaoToaDoSai;
                return RedirectToAction("LichTrinh", new { id });
            }

            tour.ViDoXuatPhat = viDo;
            tour.KinhDoXuatPhat = kinhDo;
            XoaDuongDiNgaySau(id, 0);
            _context.SaveChanges();
            await LamMoiDuongDi(id);

            TempData["success"] = viDo.HasValue ? "Đã lưu điểm xuất phát." : "Đã xóa điểm xuất phát.";
            return RedirectToAction("LichTrinh", new { id });
        }

        // Lưu hoặc xóa điểm tới cuối ngày. Tour có đủ điểm xuất phát và điểm mọi ngày thì trang tour hiện bản đồ thật.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CapNhatToaDoLichTrinh(int id, string? toaDo)
        {
            if (!LaAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            var lichTrinh = _context.LichTrinhTours.FirstOrDefault(x => x.Id == id);
            if (lichTrinh == null)
            {
                return NotFound();
            }

            if (!LichTrinhTour.DocToaDo(toaDo, out var viDo, out var kinhDo))
            {
                TempData["error"] = ThongBaoToaDoSai;
                return RedirectToAction("LichTrinh", new { id = lichTrinh.TourId });
            }

            lichTrinh.ViDo = viDo;
            lichTrinh.KinhDo = kinhDo;
            lichTrinh.DuongDi = null;
            XoaDuongDiNgaySau(lichTrinh.TourId, lichTrinh.NgayThu);
            _context.SaveChanges();
            await LamMoiDuongDi(lichTrinh.TourId);

            TempData["success"] = viDo.HasValue
                ? $"Đã lưu tọa độ ngày {lichTrinh.NgayThu}."
                : $"Đã xóa tọa độ ngày {lichTrinh.NgayThu}.";
            return RedirectToAction("LichTrinh", new { id = lichTrinh.TourId });
        }
        // Đây là action dùng để xóa một lịch trình của một tour cụ thể trong trang quản trị. Khi admin nhấn nút xóa ở một lịch trình nào đó trong trang danh sách lịch trình của tour, action này sẽ được gọi với Id của lịch trình đó để xác định chính xác lịch trình cần xóa. Hàm sẽ kiểm tra quyền admin, sau đó tìm lịch trình trong database theo Id. Nếu không tìm thấy lịch trình thì trả về lỗi NotFound. Nếu tìm thấy lịch trình, hệ thống sẽ lấy Id của tour mà lịch trình đó thuộc về để có thể chuyển hướng lại đúng trang danh sách lịch trình của tour đó sau khi xóa. Sau đó hệ thống sẽ xóa bản ghi của lịch trình đó khỏi database và lưu lại để đảm bảo rằng lịch trình đã bị xóa thành công. Cuối cùng chuyển hướng admin về trang danh sách lịch trình của tour đó để thấy rằng lịch trình đã bị xóa khỏi danh sách.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XoaLichTrinh(int id)
        {
            if (!LaAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }
            // Tìm lịch trình trong database theo Id. Nếu không tìm thấy lịch trình thì trả về lỗi NotFound. Nếu tìm thấy lịch trình, hệ thống sẽ lấy Id của tour mà lịch trình đó thuộc về để có thể chuyển hướng lại đúng trang danh sách lịch trình của tour đó sau khi xóa. Sau đó hệ thống sẽ xóa bản ghi của lịch trình đó khỏi database và lưu lại để đảm bảo rằng lịch trình đã bị xóa thành công. Cuối cùng chuyển hướng admin về trang danh sách lịch trình của tour đó để thấy rằng lịch trình đã bị xóa khỏi danh sách.
            var lichTrinh = _context.LichTrinhTours.FirstOrDefault(x => x.Id == id);

            if (lichTrinh == null)
            {
                return NotFound();
            }

            int tourId = lichTrinh.TourId;

            XoaDuongDiNgaySau(tourId, lichTrinh.NgayThu);
            _context.LichTrinhTours.Remove(lichTrinh);
            _context.SaveChanges();
            await LamMoiDuongDi(tourId);

            return RedirectToAction("LichTrinh", new { id = tourId });
        }
        // Đây là action hiển thị danh sách các lịch khởi hành của một tour cụ thể trong trang quản trị. Admin có thể xem chi tiết các lịch khởi hành của tour đó, bao gồm ngày khởi hành, giờ khởi hành, số chỗ còn, giá người lớn, giá trẻ em và trạng thái hiển thị. Admin cũng có thể lọc danh sách lịch khởi hành theo trạng thái hiển thị như sắp khởi hành, đang diễn ra, đã hoàn thành hoặc đã hủy để dễ dàng quản lý các lịch khởi hành của tour đó.
        public IActionResult LichKhoiHanh(int id, string trangThai = "SapKhoiHanh")
        {
            if (!LaAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            var tour = _context.Tours.FirstOrDefault(x => x.Id == id);

            if (tour == null)
            {
                return NotFound();
            }

            var dsTatCa = _context.LichKhoiHanhTours
                .Include(x => x.Tour)
                .Include(x => x.SoDoChoNgoi)
                .Where(x => x.TourId == id)
                .OrderBy(x => x.NgayKhoiHanh)
                .ToList();

            ViewBag.Tour = tour;
            ViewBag.TrangThai = trangThai;

            ViewBag.CountSapKhoiHanh = dsTatCa.Count(x => x.TrangThaiHienThi == "Sắp khởi hành");
            ViewBag.CountDangDienRa = dsTatCa.Count(x => x.TrangThaiHienThi == "Đang diễn ra");
            ViewBag.CountDaHoanThanh = dsTatCa.Count(x => x.TrangThaiHienThi == "Đã hoàn thành");
            ViewBag.CountDaHuy = dsTatCa.Count(x => x.TrangThaiHienThi == "Đã hủy");
            ViewBag.CountTatCa = dsTatCa.Count;

            var tenTrangThai = trangThai switch
            {
                "DangDienRa" => "Đang diễn ra",
                "DaHoanThanh" => "Đã hoàn thành",
                "DaHuy" => "Đã hủy",
                "TatCa" => "",
                _ => "Sắp khởi hành"
            };

            var dsHienThi = trangThai == "TatCa"
                ? dsTatCa
                : dsTatCa.Where(x => x.TrangThaiHienThi == tenTrangThai).ToList();

            // Xe chọn được cho lịch: các sơ đồ của đúng phương tiện của tour (RB-14), và số chỗ mở bán của từng lịch (RB-37)
            ViewBag.DsXe = DanhSachXe(tour.PhuongTienId);
            ViewBag.SoChoMoBan = SucChuaLich.SoChoMoBan(_context, dsHienThi.Select(x => x.Id));

            return View(dsHienThi);
        }

        // Trang chọn chỗ mở bán của một lịch (RB-37): admin bấm vào từng ghế để mở hoặc khóa.
        // Chỗ đã có khách luôn mở và không bấm được.
        public IActionResult ChoMoBan(int id)
        {
            if (!LaAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            var lich = _context.LichKhoiHanhTours
                .Include(x => x.Tour).ThenInclude(t => t!.PhuongTienInfo)
                .Include(x => x.SoDoChoNgoi)
                .FirstOrDefault(x => x.Id == id);
            if (lich == null || lich.SoDoChoNgoi == null)
            {
                return NotFound();
            }

            var coKhach = _context.ChoNgoiChuyenDis.Where(c => c.LichKhoiHanhTourId == id).Select(c => c.ViTriChoNgoiId).ToHashSet();
            var khoa = SucChuaLich.ViTriKhoa(_context, id);
            var model = new ChonChoNgoiViewModel
            {
                TenTour = lich.Tour?.TenTour ?? string.Empty,
                NgayDi = lich.NgayKhoiHanh,
                TenSoDo = lich.SoDoChoNgoi.TenSoDo,
                LaMayBay = lich.Tour?.PhuongTienInfo?.TenPhuongTien?.StartsWith("Máy bay") == true,
                SoHang = lich.SoDoChoNgoi.SoHang,
                SoCot = lich.SoDoChoNgoi.SoCot,
                DatTourId = lich.Id,
                DsViTri = _context.ViTriChoNgois
                    .Where(v => v.SoDoChoNgoiId == lich.SoDoChoNgoi.Id)
                    .OrderBy(v => v.Hang).ThenBy(v => v.Cot)
                    .Select(v => new ViTriHienThi { Id = v.Id, MaViTri = v.MaViTri, Hang = v.Hang, Cot = v.Cot })
                    .ToList()
            };
            foreach (var v in model.DsViTri)
            {
                v.DaCoNguoi = coKhach.Contains(v.Id);
                v.KhongMoBan = khoa.Contains(v.Id);
            }

            ViewBag.Lich = lich;
            ViewBag.DangGiu = SucChuaLich.SoGheDangGiu(_context, id);
            ViewBag.ChoSua = lich.TrangThaiHienThi == "Sắp khởi hành";
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChoMoBan(int id, List<int>? moBan)
        {
            if (!LaAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            var lich = _context.LichKhoiHanhTours.Include(x => x.Tour).FirstOrDefault(x => x.Id == id);
            if (lich == null || !lich.SoDoChoNgoiId.HasValue)
            {
                return NotFound();
            }
            if (lich.TrangThaiHienThi != "Sắp khởi hành")
            {
                TempData["error"] = "Chỉ đổi chỗ mở bán của lịch sắp khởi hành.";
                return RedirectToAction("ChoMoBan", new { id });
            }

            var dsViTri = _context.ViTriChoNgois.Where(v => v.SoDoChoNgoiId == lich.SoDoChoNgoiId.Value).Select(v => v.Id).ToList();
            var coKhach = _context.ChoNgoiChuyenDis.Where(c => c.LichKhoiHanhTourId == id).Select(c => c.ViTriChoNgoiId).ToHashSet();
            // Chỗ mở bán: các ô admin tick (chỉ nhận vị trí thuộc xe của lịch) cộng các chỗ đã có khách
            var mo = new HashSet<int>((moBan ?? new List<int>()).Where(dsViTri.Contains));
            mo.UnionWith(coKhach);
            var khoaMoi = dsViTri.Where(v => !mo.Contains(v)).ToList();

            int dangGiu = SucChuaLich.SoGheDangGiu(_context, id);
            if (mo.Count < Math.Max(1, dangGiu))
            {
                TempData["error"] = $"Phải mở bán ít nhất {Math.Max(1, dangGiu)} chỗ vì lịch đã có {dangGiu} khách giữ chỗ.";
                return RedirectToAction("ChoMoBan", new { id });
            }

            using (var giaoDichCsdl = _context.Database.BeginTransaction())
            {
                _context.ChoKhongMoBans.RemoveRange(_context.ChoKhongMoBans.Where(k => k.LichKhoiHanhTourId == id));
                _context.SaveChanges();
                _context.ChoKhongMoBans.AddRange(khoaMoi.Select(v => new ChoKhongMoBan { LichKhoiHanhTourId = id, ViTriChoNgoiId = v }));
                _context.SaveChanges();

                // Khách vừa chọn đúng một chỗ đang bị khóa trong lúc admin lưu: không lưu, báo admin thử lại
                bool trung = _context.ChoNgoiChuyenDis.Any(c => c.LichKhoiHanhTourId == id && khoaMoi.Contains(c.ViTriChoNgoiId));
                if (trung)
                {
                    giaoDichCsdl.Rollback();
                    TempData["error"] = "Có khách vừa chọn một chỗ bạn định khóa. Trang đã tải lại, bạn kiểm tra rồi lưu lại nhé.";
                    return RedirectToAction("ChoMoBan", new { id });
                }

                try
                {
                    SucChuaLich.TinhLaiSoChoCon(_context, id);
                }
                catch (Exception)
                {
                    // CHECK số chỗ còn không âm chặn: có khách vừa giữ thêm chỗ
                    giaoDichCsdl.Rollback();
                    TempData["error"] = "Số khách giữ chỗ vừa thay đổi, chưa lưu được. Bạn thử lại nhé.";
                    return RedirectToAction("ChoMoBan", new { id });
                }
                giaoDichCsdl.Commit();
            }

            TempData["success"] = $"Đã lưu: mở bán {mo.Count} trên {dsViTri.Count} chỗ.";
            return RedirectToAction("LichKhoiHanh", new { id = lich.TourId });
        }

        // Một xe (sơ đồ chỗ ngồi) chọn được cho lịch
        public record XeChon(int Id, string TenSoDo, int SoViTri);

        // Các sơ đồ của một phương tiện, nhỏ đến lớn
        private List<XeChon> DanhSachXe(int? phuongTienId) =>
            phuongTienId.HasValue
                ? _context.SoDoChoNgois
                    .Where(s => s.PhuongTienId == phuongTienId.Value)
                    .Select(s => new XeChon(s.Id, s.TenSoDo, s.ViTriChoNgois.Count))
                    .AsEnumerable()
                    .OrderBy(s => s.SoViTri).ThenBy(s => s.Id)
                    .ToList()
                : new List<XeChon>();
        // Đây là action dùng để thêm mới một lịch khởi hành cho một tour cụ thể trong trang quản trị. Khi admin điền thông tin ngày khởi hành, giờ khởi hành, số chỗ còn, giá người lớn và giá trẻ em vào form thêm lịch khởi hành và nhấn nút lưu, action này sẽ được gọi với các thông tin đó cùng với Id của tour để xác định tour cần thêm lịch khởi hành. Hàm sẽ kiểm tra quyền admin, sau đó sẽ tạo một đối tượng LichKhoiHanhTour mới với các thông tin đã nhận được từ form. Đối tượng này sẽ được thêm vào database và lưu lại để đảm bảo rằng lịch khởi hành mới đã được tạo thành công. Cuối cùng chuyển hướng admin về trang danh sách lịch khởi hành của tour đó để thấy rằng lịch khởi hành mới đã xuất hiện trong danh sách.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ThemLichKhoiHanh(int tourId, DateTime ngayKhoiHanh, TimeSpan? gioKhoiHanh, int soDoChoNgoiId, int soChoMoBan, decimal? giaNguoiLon, decimal? giaTreEm)
        {
            if (!LaAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            var tour = _context.Tours.FirstOrDefault(x => x.Id == tourId);

            if (tour == null)
            {
                return NotFound();
            }

            decimal giaLon = giaNguoiLon.HasValue && giaNguoiLon.Value > 0
                ? giaNguoiLon.Value
                : tour.Gia;

            decimal giaTre = giaTreEm.HasValue && giaTreEm.Value > 0
                ? giaTreEm.Value
                : giaLon * 0.5m;

            // Admin chọn xe trong các xe của đúng phương tiện của tour (RB-14)
            var xe = DanhSachXe(tour.PhuongTienId).FirstOrDefault(x => x.Id == soDoChoNgoiId);
            if (xe == null)
            {
                TempData["error"] = tour.PhuongTienId.HasValue
                    ? "Vui lòng chọn xe cho lịch khởi hành."
                    : "Tour chưa có phương tiện. Hãy gán phương tiện cho tour trước khi thêm lịch.";
                return RedirectToAction("LichKhoiHanh", new { id = tourId });
            }

            if (soChoMoBan <= 0 || soChoMoBan > xe.SoViTri)
            {
                TempData["error"] = $"Số chỗ mở bán phải từ 1 đến {xe.SoViTri} ({xe.TenSoDo}).";
                return RedirectToAction("LichKhoiHanh", new { id = tourId });
            }

            // Lịch mới chưa có ai đặt nên số chỗ còn bằng số chỗ mở bán (RB-12)
            var lich = new LichKhoiHanhTour
            {
                SoDoChoNgoiId = xe.Id,
                TourId = tourId,
                NgayKhoiHanh = ngayKhoiHanh,
                GioKhoiHanh = gioKhoiHanh,
                SoChoCon = soChoMoBan,
                GiaNguoiLon = giaLon,
                GiaTreEm = giaTre,
                TrangThai = TrangThaiLich.MoBan,
            };

            using (var giaoDichCsdl = _context.Database.BeginTransaction())
            {
                _context.LichKhoiHanhTours.Add(lich);
                _context.SaveChanges();

                // Chỗ dư khóa tạm ở cuối xe; admin chọn lại chỗ nào mở bán ở trang Chọn chỗ mở bán (RB-37)
                foreach (var viTriId in SucChuaLich.ChonChoKhoaCuoiXe(_context, lich.Id, xe.Id, soChoMoBan) ?? new List<int>())
                {
                    _context.ChoKhongMoBans.Add(new ChoKhongMoBan { LichKhoiHanhTourId = lich.Id, ViTriChoNgoiId = viTriId });
                }
                _context.SaveChanges();
                giaoDichCsdl.Commit();
            }

            TempData["success"] = soChoMoBan < xe.SoViTri
                ? $"Đã thêm lịch: {xe.TenSoDo}, mở bán {soChoMoBan} chỗ. Các chỗ cuối xe đang khóa, bấm Chọn chỗ mở bán để đổi."
                : $"Đã thêm lịch: {xe.TenSoDo}, mở bán đủ {soChoMoBan} chỗ.";
            return RedirectToAction("LichKhoiHanh", new { id = tourId });
        }
        // Đây là action dùng để cập nhật thông tin một lịch khởi hành của một tour cụ thể trong trang quản trị. Khi admin chỉnh sửa thông tin ngày khởi hành, giờ khởi hành, số chỗ còn, giá người lớn hoặc giá trẻ em của một lịch khởi hành nào đó trong trang danh sách lịch khởi hành của tour và nhấn nút lưu, action này sẽ được gọi với các thông tin đã chỉnh sửa cùng với Id của lịch khởi hành đó để xác định chính xác lịch khởi hành cần cập nhật. Hàm sẽ kiểm tra quyền admin, sau đó tìm lịch khởi hành trong database theo Id. Nếu không tìm thấy lịch khởi hành thì trả về lỗi NotFound. Nếu tìm thấy lịch khởi hành, hệ thống sẽ cập nhật các thông tin của lịch khởi hành đó với các giá trị đã nhận được từ form chỉnh sửa. Sau đó hệ thống sẽ lưu lại để đảm bảo rằng thông tin của lịch khởi hành đã được cập nhật thành công. Cuối cùng chuyển hướng admin về trang danh sách lịch khởi hành của tour đó để thấy rằng thông tin của lịch khởi hành đã được cập nhật trong danh sách.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CapNhatLichKhoiHanh(int id, DateTime ngayKhoiHanh, TimeSpan? gioKhoiHanh, int soDoChoNgoiId, decimal? giaNguoiLon, decimal? giaTreEm)
        {
            if (!LaAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }
            // Tìm lịch khởi hành trong database theo Id. Nếu không tìm thấy lịch khởi hành thì trả về lỗi NotFound. Nếu tìm thấy lịch khởi hành, hệ thống sẽ cập nhật các thông tin của lịch khởi hành đó với các giá trị đã nhận được từ form chỉnh sửa. Sau đó hệ thống sẽ lưu lại để đảm bảo rằng thông tin của lịch khởi hành đã được cập nhật thành công. Cuối cùng chuyển hướng admin về trang danh sách lịch khởi hành của tour đó để thấy rằng thông tin của lịch khởi hành đã được cập nhật trong danh sách.
            var lich = _context.LichKhoiHanhTours
                .Include(x => x.Tour)
                .FirstOrDefault(x => x.Id == id);

            if (lich == null)
            {
                return NotFound();
            }
            // Nếu admin không nhập giá người lớn hoặc giá trẻ em, hệ thống sẽ tự động lấy giá từ tour gốc của lịch khởi hành đó để làm giá mặc định. Nếu giá người lớn không được nhập hoặc có giá trị không hợp lệ (nhỏ hơn hoặc bằng 0), hệ thống sẽ sử dụng giá của tour gốc. Nếu giá trẻ em không được nhập hoặc có giá trị không hợp lệ, hệ thống sẽ tính giá trẻ em bằng cách lấy giá người lớn nhân với 0.5 (tức là trẻ em sẽ được giảm 50% so với người lớn).
            decimal giaLon = giaNguoiLon.HasValue && giaNguoiLon.Value > 0
                ? giaNguoiLon.Value
                : lich.Tour?.Gia ?? 0;

            decimal giaTre = giaTreEm.HasValue && giaTreEm.Value > 0
                ? giaTreEm.Value
                : giaLon * 0.5m;
            // Cập nhật các thông tin của lịch khởi hành đó với các giá trị đã nhận được từ form chỉnh sửa. Sau đó hệ thống sẽ lưu lại để đảm bảo rằng thông tin của lịch khởi hành đã được cập nhật thành công. Cuối cùng chuyển hướng admin về trang danh sách lịch khởi hành của tour đó để thấy rằng thông tin của lịch khởi hành đã được cập nhật trong danh sách.
            // Số ghế khách đang giữ trên lịch (RB-12)
            int daGiu = SucChuaLich.SoGheDangGiu(_context, lich.Id);

            // Lịch đã có khách giữ chỗ thì không đổi ngày: khách đặt đi ngày nào phải giữ ngày đó, và hạn giữ chỗ
            // của đơn nhóm không được nằm sau ngày đi (R26). Muốn dời ngày thì hủy lịch rồi tạo lịch mới. Giờ đi vẫn đổi được.
            if (daGiu > 0 && ngayKhoiHanh.Date != lich.NgayKhoiHanh.Date)
            {
                TempData["error"] = $"Lịch này đã có {daGiu} khách giữ chỗ nên không đổi ngày đi được. Muốn dời ngày, hủy lịch này rồi tạo lịch mới.";
                return RedirectToAction("LichKhoiHanh", new { id = lich.TourId });
            }

            // Đổi xe (RB-14): chỉ khi chưa có khách ngồi trên xe hiện tại. Số chỗ mở bán giữ nguyên
            // (nhỏ hơn nếu xe mới ít chỗ hơn), chỗ dư trên xe mới khóa tạm ở cuối xe (RB-37).
            bool doiXe = soDoChoNgoiId != (lich.SoDoChoNgoiId ?? 0);
            XeChon? xeMoi = null;
            int moBanMoi = 0;
            if (doiXe)
            {
                xeMoi = DanhSachXe(lich.Tour?.PhuongTienId).FirstOrDefault(x => x.Id == soDoChoNgoiId);
                if (xeMoi == null)
                {
                    TempData["error"] = "Xe đã chọn không thuộc phương tiện của tour.";
                    return RedirectToAction("LichKhoiHanh", new { id = lich.TourId });
                }
                if (_context.ChoNgoiChuyenDis.Any(c => c.LichKhoiHanhTourId == lich.Id))
                {
                    TempData["error"] = "Lịch đã có khách chọn chỗ ngồi trên xe hiện tại nên không đổi xe được.";
                    return RedirectToAction("LichKhoiHanh", new { id = lich.TourId });
                }
                moBanMoi = Math.Min(SucChuaLich.SoChoMoBan(_context, lich.Id), xeMoi.SoViTri);
                if (moBanMoi < Math.Max(1, daGiu))
                {
                    TempData["error"] = $"{xeMoi.TenSoDo} không đủ chỗ cho {daGiu} khách đã đặt.";
                    return RedirectToAction("LichKhoiHanh", new { id = lich.TourId });
                }
            }

            lich.NgayKhoiHanh = ngayKhoiHanh;
            lich.GioKhoiHanh = gioKhoiHanh;
            lich.GiaNguoiLon = giaLon;
            lich.GiaTreEm = giaTre;

            using (var giaoDichCsdl = _context.Database.BeginTransaction())
            {
                if (doiXe && xeMoi != null)
                {
                    _context.ChoKhongMoBans.RemoveRange(_context.ChoKhongMoBans.Where(k => k.LichKhoiHanhTourId == lich.Id));
                    lich.SoDoChoNgoiId = xeMoi.Id;
                    _context.SaveChanges();
                    foreach (var viTriId in SucChuaLich.ChonChoKhoaCuoiXe(_context, lich.Id, xeMoi.Id, moBanMoi) ?? new List<int>())
                    {
                        _context.ChoKhongMoBans.Add(new ChoKhongMoBan { LichKhoiHanhTourId = lich.Id, ViTriChoNgoiId = viTriId });
                    }
                }
                _context.SaveChanges();
                if (doiXe)
                {
                    SucChuaLich.TinhLaiSoChoCon(_context, lich.Id);
                }
                giaoDichCsdl.Commit();
            }

            // Lịch vừa được gán xe khi đã có khách đặt: xếp chỗ cho các ghế chưa có chỗ (RB-36)
            if (lich.SoDoChoNgoiId.HasValue && XepChoNgoi.XepChoConThieu(_context, lich.Id) > 0)
            {
                try
                {
                    _context.SaveChanges();
                }
                catch (DbUpdateException)
                {
                    // Trùng với khách đang chọn chỗ: bỏ lần này, dịch vụ chạy nền sẽ xếp lại sau
                    _context.ChangeTracker.Clear();
                }
            }

            return RedirectToAction("LichKhoiHanh", new { id = lich.TourId });
        }
        // Đây là action dùng để hủy một lịch khởi hành của một tour cụ thể trong trang quản trị. Khi admin nhấn nút hủy ở một lịch khởi hành nào đó trong trang danh sách lịch khởi hành của tour, action này sẽ được gọi với Id của lịch khởi hành đó để xác định chính xác lịch khởi hành cần hủy. Hàm sẽ kiểm tra quyền admin, sau đó tìm lịch khởi hành trong database theo Id. Nếu không tìm thấy lịch khởi hành thì trả về lỗi NotFound. Nếu tìm thấy lịch khởi hành, hệ thống sẽ lấy Id của tour mà lịch khởi hành đó thuộc về để có thể chuyển hướng lại đúng trang danh sách lịch khởi hành của tour đó sau khi hủy. Sau đó hệ thống sẽ cập nhật trạng thái của lịch khởi hành đó thành "Đã hủy" và đặt số chỗ còn lại về 0 để đảm bảo rằng không còn chỗ nào có thể đặt cho lịch khởi hành đã bị hủy. Đồng thời, hệ thống sẽ tìm tất cả các đơn đặt tour liên quan đến lịch khởi hành đó mà chưa bị hủy và cập nhật trạng thái thanh toán của các đơn đặt tour này thành "Đã hủy" cùng với ghi chú giải thích rằng lý do hủy là do admin đã hủy lịch khởi hành. Cuối cùng chuyển hướng admin về trang danh sách lịch khởi hành của tour đó để thấy rằng lịch khởi hành đã được cập nhật trạng thái thành "Đã hủy" trong danh sách.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult HuyLichKhoiHanh(int id)
        {
            if (!LaAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }
            // Tìm lịch khởi hành trong database theo Id. Nếu không tìm thấy lịch khởi hành thì trả về lỗi NotFound. Nếu tìm thấy lịch khởi hành, hệ thống sẽ lấy Id của tour mà lịch khởi hành đó thuộc về để có thể chuyển hướng lại đúng trang danh sách lịch khởi hành của tour đó sau khi hủy. Sau đó hệ thống sẽ cập nhật trạng thái của lịch khởi hành đó thành "Đã hủy" và đặt số chỗ còn lại về 0 để đảm bảo rằng không còn chỗ nào có thể đặt cho lịch khởi hành đã bị hủy. Đồng thời, hệ thống sẽ tìm tất cả các đơn đặt tour liên quan đến lịch khởi hành đó mà chưa bị hủy và cập nhật trạng thái thanh toán của các đơn đặt tour này thành "Đã hủy" cùng với ghi chú giải thích rằng lý do hủy là do admin đã hủy lịch khởi hành. Cuối cùng chuyển hướng admin về trang danh sách lịch khởi hành của tour đó để thấy rằng lịch khởi hành đã được cập nhật trạng thái thành "Đã hủy" trong danh sách.
            var lich = _context.LichKhoiHanhTours
                .Include(x => x.Tour)
                .FirstOrDefault(x => x.Id == id);

            if (lich == null)
            {
                return NotFound();
            }
            // Lấy Id của tour mà lịch khởi hành đó thuộc về để có thể chuyển hướng lại đúng trang danh sách lịch khởi hành của tour đó sau khi hủy. Sau đó hệ thống sẽ cập nhật trạng thái của lịch khởi hành đó thành "Đã hủy" và đặt số chỗ còn lại về 0 để đảm bảo rằng không còn chỗ nào có thể đặt cho lịch khởi hành đã bị hủy. Đồng thời, hệ thống sẽ tìm tất cả các đơn đặt tour liên quan đến lịch khởi hành đó mà chưa bị hủy và cập nhật trạng thái thanh toán của các đơn đặt tour này thành "Đã hủy" cùng với ghi chú giải thích rằng lý do hủy là do admin đã hủy lịch khởi hành. Cuối cùng chuyển hướng admin về trang danh sách lịch khởi hành của tour đó để thấy rằng lịch khởi hành đã được cập nhật trạng thái thành "Đã hủy" trong danh sách.
            var tourId = lich.TourId;

            if (lich.TrangThai == TrangThaiLich.DaHuy)
            {
                TempData["error"] = "Lịch này đã được hủy trước đó.";
                return RedirectToAction("LichKhoiHanh", new { id = tourId });
            }

            lich.TrangThai = TrangThaiLich.DaHuy;

            var dsDon = _context.DatTours
                .Include(x => x.HanhKhachDatTours)
                .Where(x => x.LichKhoiHanhTourId == id && x.TrangThai != "DaHuy")
                .ToList();

            // Mọi đơn của lịch bị hủy theo: nhả ghế, bỏ chỗ ngồi đã chọn (RB-16, RB-17).
            // Lịch đã hủy không ai giữ chỗ nữa nên số chỗ còn bằng sức chứa (RB-12).
            var bayGio = GioVN.BayGio;
            var dsGhe = dsDon.SelectMany(d => d.HanhKhachDatTours).Where(h => h.NgayNha == null).ToList();
            var dsGheId = dsGhe.Select(h => h.Id).ToList();
            foreach (var ghe in dsGhe)
            {
                ghe.NgayNha = bayGio;
            }
            _context.ChoNgoiChuyenDis.RemoveRange(
                _context.ChoNgoiChuyenDis.Where(c => dsGheId.Contains(c.HanhKhachDatTourId)));
            lich.SoChoCon = SucChuaLich.SoChoMoBan(_context, lich.Id);
            // Cập nhật trạng thái thanh toán của các đơn đặt tour liên quan thành "Đã hủy" và thêm ghi chú giải thích lý do hủy là do admin đã hủy lịch khởi hành. Điều này sẽ giúp khách hàng hiểu được lý do tại sao đơn đặt tour của họ bị hủy và cũng giúp admin quản lý các đơn đặt tour liên quan đến lịch khởi hành đã bị hủy một cách hiệu quả hơn.
            foreach (var don in dsDon)
            {
                don.TrangThai = "DaHuy";
                don.GhiChu = string.IsNullOrWhiteSpace(don.GhiChu)
                    ? "Lịch khởi hành đã bị admin hủy."
                    : don.GhiChu + "\nLịch khởi hành đã bị admin hủy.";
            }

            _context.SaveChanges();

            TempData["success"] = "Đã hủy lịch khởi hành thành công.";
            return RedirectToAction("LichKhoiHanh", new { id = tourId });
        }

        // Tạm ngưng nhận đặt hoặc mở bán lại một lịch sắp khởi hành. Đơn đã đặt giữ nguyên.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DoiTrangThaiBanLich(int id)
        {
            if (!LaAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            var lich = _context.LichKhoiHanhTours.FirstOrDefault(x => x.Id == id);
            if (lich == null)
            {
                return NotFound();
            }

            if (lich.TrangThai == TrangThaiLich.DaHuy)
            {
                TempData["error"] = "Lịch đã hủy, không đổi trạng thái bán được.";
                return RedirectToAction("LichKhoiHanh", new { id = lich.TourId });
            }

            lich.TrangThai = lich.TrangThai == TrangThaiLich.TamNgung ? TrangThaiLich.MoBan : TrangThaiLich.TamNgung;
            _context.SaveChanges();

            TempData["success"] = lich.TrangThai == TrangThaiLich.TamNgung
                ? "Đã tạm ngưng bán lịch này. Khách không đặt mới được, đơn cũ giữ nguyên."
                : "Đã mở bán lại lịch này.";
            return RedirectToAction("LichKhoiHanh", new { id = lich.TourId });
        }
        // Hàm phụ để nạp dữ liệu cho các dropdown list trong trang thêm hoặc chỉnh sửa phân công hướng dẫn viên và tài xế cho lịch khởi hành của tour. Hàm này sẽ lấy danh sách các lịch khởi hành của tour hiện tại (hoặc tất cả nếu không có lịch khởi hành cụ thể nào được chọn) để hiển thị trong dropdown chọn lịch khởi hành. Đồng thời, hàm cũng sẽ lấy danh sách các nhân viên đang làm việc và có loại nhân viên là "HuongDanVien" để hiển thị trong dropdown chọn hướng dẫn viên, và danh sách các nhân viên đang làm việc và có loại nhân viên là "TaiXe" để hiển thị trong dropdown chọn tài xế. Dữ liệu này sẽ được lưu vào ViewBag để có thể sử dụng trong view khi hiển thị form thêm hoặc chỉnh sửa phân công.
        private void NapDuLieuPhanCong(int? lichHienTaiId = null)
        {
            var homNay = GioVN.HomNay;

            var lichKhoiHanhs = _context.LichKhoiHanhTours
                .AsNoTracking()
                .Include(x => x.Tour)
                .Where(x =>
                    x.TrangThai != TrangThaiLich.DaHuy &&
                    x.NgayKhoiHanh >= homNay &&
                    x.SoChoCon > 0 &&
                    (
                        x.Id == lichHienTaiId ||
                        !_context.PhanCongTours.Any(pc =>
                            pc.LichKhoiHanhTourId == x.Id)
                    )
                )
                .OrderBy(x => x.NgayKhoiHanh)
                .ThenBy(x => x.GioKhoiHanh)
                .ToList();

            ViewBag.LichKhoiHanhList = lichKhoiHanhs
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text =
                        $"{x.Tour?.TenTour} - " +
                        $"{x.NgayKhoiHanh:dd/MM/yyyy} - " +
                        $"{(x.GioKhoiHanh.HasValue ? x.GioKhoiHanh.Value.ToString(@"hh\:mm") : "--:--")} - " +
                        $"Còn {x.SoChoCon} chỗ"
                })
                .ToList();

            // Họ tên, số điện thoại và vai trò lấy từ tài khoản của nhân viên (RB-15)
            ViewBag.HuongDanVienList = _context.NhanViens
                .AsNoTracking()
                .Where(x => x.DangLamViec && x.TaiKhoan!.VaiTro == "huongdanvien")
                .OrderBy(x => x.TaiKhoan!.HoTen)
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = x.TaiKhoan!.HoTen + " - " + (x.TaiKhoan.SoDienThoai ?? x.TaiKhoan.Email ?? "")
                })
                .ToList();

            ViewBag.TaiXeList = _context.NhanViens
                .AsNoTracking()
                .Where(x => x.DangLamViec && x.TaiKhoan!.VaiTro == "taixe")
                .OrderBy(x => x.TaiKhoan!.HoTen)
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = x.TaiKhoan!.HoTen + " - " + (x.TaiKhoan.SoDienThoai ?? x.TaiKhoan.Email ?? "")
                })
                .ToList();
        }

        // RB-15: người được chọn làm hướng dẫn viên phải có vai trò huongdanvien, tài xế phải là taixe,
        // và đang làm việc. Form chỉ liệt kê đúng người, nhưng vẫn kiểm tra lại phía máy chủ.
        private void KiemTraNhanSuPhanCong(PhanCongTour model)
        {
            bool hdvHopLe = _context.NhanViens.Any(x =>
                x.Id == model.HuongDanVienId && x.DangLamViec && x.TaiKhoan!.VaiTro == "huongdanvien");
            if (!hdvHopLe)
            {
                ModelState.AddModelError("HuongDanVienId", "Hãy chọn một hướng dẫn viên đang làm việc.");
            }

            if (model.TaiXeId.HasValue)
            {
                bool taiXeHopLe = _context.NhanViens.Any(x =>
                    x.Id == model.TaiXeId.Value && x.DangLamViec && x.TaiKhoan!.VaiTro == "taixe");
                if (!taiXeHopLe)
                {
                    ModelState.AddModelError("TaiXeId", "Hãy chọn một tài xế đang làm việc.");
                }
            }
        }
        // Đây là action hiển thị danh sách các phân công hướng dẫn viên và tài xế cho các lịch khởi hành của tour trong trang quản trị. Admin có thể xem chi tiết các phân công đã được tạo, bao gồm tên tour, ngày khởi hành, tên hướng dẫn viên và tên tài xế. Admin cũng có thể tìm kiếm các phân công đã được tạo theo tên tour, tên hướng dẫn viên hoặc tên tài xế để dễ dàng quản lý các phân công đã tạo.
        public IActionResult PhanCongTour(string search)
        {
            if (!LaAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            NapDuLieuPhanCong();
            // Đoạn này truy vấn danh sách các phân công hướng dẫn viên và tài xế cho các lịch khởi hành của tour từ database, bao gồm các thông tin liên quan như tên tour, ngày khởi hành, tên hướng dẫn viên và tên tài xế. Sau đó áp dụng bộ lọc tìm kiếm theo tên tour, tên hướng dẫn viên hoặc tên tài xế nếu admin nhập vào ô tìm kiếm. Cuối cùng trả về view PhanCongTour.cshtml cùng với danh sách phân công đã được lọc để hiển thị trên trang quản lý phân công hướng dẫn viên và tài xế cho các lịch khởi hành của tour.
            var ds = _context.PhanCongTours
                .Include(x => x.LichKhoiHanhTour)
                    .ThenInclude(x => x.Tour)
                .Include(x => x.HuongDanVien)
                    .ThenInclude(x => x!.TaiKhoan)
                .Include(x => x.TaiXe)
                    .ThenInclude(x => x!.TaiKhoan)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search)) // 
            {// Chỉ tìm kiếm khi người dùng có nhập từ khóa hợp lệ.
                ds = ds.Where(x =>
                    (x.LichKhoiHanhTour != null &&
                     x.LichKhoiHanhTour.Tour != null &&
                     x.LichKhoiHanhTour.Tour.TenTour.Contains(search)) ||
                    (x.HuongDanVien != null && x.HuongDanVien.TaiKhoan!.HoTen.Contains(search)) ||
                    (x.TaiXe != null && x.TaiXe.TaiKhoan!.HoTen.Contains(search))
                );
            }

            ViewBag.Search = search;

            return View(ds.OrderByDescending(x => x.Id).ToList());
        }
        // Đây là action hiển thị trang thêm mới một phân công hướng dẫn viên và tài xế cho một lịch khởi hành của tour trong trang quản trị. Admin có thể chọn một lịch khởi hành cụ thể và sau đó chọn hướng dẫn viên và tài xế để phân công cho lịch khởi hành đó. Khi admin điền thông tin và nhấn nút lưu, action này sẽ được gọi với các thông tin đã nhập để tạo một đối tượng PhanCongTour mới. Đối tượng này sẽ được thêm vào database và lưu lại để đảm bảo rằng phân công đã được tạo thành công. Cuối cùng chuyển hướng admin về trang danh sách phân công tour để thấy rằng phân công mới đã xuất hiện trong danh sách.
        public IActionResult ThemPhanCongTour()
        {
            if (!LaAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            NapDuLieuPhanCong();
            return View();
        }
        // Đây là action dùng để thêm mới một phân công hướng dẫn viên và tài xế cho một lịch khởi hành của tour trong trang quản trị. Khi admin điền thông tin và nhấn nút lưu, action này sẽ được gọi với các thông tin đã nhập để tạo một đối tượng PhanCongTour mới. Đối tượng này sẽ được thêm vào database và lưu lại để đảm bảo rằng phân công đã được tạo thành công. Cuối cùng chuyển hướng admin về trang danh sách phân công tour để thấy rằng phân công mới đã xuất hiện trong danh sách.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ThemPhanCongTour(PhanCongTour model)
        {
            if (!LaAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            var lich = _context.LichKhoiHanhTours
                .FirstOrDefault(x => x.Id == model.LichKhoiHanhTourId);

            if (lich == null)
            {
                ModelState.AddModelError(
                    "LichKhoiHanhTourId",
                    "Lịch khởi hành không tồn tại.");
            }
            else
            {
                if (lich.TrangThai == TrangThaiLich.DaHuy)
                {
                    ModelState.AddModelError(
                        "LichKhoiHanhTourId",
                        "Không thể phân công cho lịch đã hủy.");
                }
                else if (lich.NgayKhoiHanh.Date < GioVN.HomNay)
                {
                    ModelState.AddModelError(
                        "LichKhoiHanhTourId",
                        "Không thể phân công cho lịch đã qua.");
                }
                else if (lich.SoChoCon <= 0)
                {
                    ModelState.AddModelError(
                        "LichKhoiHanhTourId",
                        "Lịch khởi hành đã hết chỗ.");
                }
            }

            if (_context.PhanCongTours.Any(x =>
                x.LichKhoiHanhTourId == model.LichKhoiHanhTourId))
            {
                ModelState.AddModelError(
                    "LichKhoiHanhTourId",
                    "Lịch khởi hành này đã được phân công rồi.");
            }

            KiemTraNhanSuPhanCong(model);

            if (!ModelState.IsValid)
            {
                NapDuLieuPhanCong();
                return View(model);
            }

            model.NgayPhanCong = GioVN.BayGio;

            _context.PhanCongTours.Add(model);
            _context.SaveChanges();

            TempData["success"] = "Phân công tour thành công!";
            return RedirectToAction("PhanCongTour");
        }
        // Đây là action hiển thị trang chỉnh sửa một phân công hướng dẫn viên và tài xế cho một lịch khởi hành của tour trong trang quản trị. Admin có thể chỉnh sửa hướng dẫn viên, tài xế hoặc ghi chú của một phân công đã được tạo trước đó. Khi admin chỉnh sửa thông tin và nhấn nút lưu, action này sẽ được gọi với các thông tin đã chỉnh sửa cùng với Id của phân công đó để xác định chính xác phân công cần cập nhật. Hàm sẽ kiểm tra quyền admin, sau đó tìm phân công trong database theo Id. Nếu không tìm thấy phân công thì trả về lỗi NotFound. Nếu tìm thấy phân công, hệ thống sẽ nạp dữ liệu cho các dropdown list để hiển thị trong form chỉnh sửa, đồng thời trả về view cùng với đối tượng phân công đã tìm thấy để hiển thị thông tin hiện tại của phân công đó trên form chỉnh sửa.
        public IActionResult SuaPhanCongTour(int id)
        {
            if (!LaAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }
            // Tìm phân công trong database theo Id. Nếu không tìm thấy phân công thì trả về lỗi NotFound. Nếu tìm thấy phân công, hệ thống sẽ nạp dữ liệu cho các dropdown list để hiển thị trong form chỉnh sửa, đồng thời trả về view cùng với đối tượng phân công đã tìm thấy để hiển thị thông tin hiện tại của phân công đó trên form chỉnh sửa.
            var pc = _context.PhanCongTours
                .FirstOrDefault(x => x.Id == id);

            if (pc == null)
            {
                return NotFound();
            }

            NapDuLieuPhanCong(pc.LichKhoiHanhTourId);
            return View(pc);
        }
        // Đây là action dùng để cập nhật thông tin một phân công hướng dẫn viên và tài xế cho một lịch khởi hành của tour trong trang quản trị. Khi admin chỉnh sửa thông tin và nhấn nút lưu, action này sẽ được gọi với các thông tin đã chỉnh sửa cùng với Id của phân công đó để xác định chính xác phân công cần cập nhật. Hàm sẽ kiểm tra quyền admin, sau đó tìm phân công trong database theo Id. Nếu không tìm thấy phân công thì trả về lỗi NotFound. Nếu tìm thấy phân công, hệ thống sẽ cập nhật các thông tin của phân công đó với các giá trị đã nhận được từ form chỉnh sửa. Sau đó hệ thống sẽ lưu lại để đảm bảo rằng thông tin của phân công đã được cập nhật thành công. Cuối cùng chuyển hướng admin về trang danh sách phân công tour để thấy rằng thông tin của phân công đã được cập nhật trong danh sách.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SuaPhanCongTour(PhanCongTour model)
        {
            if (!LaAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }
            // Tìm phân công trong database theo Id. Nếu không tìm thấy phân công thì trả về lỗi NotFound. Nếu tìm thấy phân công, hệ thống sẽ cập nhật các thông tin của phân công đó với các giá trị đã nhận được từ form chỉnh sửa. Sau đó hệ thống sẽ lưu lại để đảm bảo rằng thông tin của phân công đã được cập nhật thành công. Cuối cùng chuyển hướng admin về trang danh sách phân công tour để thấy rằng thông tin của phân công đã được cập nhật trong danh sách.
            var pc = _context.PhanCongTours
                .FirstOrDefault(x => x.Id == model.Id);

            if (pc == null)
            {
                return NotFound();
            }

            KiemTraNhanSuPhanCong(model);

            if (!ModelState.IsValid)
            {
                NapDuLieuPhanCong(pc.LichKhoiHanhTourId);
                return View(model);
            }

            pc.HuongDanVienId = model.HuongDanVienId;
            pc.TaiXeId = model.TaiXeId;
            pc.GhiChu = model.GhiChu;

            _context.SaveChanges();

            TempData["success"] = "Đã thay đổi nhân sự phụ trách tour.";
            return RedirectToAction("PhanCongTour");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult XoaPhanCongTour(int id)
        {
            if (!LaAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }
            // Tìm phân công trong database theo Id. Nếu không tìm thấy phân công thì trả về lỗi NotFound. Nếu tìm thấy phân công, hệ thống sẽ xóa bản ghi của phân công đó khỏi database và lưu lại để đảm bảo rằng phân công đã bị xóa thành công. Cuối cùng chuyển hướng admin về trang danh sách phân công tour để thấy rằng phân công đã bị xóa khỏi danh sách.
            var pc = _context.PhanCongTours.FirstOrDefault(x => x.Id == id);

            if (pc == null)
            {
                return NotFound();
            }
            // Xóa bản ghi của phân công đó khỏi database và lưu lại để đảm bảo rằng phân công đã bị xóa thành công. Cuối cùng chuyển hướng admin về trang danh sách phân công tour để thấy rằng phân công đã bị xóa khỏi danh sách.
            _context.PhanCongTours.Remove(pc);
            _context.SaveChanges();

            return RedirectToAction("PhanCongTour");
        }
        // Đây là action hiển thị danh sách các khuyến mãi đã được tạo trong trang quản trị. Admin có thể xem chi tiết các khuyến mãi, bao gồm tên khuyến mãi, mã khuyến mãi, mô tả, kiểu giảm giá, giá trị giảm, ngày bắt đầu, ngày kết thúc, màu nền, thứ tự hiển thị và trạng thái hiển thị. Admin cũng có thể tìm kiếm các khuyến mãi đã được tạo theo tên khuyến mãi hoặc mã khuyến mãi để dễ dàng quản lý các khuyến mãi đã tạo.
        public IActionResult KhuyenMai(string search)
        {
            if (!LaAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }
            // Đoạn này truy vấn danh sách các khuyến mãi đã được tạo từ database, bao gồm các thông tin liên quan như tên khuyến mãi, mã khuyến mãi, mô tả, kiểu giảm giá, giá trị giảm, ngày bắt đầu, ngày kết thúc, màu nền, thứ tự hiển thị và trạng thái hiển thị. Sau đó áp dụng bộ lọc tìm kiếm theo tên khuyến mãi hoặc mã khuyến mãi nếu admin nhập vào ô tìm kiếm. Cuối cùng trả về view KhuyenMai.cshtml cùng với danh sách khuyến mãi đã được lọc để hiển thị trên trang quản lý khuyến mãi.
            var ds = _context.KhuyenMais.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                ds = ds.Where(x =>
                    x.TenKhuyenMai.Contains(search) ||
                    x.MaKhuyenMai.Contains(search));
            }

            ViewBag.Search = search;

            var dsKem = ds
                .OrderBy(x => x.ThuTu)
                .ThenByDescending(x => x.Id)
                .Select(x => new { KhuyenMai = x, DaDung = x.DatTours.Count(d => d.TrangThai != "DaHuy") })
                .ToList();
            foreach (var x in dsKem)
            {
                x.KhuyenMai.SoLuotDaDungThucTe = x.DaDung;
            }

            return View(dsKem.Select(x => x.KhuyenMai).ToList());
        }
        // Đây là action dùng để thêm mới một khuyến mãi trong trang quản trị. Khi admin điền thông tin về khuyến mãi như tên khuyến mãi, mã khuyến mãi, mô tả, kiểu giảm giá, giá trị giảm, ngày bắt đầu, ngày kết thúc, màu nền, thứ tự hiển thị và trạng thái hiển thị vào form thêm khuyến mãi và nhấn nút lưu, action này sẽ được gọi với các thông tin đó để tạo một đối tượng KhuyenMai mới. Hàm sẽ kiểm tra quyền admin, sau đó chuẩn hóa dữ liệu của khuyến mãi để đảm bảo rằng các giá trị đã nhận được từ form là hợp lệ và có định dạng đúng. Sau đó hàm sẽ kiểm tra các điều kiện hợp lệ của khuyến mãi như tên khuyến mãi không được để trống, mã khuyến mãi phải có định dạng hợp lệ và ngày bắt đầu phải trước ngày kết thúc. Nếu có lỗi nào đó thì sẽ lưu lỗi vào TempData và chuyển hướng admin về trang danh sách khuyến mãi để hiển thị lỗi. Nếu không có lỗi nào thì hệ thống sẽ kiểm tra xem có khuyến mãi nào đã tồn tại với cùng một mã khuyến mãi hay không. Nếu đã tồn tại thì sẽ lưu lỗi vào TempData và chuyển hướng admin về trang danh sách khuyến mãi để hiển thị lỗi. Nếu chưa tồn tại thì hệ thống sẽ tạo một đối tượng KhuyenMai mới với các thông tin đã nhận được từ form cùng với ngày tạo hiện tại và số lượt đã dùng ban đầu bằng 0. Đối tượng này sẽ được thêm vào database và lưu lại để đảm bảo rằng khuyến mãi đã được tạo thành công. Cuối cùng chuyển hướng admin về trang danh sách khuyến mãi để thấy rằng khuyến mãi mới đã xuất hiện trong danh sách.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ThemKhuyenMai(KhuyenMai model)
        {
            if (!LaAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }
            // Chuẩn hóa dữ liệu của khuyến mãi để đảm bảo rằng các giá trị đã nhận được từ form là hợp lệ và có định dạng đúng. Sau đó hàm sẽ kiểm tra các điều kiện hợp lệ của khuyến mãi như tên khuyến mãi không được để trống, mã khuyến mãi phải có định dạng hợp lệ và ngày bắt đầu phải trước ngày kết thúc. Nếu có lỗi nào đó thì sẽ lưu lỗi vào TempData và chuyển hướng admin về trang danh sách khuyến mãi để hiển thị lỗi. Nếu không có lỗi nào thì hệ thống sẽ kiểm tra xem có khuyến mãi nào đã tồn tại với cùng một mã khuyến mãi hay không. Nếu đã tồn tại thì sẽ lưu lỗi vào TempData và chuyển hướng admin về trang danh sách khuyến mãi để hiển thị lỗi. Nếu chưa tồn tại thì hệ thống sẽ tạo một đối tượng KhuyenMai mới với các thông tin đã nhận được từ form cùng với ngày tạo hiện tại và số lượt đã dùng ban đầu bằng 0. Đối tượng này sẽ được thêm vào database và lưu lại để đảm bảo rằng khuyến mãi đã được tạo thành công. Cuối cùng chuyển hướng admin về trang danh sách khuyến mãi để thấy rằng khuyến mãi mới đã xuất hiện trong danh sách.
            ChuanHoaKhuyenMai(model);
            // Sau đó hàm sẽ kiểm tra các điều kiện hợp lệ của khuyến mãi như tên khuyến mãi không được để trống, mã khuyến mãi phải có định dạng hợp lệ và ngày bắt đầu phải trước ngày kết thúc. Nếu có lỗi nào đó thì sẽ lưu lỗi vào TempData và chuyển hướng admin về trang danh sách khuyến mãi để hiển thị lỗi. Nếu không có lỗi nào thì hệ thống sẽ kiểm tra xem có khuyến mãi nào đã tồn tại với cùng một mã khuyến mãi hay không. Nếu đã tồn tại thì sẽ lưu lỗi vào TempData và chuyển hướng admin về trang danh sách khuyến mãi để hiển thị lỗi. Nếu chưa tồn tại thì hệ thống sẽ tạo một đối tượng KhuyenMai mới với các thông tin đã nhận được từ form cùng với ngày tạo hiện tại và số lượt đã dùng ban đầu bằng 0. Đối tượng này sẽ được thêm vào database và lưu lại để đảm bảo rằng khuyến mãi đã được tạo thành công. Cuối cùng chuyển hướng admin về trang danh sách khuyến mãi để thấy rằng khuyến mãi mới đã xuất hiện trong danh sách.
            string? loi = KiemTraKhuyenMai(model);
            if (!string.IsNullOrWhiteSpace(loi))
            {
                TempData["Loi"] = loi;
                return RedirectToAction("KhuyenMai");
            }
            // Nếu không có lỗi nào thì hệ thống sẽ kiểm tra xem có khuyến mãi nào đã tồn tại với cùng một mã khuyến mãi hay không. Nếu đã tồn tại thì sẽ lưu lỗi vào TempData và chuyển hướng admin về trang danh sách khuyến mãi để hiển thị lỗi. Nếu chưa tồn tại thì hệ thống sẽ tạo một đối tượng KhuyenMai mới với các thông tin đã nhận được từ form cùng với ngày tạo hiện tại và số lượt đã dùng ban đầu bằng 0. Đối tượng này sẽ được thêm vào database và lưu lại để đảm bảo rằng khuyến mãi đã được tạo thành công. Cuối cùng chuyển hướng admin về trang danh sách khuyến mãi để thấy rằng khuyến mãi mới đã xuất hiện trong danh sách.
            bool trungMa = _context.KhuyenMais.Any(x => x.MaKhuyenMai == model.MaKhuyenMai);
            if (trungMa)
            {
                TempData["Loi"] = "Mã khuyến mãi này đã tồn tại.";
                return RedirectToAction("KhuyenMai");
            }
            // Nếu chưa tồn tại thì hệ thống sẽ tạo một đối tượng KhuyenMai mới với các thông tin đã nhận được từ form cùng với ngày tạo hiện tại và số lượt đã dùng ban đầu bằng 0. Đối tượng này sẽ được thêm vào database và lưu lại để đảm bảo rằng khuyến mãi đã được tạo thành công. Cuối cùng chuyển hướng admin về trang danh sách khuyến mãi để thấy rằng khuyến mãi mới đã xuất hiện trong danh sách.
            model.NgayTao = GioVN.BayGio;
            // Đối tượng này sẽ được thêm vào database và lưu lại để đảm bảo rằng khuyến mãi đã được tạo thành công. Cuối cùng chuyển hướng admin về trang danh sách khuyến mãi để thấy rằng khuyến mãi mới đã xuất hiện trong danh sách.
            _context.KhuyenMais.Add(model);
            _context.SaveChanges();

            TempData["ThanhCong"] = "Thêm khuyến mãi thành công.";
            return RedirectToAction("KhuyenMai");
        }
        // Đây là action hiển thị trang chỉnh sửa một khuyến mãi đã được tạo trong trang quản trị. Admin có thể chỉnh sửa thông tin của một khuyến mãi đã được tạo trước đó như tên khuyến mãi, mã khuyến mãi, mô tả, kiểu giảm giá, giá trị giảm, ngày bắt đầu, ngày kết thúc, màu nền, thứ tự hiển thị và trạng thái hiển thị. Khi admin chỉnh sửa thông tin và nhấn nút lưu, action này sẽ được gọi với các thông tin đã chỉnh sửa cùng với Id của khuyến mãi đó để xác định chính xác khuyến mãi cần cập nhật. Hàm sẽ kiểm tra quyền admin, sau đó tìm khuyến mãi trong database theo Id. Nếu không tìm thấy khuyến mãi thì trả về lỗi NotFound. Nếu tìm thấy khuyến mãi, hệ thống sẽ trả về view cùng với đối tượng khuyến mãi đã tìm thấy để hiển thị thông tin hiện tại của khuyến mãi đó trên form chỉnh sửa.
        public IActionResult SuaKhuyenMai(int id)
        {
            if (!LaAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            var khuyenMai = _context.KhuyenMais.FirstOrDefault(x => x.Id == id);
            if (khuyenMai == null)
            {
                return NotFound();
            }

            ViewBag.KhuyenMaiSua = khuyenMai;

            var ds = _context.KhuyenMais
                .OrderBy(x => x.ThuTu)
                .ThenByDescending(x => x.Id)
                .ToList();

            return View("KhuyenMai", ds);
        }
        // Đây là action dùng để cập nhật thông tin một khuyến mãi đã được tạo trong trang quản trị. Khi admin chỉnh sửa thông tin và nhấn nút lưu, action này sẽ được gọi với các thông tin đã chỉnh sửa cùng với Id của khuyến mãi đó để xác định chính xác khuyến mãi cần cập nhật. Hàm sẽ kiểm tra quyền admin, sau đó tìm khuyến mãi trong database theo Id. Nếu không tìm thấy khuyến mãi thì trả về lỗi NotFound. Nếu tìm thấy khuyến mãi, hệ thống sẽ chuẩn hóa dữ liệu của khuyến mãi để đảm bảo rằng các giá trị đã nhận được từ form là hợp lệ và có định dạng đúng. Sau đó hàm sẽ kiểm tra các điều kiện hợp lệ của khuyến mãi như tên khuyến mãi không được để trống, mã khuyến mãi phải có định dạng hợp lệ và ngày bắt đầu phải trước ngày kết thúc. Nếu có lỗi nào đó thì sẽ lưu lỗi vào TempData và chuyển hướng admin về trang chỉnh sửa khuyến mãi đó để hiển thị lỗi. Nếu không có lỗi nào thì hệ thống sẽ kiểm tra xem có khuyến mãi nào khác đã tồn tại với cùng một mã khuyến mãi hay không (ngoại trừ chính khuyến mãi đang được chỉnh sửa). Nếu đã tồn tại thì sẽ lưu lỗi vào TempData và chuyển hướng admin về trang chỉnh sửa khuyến mãi đó để hiển thị lỗi. Nếu chưa tồn tại thì hệ thống sẽ cập nhật các thông tin của khuyến mãi đó với các giá trị đã nhận được từ form chỉnh sửa. Sau đó hệ thống sẽ lưu lại để đảm bảo rằng thông tin của khuyến mãi đã được cập nhật thành công. Cuối cùng chuyển hướng admin về trang danh sách khuyến mãi để thấy rằng thông tin của khuyến mãi đã được cập nhật trong danh sách.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SuaKhuyenMai(KhuyenMai model)
        {
            if (!LaAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            var khuyenMai = _context.KhuyenMais.FirstOrDefault(x => x.Id == model.Id);
            if (khuyenMai == null)
            {
                return NotFound();
            }

            ChuanHoaKhuyenMai(model);

            string? loi = KiemTraKhuyenMai(model);
            if (!string.IsNullOrWhiteSpace(loi))
            {
                TempData["Loi"] = loi;
                return RedirectToAction("SuaKhuyenMai", new { id = model.Id });
            }

            bool trungMa = _context.KhuyenMais.Any(x => x.Id != model.Id && x.MaKhuyenMai == model.MaKhuyenMai);
            if (trungMa)
            {
                TempData["Loi"] = "Mã khuyến mãi này đã tồn tại.";
                return RedirectToAction("SuaKhuyenMai", new { id = model.Id });
            }

            khuyenMai.TenKhuyenMai = model.TenKhuyenMai;
            khuyenMai.MaKhuyenMai = model.MaKhuyenMai;
            khuyenMai.MoTa = model.MoTa;
            khuyenMai.KieuGiamGia = model.KieuGiamGia;
            khuyenMai.GiaTriGiam = model.GiaTriGiam;
            khuyenMai.NgayBatDau = model.NgayBatDau;
            khuyenMai.NgayKetThuc = model.NgayKetThuc;
            khuyenMai.MauNen = model.MauNen;
            khuyenMai.ThuTu = model.ThuTu;
            khuyenMai.TrangThai = model.TrangThai;
            khuyenMai.SoLuotDungToiDa = model.SoLuotDungToiDa;

            _context.SaveChanges();

            TempData["ThanhCong"] = "Cập nhật khuyến mãi thành công.";
            return RedirectToAction("KhuyenMai");
        }
        // Đây là action dùng để đổi trạng thái hiển thị của một khuyến mãi đã được tạo trong trang quản trị. Khi admin nhấn nút đổi trạng thái hiển thị, action này sẽ được gọi với Id của khuyến mãi đó để xác định chính xác khuyến mãi cần đổi trạng thái. Hàm sẽ kiểm tra quyền admin, sau đó tìm khuyến mãi trong database theo Id. Nếu không tìm thấy khuyến mãi thì trả về lỗi NotFound. Nếu tìm thấy khuyến mãi, hệ thống sẽ đổi trạng thái của khuyến mãi đó từ "HoatDong" sang "TamNgung" hoặc ngược lại tùy vào trạng thái hiện tại của khuyến mãi. Sau đó hệ thống sẽ lưu lại để đảm bảo rằng trạng thái của khuyến mãi đã được đổi thành công. Cuối cùng chuyển hướng admin về trang danh sách khuyến mãi để thấy rằng trạng thái của khuyến mãi đã được cập nhật trong danh sách.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DoiTrangThaiKhuyenMai(int id)
        {
            if (!LaAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            var khuyenMai = _context.KhuyenMais.FirstOrDefault(x => x.Id == id);
            if (khuyenMai == null)
            {
                return NotFound();
            }

            khuyenMai.TrangThai = khuyenMai.TrangThai == TrangThaiKhuyenMai.HoatDong ? TrangThaiKhuyenMai.TamNgung : TrangThaiKhuyenMai.HoatDong;
            _context.SaveChanges();

            TempData["ThanhCong"] = "Đã đổi trạng thái khuyến mãi.";
            return RedirectToAction("KhuyenMai");
        }
        // Đây là action dùng để xóa một khuyến mãi đã được tạo trong trang quản trị. Khi admin nhấn nút xóa, action này sẽ được gọi với Id của khuyến mãi đó để xác định chính xác khuyến mãi cần xóa. Hàm sẽ kiểm tra quyền admin, sau đó tìm khuyến mãi trong database theo Id. Nếu không tìm thấy khuyến mãi thì trả về lỗi NotFound. Nếu tìm thấy khuyến mãi, hệ thống sẽ kiểm tra xem có đơn nào đã sử dụng khuyến mãi đó hay chưa. Nếu đã có đơn sử dụng thì hệ thống sẽ không xóa khuyến mãi khỏi database mà sẽ đổi trạng thái của khuyến mãi đó sang "TamNgung" để tạm ẩn khỏi danh sách hiển thị nhưng vẫn giữ lại trong database để đảm bảo tính toàn vẹn dữ liệu. Nếu chưa có đơn nào sử dụng thì hệ thống sẽ xóa bản ghi của khuyến mãi đó khỏi database và lưu lại để đảm bảo rằng khuyến mãi đã bị xóa thành công. Cuối cùng chuyển hướng admin về trang danh sách khuyến mãi để thấy rằng khuyến mãi đã bị xóa khỏi danh sách (hoặc đã được chuyển sang trạng thái Tạm ngưng nếu có đơn đã sử dụng).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult XoaKhuyenMai(int id)
        {
            if (!LaAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            var khuyenMai = _context.KhuyenMais.FirstOrDefault(x => x.Id == id);
            if (khuyenMai == null)
            {
                return NotFound();
            }

            bool daCoDonDung = _context.DatTours.Any(x => x.KhuyenMaiId == id);
            if (daCoDonDung)
            {
                khuyenMai.TrangThai = TrangThaiKhuyenMai.TamNgung;
                _context.SaveChanges();

                TempData["Loi"] = "Khuyến mãi đã có đơn sử dụng nên không xóa khỏi dữ liệu, hệ thống đã chuyển sang Tạm ẩn.";
                return RedirectToAction("KhuyenMai");
            }

            _context.KhuyenMais.Remove(khuyenMai);
            _context.SaveChanges();

            TempData["ThanhCong"] = "Đã xóa khuyến mãi.";
            return RedirectToAction("KhuyenMai");
        }
        // Đây là hàm dùng để chuẩn hóa dữ liệu của một khuyến mãi trước khi lưu vào database. Hàm sẽ đảm bảo rằng các giá trị đã nhận được từ form là hợp lệ và có định dạng đúng. Cụ thể, hàm sẽ thực hiện các bước sau:
        private void ChuanHoaKhuyenMai(KhuyenMai model)
        {
            // Hàm sẽ đảm bảo rằng các giá trị đã nhận được từ form là hợp lệ và có định dạng đúng. Cụ thể, hàm sẽ thực hiện các bước sau: Nếu tên khuyến mãi hoặc mã khuyến mãi là null thì sẽ gán thành chuỗi rỗng, sau đó loại bỏ khoảng trắng ở đầu và cuối của tên khuyến mãi và mã khuyến mãi. Mô tả nếu là chuỗi trắng hoặc null thì sẽ gán thành null, ngược lại sẽ loại bỏ khoảng trắng ở đầu và cuối của mô tả. Kiểu giảm giá nếu không phải là "PhanTram" thì sẽ gán thành "Tien". Màu nền nếu là chuỗi trắng hoặc null thì sẽ gán thành "#ff6b4a". Trạng thái nếu là chuỗi trắng hoặc null thì sẽ gán thành "HoatDong". Thứ tự nếu nhỏ hơn hoặc bằng 0 thì sẽ gán thành 1.
            model.TenKhuyenMai = (model.TenKhuyenMai ?? "").Trim();
            model.MaKhuyenMai = (model.MaKhuyenMai ?? "").Trim().ToUpper();
            model.MoTa = string.IsNullOrWhiteSpace(model.MoTa) ? null : model.MoTa.Trim();
            // Kiểu giảm giá nếu không phải là "PhanTram" thì sẽ gán thành "Tien". Màu nền nếu là chuỗi trắng hoặc null thì sẽ gán thành "#ff6b4a". Trạng thái nếu là chuỗi trắng hoặc null thì sẽ gán thành "HoatDong". Thứ tự nếu nhỏ hơn hoặc bằng 0 thì sẽ gán thành 1.
            if (model.KieuGiamGia != "PhanTram")
            {
                model.KieuGiamGia = "Tien";
            }
            // Màu nền nếu là chuỗi trắng hoặc null thì sẽ gán thành "#ff6b4a". Trạng thái nếu là chuỗi trắng hoặc null thì sẽ gán thành "HoatDong". Thứ tự nếu nhỏ hơn hoặc bằng 0 thì sẽ gán thành 1.
            if (string.IsNullOrWhiteSpace(model.MauNen))
            {
                model.MauNen = "#ff6b4a";
            }
            // Trạng thái nếu là chuỗi trắng hoặc null thì sẽ gán thành "HoatDong". Thứ tự nếu nhỏ hơn hoặc bằng 0 thì sẽ gán thành 1.
            if (model.TrangThai != TrangThaiKhuyenMai.HoatDong && model.TrangThai != TrangThaiKhuyenMai.TamNgung)
            {
                model.TrangThai = TrangThaiKhuyenMai.HoatDong;
            }

            if (model.ThuTu <= 0)
            {
                model.ThuTu = 1;
            }
        }
        // Đây là hàm dùng để kiểm tra các điều kiện hợp lệ của một khuyến mãi trước khi lưu vào database. Hàm sẽ trả về một chuỗi lỗi nếu có điều kiện nào đó không hợp lệ, hoặc trả về null nếu tất cả các điều kiện đều hợp lệ. Cụ thể, hàm sẽ kiểm tra các điều kiện sau: Tên khuyến mãi không được để trống. Mã khuyến mãi không được để trống. Giá trị giảm phải lớn hơn 0. Nếu kiểu giảm giá là "PhanTram" thì giá trị giảm không được vượt quá 100%. Ngày kết thúc không được nhỏ hơn ngày bắt đầu. Nếu số lượt dùng tối đa có giá trị thì phải lớn hơn 0.
        private string? KiemTraKhuyenMai(KhuyenMai model)
        {
            if (string.IsNullOrWhiteSpace(model.TenKhuyenMai))
            {
                return "Vui lòng nhập tên khuyến mãi.";
            }

            if (string.IsNullOrWhiteSpace(model.MaKhuyenMai))
            {
                return "Vui lòng nhập mã khuyến mãi.";
            }

            if (model.GiaTriGiam <= 0)
            {
                return "Giá trị giảm phải lớn hơn 0.";
            }

            if (model.KieuGiamGia == "PhanTram" && model.GiaTriGiam > 100)
            {
                return "Giảm theo phần trăm không được vượt quá 100%.";
            }

            if (model.NgayKetThuc.Date < model.NgayBatDau.Date)
            {
                return "Ngày kết thúc không được nhỏ hơn ngày bắt đầu.";
            }

            if (model.SoLuotDungToiDa.HasValue && model.SoLuotDungToiDa.Value <= 0)
            {
                return "Số lượt dùng tối đa phải lớn hơn 0.";
            }

            return null;
        }
        // Đây là action hiển thị danh sách các cuộc trò chuyện liên hệ giữa khách hàng và admin trong trang quản trị. Admin có thể xem danh sách các cuộc trò chuyện, tìm kiếm theo tên khách hàng, email hoặc nội dung tin nhắn, xem chi tiết từng cuộc trò chuyện và trả lời tin nhắn của khách hàng. Admin cũng có thể xóa toàn bộ cuộc trò chuyện với một khách hàng nếu cần thiết.
        public IActionResult TinNhan(string search)
        {
            if (!LaAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }
            // Đoạn này truy vấn danh sách các cuộc trò chuyện liên hệ giữa khách hàng và admin từ database, bao gồm các thông tin liên quan như tên khách hàng, email, nội dung tin nhắn, người gửi, ngày gửi và trạng thái đã đọc. Sau đó áp dụng bộ lọc tìm kiếm theo tên khách hàng, email hoặc nội dung tin nhắn nếu admin nhập vào ô tìm kiếm. Tiếp theo nhóm các tin nhắn theo tài khoản khách hàng để tạo thành các cuộc trò chuyện riêng biệt. Mỗi cuộc trò chuyện sẽ lấy thông tin của tin nhắn cuối cùng để hiển thị trong danh sách cuộc trò chuyện, đồng thời đếm số lượng tin nhắn chưa đọc trong cuộc trò chuyện đó. Cuối cùng trả về view TinNhan.cshtml cùng với danh sách các cuộc trò chuyện đã được lọc và sắp xếp theo thời gian gửi của tin nhắn cuối cùng để hiển thị trên trang quản lý tin nhắn.
            var ds = _context.TinNhanLienHes
                .Include(x => x.TaiKhoan)
                .AsQueryable();
            // Sau đó áp dụng bộ lọc tìm kiếm theo tên khách hàng, email hoặc nội dung tin nhắn nếu admin nhập vào ô tìm kiếm. Tiếp theo nhóm các tin nhắn theo tài khoản khách hàng để tạo thành các cuộc trò chuyện riêng biệt. Mỗi cuộc trò chuyện sẽ lấy thông tin của tin nhắn cuối cùng để hiển thị trong danh sách cuộc trò chuyện, đồng thời đếm số lượng tin nhắn chưa đọc trong cuộc trò chuyện đó. Cuối cùng trả về view TinNhan.cshtml cùng với danh sách các cuộc trò chuyện đã được lọc và sắp xếp theo thời gian gửi của tin nhắn cuối cùng để hiển thị trên trang quản lý tin nhắn.
            if (!string.IsNullOrWhiteSpace(search))
            {
                // Tên, email, số điện thoại lấy từ tài khoản khách, không còn sao chép vào từng tin
                ds = ds.Where(x =>
                    x.TaiKhoan!.HoTen.Contains(search) ||
                    (x.TaiKhoan.Email != null && x.TaiKhoan.Email.Contains(search)) ||
                    (x.TaiKhoan.SoDienThoai != null && x.TaiKhoan.SoDienThoai.Contains(search)) ||
                    x.NoiDung.Contains(search)
                );
            }
            // Tiếp theo nhóm các tin nhắn theo tài khoản khách hàng để tạo thành các cuộc trò chuyện riêng biệt. Mỗi cuộc trò chuyện sẽ lấy thông tin của tin nhắn cuối cùng để hiển thị trong danh sách cuộc trò chuyện, đồng thời đếm số lượng tin nhắn chưa đọc trong cuộc trò chuyện đó. Cuối cùng trả về view TinNhan.cshtml cùng với danh sách các cuộc trò chuyện đã được lọc và sắp xếp theo thời gian gửi của tin nhắn cuối cùng để hiển thị trên trang quản lý tin nhắn.
            var cuocTroChuyen = ds
                .AsEnumerable()
                .GroupBy(x => x.TaiKhoanId)
                .Select(g =>
                {
                    var tinCuoi = g.OrderByDescending(x => x.NgayGui).First();

                    return new TinNhanCuocTroChuyenViewModel
                    {
                        TaiKhoanId = g.Key,
                        HoTen = tinCuoi.TaiKhoan?.HoTen ?? "Khách hàng",
                        Email = DinhDanhTaiKhoan.HienThi(tinCuoi.TaiKhoan),
                        TinNhanCuoi = tinCuoi.NoiDung,
                        NguoiGuiCuoi = tinCuoi.NguoiGui,
                        NgayGuiCuoi = tinCuoi.NgayGui,
                        ChuaDoc = g.Count(x => x.NguoiGui != "admin" && !x.DaDoc)
                    };
                })
                .OrderByDescending(x => x.NgayGuiCuoi) // Sắp xếp theo thời gian gửi của tin nhắn cuối cùng trong cuộc trò chuyện, tin nhắn mới nhất sẽ hiển thị ở đầu danh sách.
                .ToList();
            // Cuối cùng trả về view TinNhan.cshtml cùng với danh sách các cuộc trò chuyện đã được lọc và sắp xếp theo thời gian gửi của tin nhắn cuối cùng để hiển thị trên trang quản lý tin nhắn.
            ViewBag.Search = search;
            ViewBag.TongCuocTroChuyen = cuocTroChuyen.Count;
            ViewBag.ChuaDoc = cuocTroChuyen.Sum(x => x.ChuaDoc);

            return View(cuocTroChuyen);
        }
        // Đây là action hiển thị chi tiết nội dung các tin nhắn trong một cuộc trò chuyện liên hệ giữa khách hàng và admin. Admin có thể xem toàn bộ nội dung tin nhắn đã gửi và nhận trong cuộc trò chuyện đó, đồng thời trả lời tin nhắn của khách hàng nếu cần thiết. Khi admin truy cập vào chi tiết cuộc trò chuyện, hệ thống sẽ đánh dấu tất cả các tin nhắn chưa đọc của khách hàng trong cuộc trò chuyện đó là đã đọc để cập nhật trạng thái đã đọc của tin nhắn. Admin cũng có thể xóa toàn bộ cuộc trò chuyện với khách hàng đó nếu cần thiết.
        public IActionResult ChiTietTinNhan(int taiKhoanId)
        {
            if (!LaAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            var ds = _context.TinNhanLienHes
                .Include(x => x.TaiKhoan)
                .Where(x => x.TaiKhoanId == taiKhoanId)
                .OrderBy(x => x.NgayGui)
                .ToList();
            // Khi admin truy cập vào chi tiết cuộc trò chuyện, hệ thống sẽ đánh dấu tất cả các tin nhắn chưa đọc của khách hàng trong cuộc trò chuyện đó là đã đọc để cập nhật trạng thái đã đọc của tin nhắn. Admin cũng có thể xóa toàn bộ cuộc trò chuyện với khách hàng đó nếu cần thiết.
            foreach (var tin in ds.Where(x => x.NguoiGui != "admin" && !x.DaDoc))
            {
                tin.DaDoc = true;
                tin.NgayDoc = GioVN.BayGio;
            }

            _context.SaveChanges();

            ViewBag.TaiKhoanId = taiKhoanId;
            var khachHang = _context.TaiKhoans.AsNoTracking().FirstOrDefault(x => x.Id == taiKhoanId);
            ViewBag.KhachHang = khachHang?.HoTen ?? "Khách hàng";
            ViewBag.Email = DinhDanhTaiKhoan.HienThi(khachHang);

            return View(ds);
        }
        // Đây là action dùng để trả lời tin nhắn của khách hàng trong một cuộc trò chuyện liên hệ giữa khách hàng và admin. Khi admin điền nội dung trả lời vào form trả lời tin nhắn và nhấn nút gửi, action này sẽ được gọi với Id của tài khoản khách hàng đó để xác định chính xác cuộc trò chuyện cần trả lời cùng với nội dung trả lời mà admin đã nhập. Hàm sẽ kiểm tra quyền admin, sau đó kiểm tra xem nội dung trả lời có hợp lệ hay không (không được để trống hoặc chỉ chứa khoảng trắng). Nếu không hợp lệ thì sẽ chuyển hướng admin về trang chi tiết cuộc trò chuyện đó để hiển thị lỗi. Nếu hợp lệ thì hệ thống sẽ tạo một đối tượng TinNhanLienHe mới với các thông tin sau: Id tài khoản khách hàng, tên người gửi là "Admin TravelVN", email lấy từ tài khoản khách hàng nếu có hoặc để trống nếu không có, nội dung lấy từ form trả lời đã được loại bỏ khoảng trắng ở đầu và cuối, người gửi là "admin", ngày gửi là thời gian hiện tại, trạng thái đã đọc là true và ngày đọc là thời gian hiện tại. Đối tượng này sẽ được thêm vào database và lưu lại để đảm bảo rằng tin nhắn trả lời đã được gửi thành công. Cuối cùng chuyển hướng admin về trang chi tiết cuộc trò chuyện đó để thấy rằng tin nhắn trả lời đã xuất hiện trong danh sách tin nhắn của cuộc trò chuyện.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TraLoiTinNhan(int taiKhoanId, string noiDung)
        {
            if (!LaAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }
            // Nếu không hợp lệ thì sẽ chuyển hướng admin về trang chi tiết cuộc trò chuyện đó để hiển thị lỗi. Nếu hợp lệ thì hệ thống sẽ tạo một đối tượng TinNhanLienHe mới với các thông tin sau: Id tài khoản khách hàng, tên người gửi là "Admin TravelVN", email lấy từ tài khoản khách hàng nếu có hoặc để trống nếu không có, nội dung lấy từ form trả lời đã được loại bỏ khoảng trắng ở đầu và cuối, người gửi là "admin", ngày gửi là thời gian hiện tại, trạng thái đã đọc là true và ngày đọc là thời gian hiện tại. Đối tượng này sẽ được thêm vào database và lưu lại để đảm bảo rằng tin nhắn trả lời đã được gửi thành công. Cuối cùng chuyển hướng admin về trang chi tiết cuộc trò chuyện đó để thấy rằng tin nhắn trả lời đã xuất hiện trong danh sách tin nhắn của cuộc trò chuyện.
            if (string.IsNullOrWhiteSpace(noiDung))
            {
                return RedirectToAction("ChiTietTinNhan", new { taiKhoanId });
            }

            var khach = _context.TaiKhoans.FirstOrDefault(x => x.Id == taiKhoanId);// Lấy thông tin tài khoản khách hàng để lấy email nếu có, nếu không có thì để trống. Điều này giúp đảm bảo rằng tin nhắn trả lời của admin sẽ có thông tin email của khách hàng nếu có sẵn, hoặc sẽ không có thông tin email nếu tài khoản khách hàng đó không tồn tại hoặc không có email.
            // Điều này giúp đảm bảo rằng tin nhắn trả lời của admin sẽ có thông tin email của khách hàng nếu có sẵn, hoặc sẽ không có thông tin email nếu tài khoản khách hàng đó không tồn tại hoặc không có email.
            var tinNhan = new TinNhanLienHe
            {
                TaiKhoanId = taiKhoanId,
                NoiDung = noiDung.Trim(),
                NguoiGui = "admin",
                NgayGui = GioVN.BayGio,
                DaDoc = true,
                NgayDoc = GioVN.BayGio
            };
            // Đối tượng này sẽ được thêm vào database và lưu lại để đảm bảo rằng tin nhắn trả lời đã được gửi thành công. Cuối cùng chuyển hướng admin về trang chi tiết cuộc trò chuyện đó để thấy rằng tin nhắn trả lời đã xuất hiện trong danh sách tin nhắn của cuộc trò chuyện.
            _context.TinNhanLienHes.Add(tinNhan);
            _context.SaveChanges();

            return RedirectToAction("ChiTietTinNhan", new { taiKhoanId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult XoaCuocTroChuyen(int taiKhoanId)
        {
            if (!LaAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            var ds = _context.TinNhanLienHes
                .Where(x => x.TaiKhoanId == taiKhoanId)
                .ToList();

            _context.TinNhanLienHes.RemoveRange(ds);
            _context.SaveChanges();

            return RedirectToAction("TinNhan");
        }
        [HttpGet]
        public IActionResult LayChiTietTinNhan(int taiKhoanId)
        {
            if (!LaAdmin())
            {
                return Json(new List<object>());
            }

            var ds = _context.TinNhanLienHes
                .Include(x => x.TaiKhoan)
                .Where(x => x.TaiKhoanId == taiKhoanId)
                .OrderBy(x => x.NgayGui)
                .ToList();

            foreach (var tin in ds.Where(x => x.NguoiGui != "admin" && !x.DaDoc))
            {
                tin.DaDoc = true;
                tin.NgayDoc = GioVN.BayGio;
            }

            _context.SaveChanges();

            var result = ds.Select(x => new
            {
                x.Id,
                HoTen = x.NguoiGui == "admin" ? "Admin TravelVN" : x.TaiKhoan?.HoTen ?? "Khách hàng",
                x.NoiDung,
                x.NguoiGui,
                ThoiGian = x.NgayGui.ToString("dd/MM/yyyy HH:mm")
            });

            return Json(result);
        }
        // Đây là action dùng để trả lời tin nhắn của khách hàng trong một cuộc trò chuyện liên hệ giữa khách hàng và admin thông qua AJAX. Khi admin điền nội dung trả lời vào form trả lời tin nhắn và nhấn nút gửi, action này sẽ được gọi với Id của tài khoản khách hàng đó để xác định chính xác cuộc trò chuyện cần trả lời cùng với nội dung trả lời mà admin đã nhập. Hàm sẽ kiểm tra quyền admin, sau đó kiểm tra xem nội dung trả lời có hợp lệ hay không (không được để trống hoặc chỉ chứa khoảng trắng). Nếu không hợp lệ thì sẽ trả về JSON với thuộc tính success là false để hiển thị lỗi. Nếu hợp lệ thì hệ thống sẽ tạo một đối tượng TinNhanLienHe mới với các thông tin sau: Id tài khoản khách hàng, tên người gửi là "Admin TravelVN", email lấy từ tài khoản khách hàng nếu có hoặc để trống nếu không có, nội dung lấy từ form trả lời đã được loại bỏ khoảng trắng ở đầu và cuối, người gửi là "admin", ngày gửi là thời gian hiện tại, trạng thái đã đọc là true và ngày đọc là thời gian hiện tại. Đối tượng này sẽ được thêm vào database và lưu lại để đảm bảo rằng tin nhắn trả lời đã được gửi thành công. Cuối cùng trả về JSON với thuộc tính success là true để hiển thị rằng tin nhắn trả lời đã được gửi thành công.
       // k load lại trang 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TraLoiTinNhanAjax(int taiKhoanId, string noiDung)
        {
            if (!LaAdmin())
            {
                return Json(new { success = false });
            }

            if (string.IsNullOrWhiteSpace(noiDung))
            {
                return Json(new { success = false });
            }

            var khach = _context.TaiKhoans.FirstOrDefault(x => x.Id == taiKhoanId);

            var tinNhan = new TinNhanLienHe
            {
                TaiKhoanId = taiKhoanId,
                NoiDung = noiDung.Trim(),
                NguoiGui = "admin",
                NgayGui = GioVN.BayGio,
                DaDoc = true,
                NgayDoc = GioVN.BayGio
            };

            _context.TinNhanLienHes.Add(tinNhan);
            _context.SaveChanges();

            return Json(new { success = true });
        }
        // Đây là action hiển thị danh sách nhân viên trong trang quản trị. Admin có thể xem danh sách các nhân viên đã được tạo, tìm kiếm theo tên, số điện thoại hoặc loại nhân viên, thêm mới nhân viên, đổi trạng thái làm việc của nhân viên và xóa nhân viên nếu cần thiết.
        public IActionResult NhanSu(string search)
        {
            if (!LaAdmin()) return RedirectToAction("DangNhap", "TaiKhoan");

            var ds = _context.NhanViens
                .Include(x => x.TaiKhoan)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                ds = ds.Where(x =>
                    x.TaiKhoan!.HoTen.Contains(search) ||
                    (x.TaiKhoan.SoDienThoai != null && x.TaiKhoan.SoDienThoai.Contains(search)) ||
                    (x.TaiKhoan.Email != null && x.TaiKhoan.Email.Contains(search)) ||
                    x.TaiKhoan.VaiTro.Contains(search));
            }

            ViewBag.Search = search;
            return View(ds.OrderByDescending(x => x.Id).ToList());
        }
        // Đây là action dùng để thêm mới một nhân viên vào hệ thống trong trang quản trị. Khi admin điền thông tin của nhân viên mới vào form thêm nhân viên và nhấn nút lưu, action này sẽ được gọi với các thông tin đã nhập từ form như họ tên, số điện thoại, loại nhân viên, tài khoản đăng nhập, mật khẩu và ghi chú (nếu có). Hàm sẽ kiểm tra quyền admin, sau đó kiểm tra xem các thông tin bắt buộc đã được nhập đầy đủ hay chưa. Nếu có thông tin nào đó chưa hợp lệ hoặc thiếu thì sẽ lưu lỗi vào TempData và chuyển hướng admin về trang danh sách nhân viên để hiển thị lỗi. Nếu tất cả các thông tin đều hợp lệ thì hệ thống sẽ kiểm tra xem tài khoản đăng nhập đã tồn tại trong database hay chưa. Nếu đã tồn tại thì sẽ lưu lỗi vào TempData và chuyển hướng admin về trang danh sách nhân viên để hiển thị lỗi. Nếu chưa tồn tại thì hệ thống sẽ tạo một đối tượng TaiKhoan mới với các thông tin đã nhận được từ form cùng với vai trò tương ứng với loại nhân viên (huongdanvien hoặc taixe) và ngày tạo hiện tại. Đối tượng TaiKhoan này sẽ được thêm vào database và lưu lại để đảm bảo rằng tài khoản đã được tạo thành công. Sau đó hệ thống sẽ tạo một đối tượng NhanVien mới với các thông tin đã nhận được từ form cùng với Id của tài khoản vừa tạo, trạng thái đang làm việc là true và ngày tạo hiện tại. Đối tượng NhanVien này sẽ được thêm vào database và lưu lại để đảm bảo rằng nhân viên đã được thêm thành công. Cuối cùng chuyển hướng admin về trang danh sách nhân viên để thấy rằng nhân viên mới đã xuất hiện trong danh sách.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ThemNhanVien(
            string hoTen,
            string soDienThoai,
            string loaiNhanVien,
            string taiKhoanDangNhap,
            string matKhau,
            string? ghiChu)
        {
            // Khi admin điền thông tin của nhân viên mới vào form thêm nhân viên và nhấn nút lưu, action này sẽ được gọi với các thông tin đã nhập từ form như họ tên, số điện thoại, loại nhân viên, tài khoản đăng nhập, mật khẩu và ghi chú (nếu có). Hàm sẽ kiểm tra quyền admin, sau đó kiểm tra xem các thông tin bắt buộc đã được nhập đầy đủ hay chưa. Nếu có thông tin nào đó chưa hợp lệ hoặc thiếu thì sẽ lưu lỗi vào TempData và chuyển hướng admin về trang danh sách nhân viên để hiển thị lỗi. Nếu tất cả các thông tin đều hợp lệ thì hệ thống sẽ kiểm tra xem tài khoản đăng nhập đã tồn tại trong database hay chưa. Nếu đã tồn tại thì sẽ lưu lỗi vào TempData và chuyển hướng admin về trang danh sách nhân viên để hiển thị lỗi. Nếu chưa tồn tại thì hệ thống sẽ tạo một đối tượng TaiKhoan mới với các thông tin đã nhận được từ form cùng với vai trò tương ứng với loại nhân viên (huongdanvien hoặc taixe) và ngày tạo hiện tại. Đối tượng TaiKhoan này sẽ được thêm vào database và lưu lại để đảm bảo rằng tài khoản đã được tạo thành công. Sau đó hệ thống sẽ tạo một đối tượng NhanVien mới với các thông tin đã nhận được từ form cùng với Id của tài khoản vừa tạo, trạng thái đang làm việc là true và ngày tạo hiện tại. Đối tượng NhanVien này sẽ được thêm vào database và lưu lại để đảm bảo rằng nhân viên đã được thêm thành công. Cuối cùng chuyển hướng admin về trang danh sách nhân viên để thấy rằng nhân viên mới đã xuất hiện trong danh sách.
            if (!LaAdmin()) return RedirectToAction("DangNhap", "TaiKhoan");

            if (string.IsNullOrWhiteSpace(hoTen) ||
                string.IsNullOrWhiteSpace(loaiNhanVien) ||
                string.IsNullOrWhiteSpace(taiKhoanDangNhap) ||
                string.IsNullOrWhiteSpace(matKhau))
            {
                TempData["error"] = "Vui lòng nhập đầy đủ thông tin nhân viên.";
                return RedirectToAction("NhanSu");
            }

            // Tài khoản đăng nhập của nhân viên là email hoặc số điện thoại, như khách
            var dangNhap = taiKhoanDangNhap.Trim();
            bool laEmail = DinhDanhTaiKhoan.LaEmail(dangNhap);
            bool laSoDienThoai = DinhDanhTaiKhoan.LaSoDienThoai(dangNhap);
            if (!laEmail && !laSoDienThoai)
            {
                TempData["error"] = "Tài khoản đăng nhập phải là email hoặc số điện thoại 10 số.";
                return RedirectToAction("NhanSu");
            }

            string? email = laEmail ? dangNhap : null;
            // Số điện thoại liên hệ của nhân viên lưu vào tài khoản nếu đăng nhập bằng email
            string? soDienThoaiTk = laSoDienThoai ? dangNhap : DinhDanhTaiKhoan.ChuanHoa(soDienThoai);
            if (soDienThoaiTk != null && !DinhDanhTaiKhoan.LaSoDienThoai(soDienThoaiTk))
            {
                TempData["error"] = "Số điện thoại gồm 10 chữ số, bắt đầu bằng 0.";
                return RedirectToAction("NhanSu");
            }

            if (_context.TaiKhoans.DaDuocDung(email, soDienThoaiTk))
            {
                TempData["error"] = "Email hoặc số điện thoại này đã thuộc tài khoản khác.";
                return RedirectToAction("NhanSu");
            }

            var vaiTro = loaiNhanVien == "HuongDanVien" ? "huongdanvien" : "taixe";

            var taiKhoan = new TaiKhoan
            {
                HoTen = hoTen.Trim(),
                Email = email,
                SoDienThoai = soDienThoaiTk,
                MatKhau = BCrypt.Net.BCrypt.HashPassword(matKhau),
                VaiTro = vaiTro,
                NgayTao = GioVN.BayGio
            };

            _context.TaiKhoans.Add(taiKhoan);
            _context.SaveChanges();

            var nhanVien = new NhanVien
            {
                TaiKhoanId = taiKhoan.Id,
                DangLamViec = true,
                GhiChu = ghiChu,
                NgayTao = GioVN.BayGio
            };

            _context.NhanViens.Add(nhanVien);
            _context.SaveChanges();

            TempData["success"] = "Thêm nhân viên thành công.";
            return RedirectToAction("NhanSu");
        }
        // Đây là action dùng để đổi trạng thái làm việc của một nhân viên trong trang quản trị. Khi admin nhấn nút đổi trạng thái làm việc của một nhân viên, action này sẽ được gọi với Id của nhân viên đó để xác định chính xác nhân viên cần đổi trạng thái. Hàm sẽ kiểm tra quyền admin, sau đó tìm nhân viên trong database theo Id. Nếu không tìm thấy nhân viên thì trả về lỗi NotFound. Nếu tìm thấy nhân viên, hệ thống s�� đổi trạng thái làm việc của nhân viên đó từ đang làm việc sang nghỉ việc hoặc ngược lại tùy vào trạng thái hiện tại của nhân viên. Sau đó hệ thống sẽ lưu lại để đảm bảo rằng trạng thái làm việc của nhân viên đã được đổi thành công. Cuối cùng chuyển hướng admin về trang danh sách nhân viên để thấy rằng trạng thái làm việc của nhân viên đã được cập nhật trong danh sách.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DoiTrangThaiNhanVien(int id)
        {
            if (!LaAdmin()) return RedirectToAction("DangNhap", "TaiKhoan");

            var nv = _context.NhanViens.FirstOrDefault(x => x.Id == id);
            if (nv == null) return NotFound();

            nv.DangLamViec = !nv.DangLamViec;
            _context.SaveChanges();

            return RedirectToAction("NhanSu");
        }
    }
}