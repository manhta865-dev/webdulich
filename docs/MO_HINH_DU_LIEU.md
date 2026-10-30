# Mô hình dữ liệu TravelVN (bản đã chốt, chờ áp dụng)

Trạng thái: **bản thiết kế, chưa áp dụng lên CSDL nào**. Nguồn: class diagram `TravelVN.mdj` (StarUML) cộng quyết định của người dùng ngày 27/10/2026. Cấu trúc hiện tại nằm ở `webdulich/Models/` và `Migrations/`.

Mọi thay đổi đi theo quy trình mở rộng, chuyển dữ liệu, thu hẹp (xem [mục 7](#7-kế-hoạch-áp-dụng)). CSDL host chỉ nhận script do người dùng tự chạy sau khi sao lưu.

## 1. Quy ước tên

| Từ | Nghĩa | Bảng |
|---|---|---|
| **Ghế** | Suất đi của một người trong một đơn | `HanhKhachDatTour` |
| **Chỗ ngồi** | Vị trí vật lý trên xe hoặc máy bay (ví dụ 12A) | `ViTriChoNgoi` |
| **Đơn** | Một lần đặt, cá nhân hoặc nhóm | `DatTours` |

Ghế và chỗ ngồi là hai thứ khác nhau: ghế gắn với người và tiền, chỗ ngồi gắn với vị trí trên sơ đồ. `ChoNgoiChuyenDi` nối hai thứ này cho từng chuyến.

## 2. Các bảng sau thay đổi

Ký hiệu: PK khóa chính, FK khóa ngoại, UQ duy nhất (với cột cho phép rỗng thì dùng chỉ mục có lọc `IS NOT NULL`, vì SQL Server chỉ cho một giá trị NULL trong chỉ mục duy nhất thường), `?` cho phép rỗng. Tiền dùng `decimal(18,2)`.

### 2.1 Đặt tour

**DatTours** (đơn)

| Cột | Kiểu | Ghi chú |
|---|---|---|
| Id | int PK | |
| TaiKhoanId | int? FK → TaiKhoans | Người tạo đơn; rỗng với đơn cũ chưa gắn tài khoản |
| LichKhoiHanhTourId | int FK → LichKhoiHanhTours | **Bắt buộc**, Restrict |
| KhuyenMaiId | int? FK → KhuyenMais | Rỗng nếu không dùng mã |
| MaChuyenDi | string UQ | Mã đơn, tạo một lần ở server, ngắn, kiểm tra trùng |
| LoaiDon | string | `CaNhan` hoặc `Nhom` |
| TrangThai | string | Xem mục 4 |
| MaMoi | string? UQ | Mã link mời của đơn nhóm |
| HanGiuCho | datetime? | Hạn giữ chỗ của đơn nhóm (24 giờ, tham số cấu hình) |
| DaGiaHan | bit | Đơn nhóm đã được gia hạn một lần khi hết hạn (nhắc rồi mới nhả); thêm ở Mo9 |
| NgayDat | datetime | |
| TenKhach, DienThoai, Email, DiaChi, GhiChu? | string | Thông tin liên hệ do người đặt nhập độc lập, không suy ra từ `TaiKhoans` |
| SoTienGiam | decimal | Snapshot số tiền giảm lúc đặt |

Bỏ: `TourId`, `NgayDi`, `MaKhuyenMai`, `TongTien`, `TongTienTruocGiam`, `SoNguoi`, `PhuongThucThanhToan`, `TrangThaiThanhToan`.

**HanhKhachDatTour** (ghế)

| Cột | Kiểu | Ghi chú |
|---|---|---|
| Id | int PK | |
| DatTourId | int FK → DatTours | Cascade |
| TaiKhoanId | int? FK → TaiKhoans | NoAction (tránh nhiều đường cascade). UQ(DatTourId, TaiKhoanId) khi có giá trị |
| HoTen?, LoaiGiayTo?, SoGiayTo? | string | Rỗng cho đến khi thành viên tự điền |
| LoaiHanhKhach | string | `Người lớn` hoặc `Trẻ nhỏ` |
| GiaLucDat | decimal | Snapshot giá lúc đặt |
| MaThanhToan | string? UQ | Nội dung chuyển khoản của riêng ghế |
| NgayNha | datetime? | Có giá trị nghĩa là ghế đã nhả |
| TrangThaiGhe | `[NotMapped]` | **Không lưu cột**, tính từ `NgayNha`, giao dịch và thông tin đã điền |

**GiaoDichThanhToan** (mới)

| Cột | Kiểu | Ghi chú |
|---|---|---|
| Id | int PK | |
| DatTourId | int? FK → DatTours | Restrict |
| HanhKhachDatTourId | int? FK → HanhKhachDatTours | Restrict |
| NguoiXacNhanId | int? FK → TaiKhoans | Restrict |
| NguoiTraId | int? FK → TaiKhoans | Restrict. Ai đã trả: chủ ghế, hoặc trưởng nhóm trả hộ (thêm ở Mo9) |
| SoTien | decimal | |
| PhuongThuc | string | |
| TrangThai | string | `ChoXacNhan`, `ThanhCong`, `ThatBai` |
| MaThamChieuNganHang | string? UQ | Chống xử lý trùng một giao dịch ngân hàng |
| NgayTao | datetime | |
| NgayXacNhan | datetime? | |

CHECK đúng một trong `DatTourId`, `HanhKhachDatTourId` có giá trị. Chỉ mục duy nhất có lọc `TrangThai = 'ThanhCong'` cho `DatTourId` và cho `HanhKhachDatTourId`: mỗi đơn hoặc ghế tối đa một giao dịch thành công.

### 2.2 Chọn chỗ ngồi (3 bảng mới)

**SoDoChoNgoi**: `Id` PK, `TenSoDo`, `PhuongTienId` int? FK → PhuongTiens, `SoHang`, `SoCot`.

**ViTriChoNgoi**: `Id` PK, `SoDoChoNgoiId` FK → SoDoChoNgoi (Cascade), `MaViTri` (ví dụ `12A`), `Hang`, `Cot`. UQ(`SoDoChoNgoiId`, `MaViTri`) và UQ(`SoDoChoNgoiId`, `Hang`, `Cot`).

**ChoNgoiChuyenDi**: `LichKhoiHanhTourId` FK (Restrict), `ViTriChoNgoiId` FK (Restrict), `HanhKhachDatTourId` FK **UQ** (Cascade). PK(`LichKhoiHanhTourId`, `ViTriChoNgoiId`).

- PK ghép: CSDL tự chặn hai người chọn cùng một chỗ trên cùng chuyến.
- UQ trên `HanhKhachDatTourId`: mỗi ghế tối đa một chỗ ngồi.
- `LichKhoiHanhTourId` ở đây suy ra được qua ghế và đơn. Đây là **dư thừa có chủ đích** để có khóa ghép chống trùng; tính nhất quán được kiểm tra trong code (RB-18).

### 2.3 Các bảng khác

| Bảng | Thay đổi |
|---|---|
| **LichKhoiHanhTour** | Thêm `SoChoToiDa` int, `SoDoChoNgoiId` int? FK → SoDoChoNgoi (Restrict). `TrangThai` chỉ còn `MoBan`, `TamNgung`, `DaHuy`. FK tới Tour đổi Cascade thành Restrict. **Giữ `SoChoCon`** (xem RB-12) |
| **Tour** | Thêm `NgungBan` bool |
| **TaiKhoan** | Tách `TaiKhoanDangNhap` thành `Email` string? UQ và `SoDienThoai` string? UQ, CHECK có ít nhất một |
| **NhanVien** | Bỏ `HoTen`, `LoaiNhanVien`, `SoDienThoai`. Họ tên và số điện thoại lấy từ `TaiKhoan`, loại nhân viên lấy từ `TaiKhoan.VaiTro` |
| **KhuyenMai** | Bỏ `SoLuotDaDung` (đếm từ `DatTours` chưa hủy). `TrangThai` chỉ còn `HoatDong`, `TamNgung` |
| **TinNhanLienHe** | Bỏ `HoTen`, `Email`. `TaiKhoanId` bắt buộc, FK Cascade |
| **DiaDiem** | Giữ `GiaTour` (giá tham khảo do admin nhập, không vi phạm 3NF) |
| **AspNet\* (7 bảng)** | Dư, xóa ở migration riêng cuối cùng (mục 7, bước 8) |

## 3. Quan hệ và kiểu xóa

| Từ | Đến | Khi xóa |
|---|---|---|
| DatTours.LichKhoiHanhTourId | LichKhoiHanhTours | Restrict |
| DatTours.KhuyenMaiId | KhuyenMais | Restrict |
| DatTours.TaiKhoanId | TaiKhoans | cần chốt (câu hỏi 5) |
| HanhKhachDatTour.DatTourId | DatTours | Cascade |
| HanhKhachDatTour.TaiKhoanId | TaiKhoans | NoAction |
| GiaoDichThanhToan.* | DatTours, HanhKhachDatTour, TaiKhoans | Restrict |
| ChoNgoiChuyenDi.LichKhoiHanhTourId, ViTriChoNgoiId | LichKhoiHanhTours, ViTriChoNgoi | Restrict |
| ChoNgoiChuyenDi.HanhKhachDatTourId | HanhKhachDatTour | Cascade |
| ViTriChoNgoi.SoDoChoNgoiId | SoDoChoNgoi | Cascade |
| LichKhoiHanhTour.TourId | Tours | Restrict (dùng `NgungBan` thay cho xóa) |
| LichKhoiHanhTour.SoDoChoNgoiId | SoDoChoNgoi | Restrict |
| TinNhanLienHe.TaiKhoanId | TaiKhoans | Cascade |

## 4. Giá trị trạng thái

| Cột | Giá trị | Ghi chú |
|---|---|---|
| LichKhoiHanhTour.TrangThai | `MoBan`, `TamNgung`, `DaHuy` | "Sắp khởi hành", "Đang diễn ra", "Đã hoàn thành" vẫn **tính từ ngày** ở `TrangThaiHienThi`, không lưu |
| KhuyenMai.TrangThai | `HoatDong`, `TamNgung` | Còn hạn, còn lượt tính riêng |
| GiaoDichThanhToan.TrangThai | `ChoXacNhan`, `ThanhCong`, `ThatBai` | |
| DatTours.LoaiDon | `CaNhan`, `Nhom` | |
| DatTours.TrangThai | đề xuất `GiuCho`, `DaXacNhan`, `DaHuy` | **Chờ chốt** (câu hỏi 4) |
| TrangThaiGhe (tính) | đề xuất `ChuaNhan`, `DaNhan`, `ChoXacNhan`, `DaTra`, `DaNha` | **Chờ chốt** (câu hỏi 4) |

## 5. Dữ liệu dẫn xuất (không lưu cột)

| Giá trị | Công thức |
|---|---|
| Số người của đơn | số ghế chưa nhả (`NgayNha IS NULL`) |
| Tổng trước giảm | tổng `GiaLucDat` của ghế chưa nhả |
| Số phải trả | tổng trước giảm − `SoTienGiam` |
| Số lượt đã dùng của khuyến mãi | số đơn có `KhuyenMaiId` và `TrangThai <> 'DaHuy'` (hủy đơn tự trả lượt, sửa lỗi 8 trong CLAUDE.md) |
| Trạng thái ghế | từ `NgayNha`, `GiaoDichThanhToan`, thông tin đã điền |
| Tour của đơn | `DatTours → LichKhoiHanhTour → Tour` |
| Ngày đi của đơn | `DatTours → LichKhoiHanhTour.NgayKhoiHanh` |

**Snapshot hợp lệ, giữ lại:** `GiaLucDat` (giá lúc đặt, giá hiện tại có thể đổi), `SoTienGiam` (khuyến mãi có thể bị sửa sau đó).

**Giữ có chủ đích:** `LichKhoiHanhTour.SoChoCon` để trừ chỗ nguyên tử bằng `UPDATE ... SET SoChoCon = SoChoCon - @n WHERE Id = @id AND SoChoCon >= @n`. Hệ quả: tạo ghế, nhả ghế và hủy đơn phải cập nhật `SoChoCon` **trong cùng giao dịch** với việc thêm hoặc sửa ghế.

## 6. Ràng buộc toàn vẹn

Mã `RB-xx` để khỏi lẫn với R1, R2 của báo cáo đồ án cơ sở (ví dụ R2 ràng buộc `DiaDiem.GiaTour`); sẽ đối chiếu số thứ tự khi viết báo cáo.

**Trong CSDL (khóa, CHECK, chỉ mục):**

| Mã | Ràng buộc |
|---|---|
| RB-01 | `DatTours.LichKhoiHanhTourId` NOT NULL, FK Restrict |
| RB-02 | `DatTours.MaChuyenDi`, `MaMoi`, `HanhKhachDatTour.MaThanhToan`, `GiaoDichThanhToan.MaThamChieuNganHang`: duy nhất (chỉ mục có lọc khi cho phép rỗng) |
| RB-03 | `HanhKhachDatTour`: UQ(`DatTourId`, `TaiKhoanId`) khi `TaiKhoanId` có giá trị |
| RB-04 | `GiaoDichThanhToan`: đúng một trong `DatTourId`, `HanhKhachDatTourId` có giá trị |
| RB-05 | Mỗi đơn hoặc ghế tối đa một giao dịch `ThanhCong` |
| RB-06 | `TaiKhoan`: `Email` và `SoDienThoai` duy nhất, CHECK có ít nhất một |
| RB-07 | `TaiKhoan.Email` đúng dạng email; `SoDienThoai` đúng 10 số bắt đầu bằng 0 (kiểm trong code, tùy chọn CHECK) |
| RB-08 | `ChoNgoiChuyenDi`: PK(`LichKhoiHanhTourId`, `ViTriChoNgoiId`) và UQ(`HanhKhachDatTourId`) |
| RB-09 | `ViTriChoNgoi`: UQ(`SoDoChoNgoiId`, `MaViTri`) và UQ(`SoDoChoNgoiId`, `Hang`, `Cot`) |
| RB-10 | `LichKhoiHanhTour`: CHECK `0 <= SoChoCon <= SoChoToiDa`; `TrangThai` thuộc `MoBan`, `TamNgung`, `DaHuy` |
| RB-11 | CHECK miền giá trị cho `LoaiDon`, `DatTours.TrangThai`, `GiaoDichThanhToan.TrangThai`, `KhuyenMai.TrangThai`; `GiaLucDat >= 0`, `SoTien > 0` |
| RB-12 | `SoChoCon` luôn bằng `SoChoToiDa` trừ số ghế chưa nhả của đơn chưa hủy trên lịch đó (truy vấn đối chiếu ở `docs/sql/doi_chieu.sql`, cập nhật cùng giao dịch) |
| RB-13 | Xóa Tour hoặc Lịch có đơn bị chặn (Restrict); dùng `Tour.NgungBan` hoặc `LichKhoiHanhTour.TrangThai = DaHuy` |

**Kiểm tra trong code (CSDL không biểu diễn được):**

| Mã | Ràng buộc |
|---|---|
| RB-14 | `SoChoToiDa` <= số vị trí của sơ đồ gắn với lịch (vì hướng dẫn viên và tài xế cũng ngồi) |
| RB-15 | `PhanCongTour`: người được chọn làm hướng dẫn viên phải có `TaiKhoan.VaiTro = huongdanvien`, tài xế phải là `taixe` |
| RB-16 | Nhả ghế (đặt `NgayNha`) thì xóa dòng `ChoNgoiChuyenDi` của ghế đó và cộng lại `SoChoCon` |
| RB-17 | Hủy đơn: nhả mọi ghế chưa nhả, trả chỗ, không còn tính lượt khuyến mãi |
| RB-18 | `ChoNgoiChuyenDi.LichKhoiHanhTourId` phải trùng lịch của đơn chứa ghế; `ViTriChoNgoi` phải thuộc `SoDoChoNgoi` của lịch đó |
| RB-19 | Không đặt cho lịch `DaHuy`, `TamNgung`, đã qua ngày khởi hành (sửa lỗi 5) |
| RB-20 | Trưởng nhóm xem tiến độ nhưng không xem `SoGiayTo` của ghế người khác |

## 7. Kế hoạch áp dụng

1. **Sao lưu** CSDL local `WebDuLich` (tệp `.bak` ngoài thư mục dự án, không commit).
2. **Mở rộng:** migration thêm cột cho phép rỗng và 4 bảng mới (`GiaoDichThanhToan`, `SoDoChoNgoi`, `ViTriChoNgoi`, `ChoNgoiChuyenDi`). Code cũ vẫn chạy.
3. **Chuyển dữ liệu:** điền `SoChoToiDa`, chuẩn hóa trạng thái lịch và khuyến mãi, tách email và số điện thoại, chuyển số điện thoại nhân viên. **Local:** xóa 7 đơn cũ (dữ liệu thử, người dùng đã chọn ngày 27/10/2026) cùng hành khách của chúng, trả lượt khuyến mãi, sau khi sao lưu. **Host:** không xóa; script có truy vấn kiểm tra trước (đơn chưa có lịch, đơn chưa có ghế) và dừng nếu còn, người dùng tự quyết định rồi mới chạy bước thu hẹp.
4. **Đổi code đọc ghi** sang cấu trúc mới, từng nhóm nhỏ, build sau mỗi nhóm: tài khoản và đăng nhập; khuyến mãi, nhân viên, tin nhắn; đọc đơn (lịch sử, admin, doanh thu); luồng đặt tour (mã chuyến đi, giữ chỗ, giao dịch, trừ chỗ nguyên tử).
5. **Thu hẹp:** migration bỏ cột cũ, siết NOT NULL, thêm chỉ mục duy nhất, CHECK, đổi kiểu xóa.
6. **Chọn chỗ ngồi:** dữ liệu mẫu sơ đồ xe và máy bay, trang admin gắn sơ đồ vào lịch, giao diện chọn chỗ.
7. **Script cho host** trong `docs/sql/`, đưa người dùng đọc và tự chạy sau khi sao lưu host.
8. **Xóa 7 bảng AspNet\*** ở migration riêng, kèm bỏ `AddIdentity`, `ApplicationUser`, và đổi lớp cha của `DuLichDbContext`.
9. **Tài liệu:** lược đồ quan hệ, bảng phụ thuộc hàm, ràng buộc, cập nhật sơ đồ.

## 8. Dữ liệu local hiện có (đo ngày 27/10/2026, chỉ đọc)

- 7 đơn, cùng trạng thái `Đã thanh toán` (5 qua ACB, 2 qua MoMo). Không có mã chuyến đi trùng, độ dài 25 đến 26 ký tự.
- 2 đơn chưa có lịch: Id 17 (tour 13, ngày đi 16/05/2026) và Id 19 (tour 5, ngày đi 22/05/2026).
- 4 đơn (Id 17, 19, 20, 22) **chưa có dòng hành khách** dù khai 2 đến 3 người; 3 đơn còn lại có đủ.
- 5 đơn có lịch đều khớp tour và ngày đi với lịch, nên bỏ `TourId` và `NgayDi` không mất dữ liệu.
- `LichKhoiHanhTours`: 3 lịch có `TrangThai` rỗng (sẽ thành `MoBan`), 2 lịch `Sắp khởi hành`, 3 lịch `Đã hủy`. Đơn Id 26 đã thanh toán nhưng nằm trên lịch Id 9 đã hủy.
- 6 tài khoản đều dùng email, chưa có số điện thoại; không trùng. 3 nhân viên: họ tên và loại khớp với tài khoản, số điện thoại không trùng nhau.
- `TinNhanLienHes`: 15 dòng, không dòng nào thiếu `TaiKhoanId`. `AspNetUsers`: 0 dòng.
- `SoLuotDaDung` của khuyến mãi khớp số đơn thực tế.
- `Tours`: 10 tour dùng `Xe du lịch`, 2 tour dùng `Máy bay`.

## 9. Quyết định cho đơn nhóm (chốt ngày 30/10/2026, cần báo giáo viên)

| Nội dung | Quyết định |
|---|---|
| Hạn giữ chỗ | 24 giờ, tham số trong `appsettings.json` |
| Nhắc trước khi nhả | Hết hạn thì gia hạn thêm 6 giờ một lần (`DaGiaHan`) và nhắc trong web; hết 6 giờ mới nhả ghế chưa trả. Chưa gửi email, SMS |
| Ghế đang chờ admin xác nhận khi hết hạn | Không nhả, giữ đến khi admin xác nhận hoặc từ chối |
| Luật chốt (F9) | Hết hạn: nhả ghế chưa trả, chốt đơn với ghế đã trả; không ai trả thì hủy đơn. Trưởng nhóm được chốt sớm |
| Trả hộ | Trưởng nhóm trả thay cho ghế bất kỳ trong đơn (`GiaoDichThanhToan.NguoiTraId`); mặc định ai trả phần người đó |
| Chọn chỗ cho nhóm | Trưởng nhóm chọn sẵn chỗ cho các ghế lúc tạo đơn; thành viên đổi được khi nhận ghế |
| Ghế của trưởng nhóm | Không bắt buộc: người đặt hộ có thể không đi |
| Nhận ghế qua link | Xem thông tin chuyến không cần đăng nhập; nhận ghế phải đăng nhập (mỗi tài khoản tối đa một ghế trong một đơn, RB-03). Người không có tài khoản (trẻ nhỏ) do trưởng nhóm điền và trả hộ |
| QR | Ảnh VietQR theo mã thanh toán của ghế, tài khoản ngân hàng là thông tin minh họa trong cấu hình |

Ràng buộc thêm (kiểm trong code):

| Mã | Ràng buộc |
|---|---|
| RB-21 | Chỉ trưởng nhóm (người tạo đơn) được thu hồi link, trả hộ, chốt sớm, hủy đơn nhóm |
| RB-22 | Ghế đã có giao dịch thành công thì không bị nhả khi hết hạn |
| RB-23 | Mã mời (`MaMoi`) đủ dài và ngẫu nhiên; thu hồi thì đổi mã, link cũ hết hiệu lực |
