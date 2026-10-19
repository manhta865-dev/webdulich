---
name: db-3nf-review
description: Thiết kế, kiểm tra và thay đổi cơ sở dữ liệu SQL Server và EF Core của TravelVN theo chuẩn 3NF với đầy đủ ràng buộc toàn vẹn: liệt kê phụ thuộc hàm, phân loại vi phạm, viết migration an toàn, tạo script cho database host. Dùng khi thêm hoặc sửa bảng, cột, khóa, chỉ mục, quan hệ, khi viết phần mô hình dữ liệu và ràng buộc của báo cáo, hoặc khi người dùng nhắc 3NF, chuẩn hóa, migration, ERD, lược đồ quan hệ.
---

# CSDL chuẩn 3NF và ràng buộc toàn vẹn

Yêu cầu của giáo viên: mọi bảng mới và bảng sửa lại phải đạt 3NF. Cấu trúc hiện tại nằm ở `Models/`, `DuLichDbContext.cs` và `Migrations/DuLichDbContextModelSnapshot.cs`.

## Kiểm 3NF cho một bảng

1. Liệt kê khóa chính, khóa ứng viên.
2. Liệt kê **phụ thuộc hàm** X → Y dựa trên nghiệp vụ, không chỉ nhìn tên cột. Hỏi: biết cột này thì suy ra được cột kia không?
3. **1NF:** giá trị nguyên tố, không lặp nhóm. Chú ý cột trộn nhiều nghĩa (ví dụ `TaiKhoanDangNhap` lúc là email lúc là số điện thoại).
4. **2NF:** với khóa ghép, không có cột phụ thuộc một phần khóa.
5. **3NF:** không có cột phụ thuộc bắc cầu qua cột không khóa khác.

## Phân loại phát hiện (đừng gộp chung)

| Loại | Ví dụ | Xử lý |
|---|---|---|
| **Vi phạm 3NF thật** (cột suy ra được từ cột không khóa trong cùng bảng) | `DatTours.TourId` suy ra qua `LichKhoiHanhTourId`; `MaKhuyenMai` qua `KhuyenMaiId` | Bỏ cột, lấy bằng join |
| **Dữ liệu dẫn xuất** (tính được từ bảng khác) | `SoNguoi`, `SoChoCon`, `SoLuotDaDung` | Bỏ và tính bằng truy vấn hoặc view; hoặc giữ như bộ nhớ đệm có chủ đích và ghi chú rõ trong báo cáo |
| **Snapshot hợp lệ** (sự thật lịch sử, không suy ra từ dữ liệu hiện tại) | Giá lúc đặt theo từng ghế | Giữ, giải thích trong báo cáo vì sao không vi phạm |

Chỉ loại đầu tiên là vi phạm 3NF theo định nghĩa chặt. Hai loại sau vẫn nên xử lý có chủ đích vì giáo viên có thể hỏi.

## Ràng buộc cần có

- **Khóa và duy nhất:** PK, FK, UNIQUE (ví dụ mã đơn, email, số điện thoại, mã mời).
- **Miền giá trị:** CHECK (số lượng >= 0, giá > 0, trạng thái thuộc danh sách cho phép). Trạng thái dùng hằng số hoặc enum trong code cộng CHECK, hoặc bảng danh mục.
- **Độ dài và kiểu:** đặt độ dài chuỗi hợp lý, tránh `nvarchar(max)` cho cột cần tìm hoặc đánh chỉ mục. Tiền dùng `decimal(18,2)`.
- **ON DELETE:** Restrict cho dữ liệu tài chính và lịch sử (đơn, giao dịch). Cascade chỉ cho dữ liệu con thuần túy (ảnh phụ, lịch trình của một tour). Dùng cờ ngừng bán thay vì xóa tour.
- **Đồng thời:** cột `rowversion` hoặc cập nhật có điều kiện cho số chỗ và trạng thái ghế, để hai người không cùng nhận một chỗ.

## Quy trình migration (EF Core 8)

1. Sao lưu database. Làm trên database local trước.
2. Sửa model hoặc DbContext. Chạy `dotnet ef migrations add TenMigration` cho project web.
3. **Đọc file migration** (`Up` và `Down`). EF hay hiểu đổi tên cột là xóa rồi thêm mới, làm mất dữ liệu. Kiểm tra mọi lệnh `DropColumn`, `DropTable`, `DropForeignKey`.
4. Cột bắt buộc mới trên bảng đã có dữ liệu: thêm cho phép rỗng, điền dữ liệu, rồi siết NOT NULL ở migration sau. Với `DateTime` bắt buộc, dữ liệu cũ sẽ nhận `0001-01-01` nếu không cho phép rỗng.
5. Trước khi thêm UNIQUE: chạy truy vấn tìm bản ghi trùng.
6. `dotnet ef database update` trên local, chạy thử web.
7. Tạo script cho host: `dotnet ef migrations script <từ> <đến> --idempotent -o docs/sql/<ten>.sql`. Đưa cho người dùng đọc và **tự chạy** trên host sau khi sao lưu. Không bao giờ tự chạy lên host.

## Việc đã biết của TravelVN (xác minh lại trước khi làm)

- `DatTours`: bỏ `TourId`, `NgayDi`, `MaKhuyenMai`, `TongTien`; bắt buộc `LichKhoiHanhTourId`; thêm `NgayDat`, `NgayCapNhat`, `HanThanhToan`; `MaChuyenDi` đặt độ dài khoảng 20 và UNIQUE. `TenKhach`: hỏi người dùng (bỏ, hay đổi thành tên người liên hệ).
- Thêm vào `HanhKhachDatTours` các cột cho ghế: trạng thái ghế, giá lúc đặt, mã thanh toán riêng (UNIQUE), tài khoản nhận ghế.
- `TaiKhoans`: tách `Email` và `SoDienThoai` (mỗi cột UNIQUE, cho phép rỗng) thay cho `TaiKhoanDangNhap`; chuyển dữ liệu cũ rồi mới bỏ cột.
- `LichKhoiHanhTours`: thêm `SoChoToiDa`, `RowVersion`, cờ hủy thay cho trạng thái chữ.
- `Tours`: thêm cờ `NgungBan`. Xóa Tour hoặc Lịch có đơn phải bị chặn.
- Bảng mới `GiaoDichThanhToan`: gắn với một ghế, số tiền, mã tham chiếu ngân hàng (UNIQUE để không xử lý trùng), thời điểm, trạng thái.
- Hai đơn cũ Id 17 và 19 chưa gắn lịch khởi hành: xử lý trước khi bắt buộc cột này.
- Giữ `DiaDiems.GiaTour` (giá tham quan).
- Hỏi giáo viên mức chặt của `HanhKhachDatTours` (có cần tách bảng hành khách riêng không).

## Đầu ra cho báo cáo

1. Bảng phụ thuộc hàm và kết luận 1NF, 2NF, 3NF cho từng bảng.
2. **Lược đồ quan hệ** dạng `TenBang(PK, cot, cot, FK → BangKhac)`.
3. Ràng buộc toàn vẹn theo mẫu R1, R2... (xem skill `uml-docs`).
