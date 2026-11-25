using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using webdulich.Models;

namespace webdulich.Services
{
    // Chặn trang quản trị theo nhóm quyền của tài khoản admin (RB-51). Đăng ký chung cho mọi controller (Program.cs),
    // chỉ xét khi người đang đăng nhập là admin và trang thuộc controller Admin*; các kiểm tra "có phải admin không"
    // sẵn có trong từng controller giữ nguyên. Trang nào thuộc nhóm nào xem QuyenAdmin.CanCo.
    public class KiemTraQuyenAdmin : IActionFilter
    {
        private readonly DuLichDbContext _db;

        public KiemTraQuyenAdmin(DuLichDbContext db)
        {
            _db = db;
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            var http = context.HttpContext;
            if (http.Session.GetString("role") != "admin")
            {
                return;
            }

            var tenController = context.RouteData.Values["controller"]?.ToString();
            if (tenController == null || !tenController.StartsWith("Admin", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // Vai trò trong phiên có thể đã cũ: admin chính vừa hạ tài khoản này xuống thì đăng xuất ngay, không đợi phiên hết hạn
            var taiKhoanId = http.Session.GetInt32("taiKhoanId");
            if (!taiKhoanId.HasValue || !_db.TaiKhoans.Any(t => t.Id == taiKhoanId.Value && t.VaiTro == "admin"))
            {
                http.Session.Clear();
                context.Result = new RedirectToActionResult("DangNhap", "TaiKhoan", null);
                return;
            }

            var canCo = QuyenAdmin.CanCo(tenController, context.RouteData.Values["action"]?.ToString());
            if (canCo == null || QuyenAdmin.Co(http, _db, canCo))
            {
                return;
            }

            var tenQuyen = QuyenAdmin.TatCa.First(q => q.Ma == canCo).Ten;

            // Gọi bằng JavaScript (fetch, AJAX): trả mã 403 kèm câu báo, không trả cả trang
            bool laGoiNgam = http.Request.Headers.XRequestedWith == "XMLHttpRequest"
                || http.Request.Headers.Accept.ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase);
            if (laGoiNgam)
            {
                context.Result = new ObjectResult(new { loi = $"Tài khoản chưa có quyền \"{tenQuyen}\"." }) { StatusCode = StatusCodes.Status403Forbidden };
                return;
            }

            if (context.Controller is Controller controller)
            {
                controller.ViewData["Title"] = "Chưa có quyền";
                controller.ViewData["TenQuyen"] = tenQuyen;
                context.Result = new ViewResult
                {
                    ViewName = "~/Views/Shared/KhongCoQuyen.cshtml",
                    ViewData = controller.ViewData,
                    TempData = controller.TempData,
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }
            else
            {
                context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
        }
    }
}
