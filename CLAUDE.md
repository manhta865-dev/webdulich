# TravelVN: đồ án chuyên ngành Hệ thống thông tin

Claude Code đọc file này đầu mỗi phiên. Hãy đọc kỹ trước khi làm bất cứ việc gì.

## Dự án

- **Đề tài (giáo viên đã duyệt):** Giải quyết khó khăn thu thông tin và thu tiền khi đặt tour theo nhóm: xây dựng hệ thống đặt tour nhóm với thanh toán chia phần trên ASP.NET Core MVC và SQL Server.
- **Nền:** web TravelVN của đồ án cơ sở. Phát triển tiếp trên nền hiện có, không viết lại, không làm app di động.
- **Thời gian:** 10 tuần, một sinh viên làm. Kế hoạch ở `docs/KE_HOACH.md`. Mốc giáo viên: 25/10 duyệt bài toán và yêu cầu, 08/11 duyệt mô hình dữ liệu và hệ thống, 29/11 MVP, 20/12 hoàn thiện, 21–27/12 nộp LMS.
- **Đọc thêm:** `docs/DE_TAI.md` (đề tài, phạm vi, yêu cầu), `docs/KHAO_SAT.md` (khảo sát), `docs/KE_HOACH.md` (lịch).
- **Ngôn ngữ:** trả lời, viết commit và tài liệu bằng tiếng Việt. Người dùng là sinh viên đang học, nên giải thích ngắn gọn từng bước, tránh thuật ngữ không cần thiết.

**Bài toán (giả thuyết, sẽ kiểm chứng bằng khảo sát):** khi cả nhóm đi tour, một người phải đứng ra đặt, thu giấy tờ từng người, ứng tiền rồi đi đòi lại; không biết ai đã trả, không có hạn chốt.

**Giải pháp:** mỗi chỗ là một "ghế có chủ riêng". Trưởng nhóm tạo đơn nhóm, giữ chỗ có hạn (đề xuất 24 giờ) và gửi link mời. Mỗi thành viên tự nhận ghế, tự điền thông tin, tự trả phần mình bằng QR chuyển khoản (giai đoạn đầu admin xác nhận thủ công, tự động qua webhook chỉ làm nếu dư thời gian). Ghế quá hạn chưa trả được nhả. Trưởng nhóm thấy tiến độ nhưng không thấy số giấy tờ của người khác.

Ba phần việc: (1) hoàn thiện sản phẩm với CSDL chuẩn 3NF, (2) làm đúng quy trình tài liệu, (3) thiết kế lại giao diện **sau khi có MVP** (theo yêu cầu giáo viên, giao diện đi sau nghiệp vụ). Quy trình tài liệu gồm (khảo sát, use case, class, sequence, activity, lược đồ quan hệ, ràng buộc toàn vẹn) theo mẫu báo cáo đồ án cơ sở.

## Quy tắc làm việc (bắt buộc)

1. **Hỏi trước khi sửa.** Với mọi thay đổi, trình bày kế hoạch (sửa gì, file nào, ảnh hưởng, rủi ro, cách hoàn tác) rồi chờ người dùng đồng ý. Không sửa gì ngoài phạm vi đã duyệt.
2. **Kiểm tra kỹ trước khi làm.** Đọc file liên quan và tìm mọi chỗ dùng thứ sắp đổi (cột, ViewBag, Session key, tên action, tên route). Không đoán.
3. **Một lỗi không được sinh ra lỗi khác.** Làm từng bước nhỏ, mỗi bước `dotnet build` sạch. Đổi cấu trúc dữ liệu theo thứ tự mở rộng, chuyển, rồi mới thu hẹp.
4. **Không bao giờ đụng database host.** Chỉ viết script SQL để người dùng tự chạy sau khi sao lưu. Không in chuỗi kết nối, khóa API, mật khẩu; không commit chúng.
5. **Phân biệt sự thật và giả thuyết.** Không bịa số liệu khảo sát hay khẳng định về thị trường Việt Nam khi chưa kiểm chứng. Khảo sát cũ (đồ án cơ sở) chỉ có 18 người, khi dùng phải nêu cỡ mẫu.
6. **Phát hiện lỗi ngoài phạm vi thì báo, không tự sửa.**
7. **Tài liệu đi cùng code.** Một tính năng chỉ xong khi có code, test tay và cập nhật tài liệu liên quan (sơ đồ, ràng buộc, báo cáo).

Skills trong `.claude/skills/` (dùng đúng lúc):

| Skill | Dùng khi |
|---|---|
| `safe-change` | Mọi thay đổi code, view, model, migration, cấu hình |
| `db-3nf-review` | Thêm hoặc sửa bảng, cột, khóa, ràng buộc, migration, phần mô hình dữ liệu |
| `uml-docs` | Vẽ use case, class, sequence, activity, ERD, lược đồ quan hệ, ràng buộc toàn vẹn |
| `ui-redesign` | Thiết kế lại giao diện, sửa CSS, layout, trang |
| `bao-cao-do-an` | Viết các chương báo cáo, khảo sát, bảng so sánh đối thủ |

## Bối cảnh kỹ thuật (đã xác định từ code, xác minh lại khi cần)

- ASP.NET Core MVC, EF Core 8, SQL Server. Project/namespace `webdulich`, DbContext `DuLichDbContext` (kế thừa `IdentityDbContext` nhưng **đăng nhập thật chạy trên bảng `TaiKhoans` với Session**: key `user`, `role`, `taiKhoanId`; mật khẩu băm BCrypt). Các bảng `AspNet...` có vẻ không dùng.
- Vai trò lưu bằng chuỗi: `admin`, `user`, `huongdanvien`, `taixe`.
- Controllers: Admin, DatTour, DiaDiem (trang danh sách và chi tiết tour), DiemTour, GoiYTour, LienHe, NhanVien, QuanTri, TaiKhoan, TrangChu.
- Luồng đặt tour hiện tại: `DatTour` (GET/POST) → `ThanhToan` → `XacNhanThanhToan` → `ThanhCong`; dữ liệu tạm giữ trong Session; thanh toán đang **giả lập**.
- **Hai database riêng:** `WebDuLich` trên máy (SQL Server Express) và bản trên host `travelvn.runasp.net` (asp.monster). Cập nhật cấu trúc phải áp dụng cho cả hai, bản host qua script.
- Hiện web host chạy HTTP, không HTTPS.

## Lỗi và rủi ro đã biết (ưu tiên sửa theo kế hoạch)

Trong code:
1. `DatTour` (GET) đọc `lich.TrangThai` trước khi kiểm tra `lich == null` (lỗi 500 thay vì 404).
2. Mã chuyến đi tạo ở `ThanhToan` (lưu Session) khác mã tạo lại ở `XacNhanThanhToan`, nên mã khách thấy lệch mã được lưu. Mã quá dài, không phù hợp làm nội dung chuyển khoản.
3. Đơn được lưu và trừ `SoChoCon` ngay khi xác nhận; chưa có trạng thái chờ thanh toán, hạn giữ chỗ, ngày đặt.
4. Kiểm tra chỗ rồi mới trừ không nguyên tử: hai người đặt cùng lúc có thể vượt chỗ.
5. `XacNhanThanhToan` không kiểm tra lịch đã hủy hoặc đã qua ngày.
6. `SuaHoSo` cập nhật `TenKhach` các đơn theo tên cũ, có thể đổi nhầm đơn của người trùng tên.
7. `LichSu`, `HuyDatTour`, `LayTaiKhoanIdDangNhap` so khớp theo họ tên: người trùng tên có thể thấy hoặc hủy đơn cũ có `TaiKhoanId` rỗng, kèm số giấy tờ hành khách.
8. Hủy đơn không trả lại lượt dùng khuyến mãi.
9. `Program.cs` thiếu `UseHttpsRedirection` và `UseHsts`; cookie session chưa cấu hình; đăng nhập không giới hạn số lần sai.

Trên bản chạy thật:
10. Nhiều tour không có lịch khởi hành tương lai và lịch trình, nên không đặt được.
11. Chân trang quảng cáo VISA, VNPay, MoMo, ZaloPay khi thanh toán chưa có thật; liên kết "Forgot your password?" không có action phía sau; số "100+ tour, 500+ khách" và ba lời khen là chữ viết cứng; trang đăng nhập lẫn tiếng Anh; mọi trang có tiêu đề "Travel Website"; ảnh điểm đến lấy trực tiếp từ website khác; dữ liệu mẫu giá và số ngày thiếu nhất quán; mọi tour đều có nhãn "Nổi bật".

## Dữ liệu và chuẩn hóa 3NF (yêu cầu của giáo viên)

Chi tiết quy trình ở skill `db-3nf-review`. Điểm chính:

- Bảng `DatTours` còn cột chưa đạt 3NF: `TourId` và `NgayDi` (suy ra từ `LichKhoiHanhTourId`), `MaKhuyenMai` (suy ra từ `KhuyenMaiId`), `TongTien` (bằng `TongTienTruocGiam − SoTienGiam`), và có thể `TenKhach` (cần hỏi người dùng: bỏ hay đổi thành tên người liên hệ nhập độc lập).
- `LichKhoiHanhTour.TrangThai` ("Sắp khởi hành" suy ra từ ngày): chỉ nên lưu sự kiện hủy.
- `TaiKhoans.TaiKhoanDangNhap` trộn email và số điện thoại: tách thành hai cột duy nhất, cho phép rỗng.
- **Giữ** `DiaDiems.GiaTour` (giá tham quan, báo cáo cũ ràng buộc R2). Giá lúc đặt lưu theo từng ghế là snapshot hợp lệ, không vi phạm 3NF.
- Hai đơn cũ Id 17 và 19 chưa gắn lịch khởi hành (tour 13 và 5, đã thanh toán): phải xử lý trước khi bắt buộc `LichKhoiHanhTourId`. Hỏi người dùng trước khi tạo lịch lịch sử hoặc sửa.
- Chống xóa dây chuyền lên dữ liệu đơn đặt: dùng Restrict và cờ `NgungBan` thay vì cho xóa Tour, Lịch có đơn.

## Hoàn thành một hạng mục khi

- [ ] Người dùng đã duyệt kế hoạch thay đổi
- [ ] `dotnet build` sạch, không thêm cảnh báo mới
- [ ] Đã grep và xử lý mọi chỗ dùng thứ đã đổi (kể cả view, ViewBag, Session)
- [ ] Có checklist test tay và người dùng đã thử
- [ ] Migration có script cho host và bước sao lưu (nếu đổi cấu trúc)
- [ ] CSDL mới đạt 3NF, ràng buộc đã ghi lại
- [ ] Sơ đồ và báo cáo liên quan đã cập nhật
- [ ] Commit nhỏ, thông điệp tiếng Việt

## Chưa xác nhận (đừng coi là sự thật)

- Khó khăn của người dùng đặt tour nhóm (cần khảo sát 40 đến 100 người).
- Việc các web tour Việt Nam đã có hay chưa tính năng tương tự (cần bảng so sánh đối thủ).
- Quy định giữ chỗ 24 giờ và các luật nghiệp vụ khác là đề xuất, cần giáo viên và người dùng chốt.
- Bài phụ chưa chốt (gợi ý bảng KPI và cảnh báo cho admin; không làm chatbot AI), tính sau khi bài chính ổn định.
