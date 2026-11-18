using System.Net;
using System.Net.Mail;
using System.Text;

namespace webdulich.Services
{
    // Một lá thư cần gửi: nội dung HTML kèm bản chữ thường cho hộp thư không hiện HTML
    public record ThuEmail(string Den, string TenNguoiNhan, string TieuDe, string NoiDungHtml, string NoiDungChu);

    // Gửi email. Cấu hình mục "Email" (appsettings), đổi chế độ không cần sửa code:
    //   CheDo = "File"    : không gửi thật, mỗi thư lưu thành file .eml trong ThuMucFile (dùng trên máy để thử)
    //   CheDo = "Smtp"    : gửi qua máy chủ SMTP (SMTP relay của host). Thông số Smtp:May, Cong, BatSsl, TenDangNhap, MatKhau
    //                       chỉ đặt trong cấu hình riêng của host (appsettings.Production.json), không commit.
    //   CheDo = "TatNhac" : tắt hẳn việc gửi
    public interface IGuiEmail
    {
        bool DangBat { get; }

        // Ném lỗi khi gửi không được để nơi gọi ghi lại và thử lại sau
        void Gui(ThuEmail thu);
    }

    public class GuiEmail : IGuiEmail
    {
        private readonly IConfiguration _cauHinh;
        private readonly string _thuMucFile;

        public GuiEmail(IConfiguration cauHinh, IWebHostEnvironment moiTruong)
        {
            _cauHinh = cauHinh;
            var thuMuc = cauHinh["Email:ThuMucFile"];
            if (string.IsNullOrWhiteSpace(thuMuc))
            {
                thuMuc = "ThuDaGui";
            }
            // Đường dẫn tương đối tính từ thư mục dự án; nằm ngoài wwwroot để không ai tải được qua web
            _thuMucFile = Path.IsPathRooted(thuMuc) ? thuMuc : Path.Combine(moiTruong.ContentRootPath, thuMuc);
        }

        private string CheDo => _cauHinh["Email:CheDo"] ?? "File";

        public bool DangBat => CheDo is "File" or "Smtp";

        public void Gui(ThuEmail thu)
        {
            using var mail = new MailMessage
            {
                From = new MailAddress(
                    _cauHinh["Email:DiaChiGui"] ?? "no-reply@travelvn.test",
                    _cauHinh["Email:TenNguoiGui"] ?? "TravelVN",
                    Encoding.UTF8),
                Subject = thu.TieuDe,
                SubjectEncoding = Encoding.UTF8,
                Body = thu.NoiDungChu,
                BodyEncoding = Encoding.UTF8,
                IsBodyHtml = false
            };
            mail.To.Add(new MailAddress(thu.Den, thu.TenNguoiNhan, Encoding.UTF8));
            mail.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(thu.NoiDungHtml, Encoding.UTF8, "text/html"));

            switch (CheDo)
            {
                case "File":
                    Directory.CreateDirectory(_thuMucFile);
                    using (var client = new SmtpClient
                    {
                        DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory,
                        PickupDirectoryLocation = _thuMucFile
                    })
                    {
                        client.Send(mail);
                    }
                    break;

                case "Smtp":
                    var may = _cauHinh["Email:Smtp:May"];
                    if (string.IsNullOrWhiteSpace(may))
                    {
                        throw new InvalidOperationException("Chưa cấu hình Email:Smtp:May.");
                    }
                    using (var client = new SmtpClient(may, _cauHinh.GetValue("Email:Smtp:Cong", 587))
                    {
                        EnableSsl = _cauHinh.GetValue("Email:Smtp:BatSsl", true),
                        DeliveryMethod = SmtpDeliveryMethod.Network,
                        Timeout = 20000
                    })
                    {
                        var tenDangNhap = _cauHinh["Email:Smtp:TenDangNhap"];
                        if (!string.IsNullOrEmpty(tenDangNhap))
                        {
                            client.Credentials = new NetworkCredential(tenDangNhap, _cauHinh["Email:Smtp:MatKhau"]);
                        }
                        client.Send(mail);
                    }
                    break;

                default:
                    throw new InvalidOperationException("Việc gửi email đang tắt.");
            }
        }
    }
}
