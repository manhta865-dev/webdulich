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
| **LichKhoiHanhTour** | `SoDoChoNgoiId` int **NOT NULL** FK → SoDoChoNgoi (Restrict): xe do admin chọn (Mo15). `TrangThai` chỉ còn `MoBan`, `TamNgung`, `DaHuy`. FK tới Tour đổi Cascade thành Restrict. **Giữ `SoChoCon`** (xem RB-12). Cột `SoChoToiDa` thêm ở Mo1, **bỏ ở Mo15** vì suy ra được từ xe và `ChoKhongMoBans` |
| **ChoKhongMoBans** (mới, Mo14) | `ChoKhongMoBans(LichKhoiHanhTourId, ViTriChoNgoiId)`, PK ghép cả hai cột; FK lịch Cascade, FK vị trí Restrict. Mỗi dòng: chuyến này không bán chỗ này (ghế hướng dẫn viên, ghế hỏng, đi đoàn nhỏ). Bảng chỉ gồm khóa nên đạt BCNF (RB-37) |
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
| ChoKhongMoBan.LichKhoiHanhTourId | LichKhoiHanhTours | Cascade (dữ liệu con thuần túy của lịch) |
| ChoKhongMoBan.ViTriChoNgoiId | ViTriChoNgoi | Restrict |
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
| Số chỗ mở bán của lịch | số `ViTriChoNgoi` của xe (`SoDoChoNgoiId`) − số dòng `ChoKhongMoBans` của lịch (`SucChuaLich.SoChoMoBan`) |

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
| RB-10 | `LichKhoiHanhTour`: CHECK `SoChoCon >= 0` (cận trên là số chỗ mở bán, liên quan bảng khác nên kiểm ở RB-12); `TrangThai` thuộc `MoBan`, `TamNgung`, `DaHuy` |
| RB-11 | CHECK miền giá trị cho `LoaiDon`, `DatTours.TrangThai`, `GiaoDichThanhToan.TrangThai`, `KhuyenMai.TrangThai`; `GiaLucDat >= 0`, `SoTien > 0` |
| RB-12 | `SoChoCon` luôn bằng số chỗ mở bán (mục 5) trừ số ghế chưa nhả của đơn chưa hủy trên lịch đó (truy vấn đối chiếu ở `docs/sql/doi_chieu.sql`, cập nhật cùng giao dịch; đổi xe hay chỗ mở bán thì tính lại bằng một câu UPDATE, `SucChuaLich.TinhLaiSoChoCon`) |
| RB-13 | Xóa Tour hoặc Lịch có đơn bị chặn (Restrict); dùng `Tour.NgungBan` hoặc `LichKhoiHanhTour.TrangThai = DaHuy` |

**Kiểm tra trong code (CSDL không biểu diễn được):**

| Mã | Ràng buộc |
|---|---|
| RB-14 | Mọi lịch có xe (`SoDoChoNgoiId` NOT NULL, Mo15). Xe do admin chọn và phải thuộc đúng phương tiện của tour (ví dụ tour đi xe du lịch chỉ chọn Xe 16, 29, 45 chỗ). Đổi xe chỉ khi chưa có khách chọn chỗ trên xe hiện tại, và xe mới phải đủ chỗ cho số khách đã giữ |
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
| Hạn giữ chỗ | 24 giờ (tham số trong `appsettings.json`), nhưng muộn nhất 23:59:59 hôm trước ngày đi (`XuLyDon.TinhHanGiuCho`, chốt 31/10/2026). Ngày đi là hôm nay thì không tạo đơn nhóm được. Gia hạn ở N5 cũng không vượt mốc này |
| Khi nào đơn còn nhận thay đổi | Đơn `GiuCho`, lịch chưa hủy, chưa tới ngày đi, chưa quá hạn giữ chỗ. Một hàm duy nhất (`LyDoDonKhongNhan`) dùng cho cả máy chủ và giao diện; không còn nhận thì trang ẩn nút và nói đúng lý do |
| Nhắc trước khi nhả | Hết hạn thì gia hạn thêm 6 giờ một lần (`DaGiaHan`) và nhắc trong web; hết 6 giờ mới nhả ghế chưa trả. Chưa gửi email, SMS |
| Ghế đang chờ admin xác nhận khi hết hạn | Không nhả, giữ đến khi admin xác nhận hoặc từ chối |
| Luật chốt (F9) | Hết hạn: nhả ghế chưa trả, chốt đơn với ghế đã trả; không ai trả thì hủy đơn. Trưởng nhóm được chốt sớm |
| Trả hộ | Trưởng nhóm trả thay cho ghế bất kỳ trong đơn (`GiaoDichThanhToan.NguoiTraId`); mặc định ai trả phần người đó |
| Chọn chỗ cho nhóm | Trưởng nhóm chọn sẵn chỗ cho các ghế lúc tạo đơn; thành viên đổi được khi nhận ghế |
| Ghế của trưởng nhóm | Không bắt buộc: người đặt hộ có thể không đi |
| Nhận ghế qua link | Xem thông tin chuyến không cần đăng nhập; nhận ghế phải đăng nhập (mỗi tài khoản tối đa một ghế trong một đơn, RB-03). Người không có tài khoản (trẻ nhỏ) do trưởng nhóm điền và trả hộ |
| QR | Ngân hàng: ảnh VietQR tạo theo số tiền và mã thanh toán của ghế, tài khoản nhận (ACB) đặt trong mục `ThanhToan` của `appsettings.json`. Ví MoMo: ảnh QR cố định, người trả tự gõ số tiền và nội dung. Giao dịch ghi phương thức `Chuyển khoản` hoặc `MoMo` |
| Trả tiền một ghế (N4) | QR hiện khi ghế đã điền đủ thông tin và đơn còn giữ chỗ. Người trả bấm "Tôi đã chuyển khoản": tạo giao dịch `ChoXacNhan` gắn với ghế (`HanhKhachDatTourId`, `DatTourId` rỗng), ghi `NguoiTraId`. Sau đó thông tin ghế khóa, không trả lại ghế được |
| Trả hộ ghế thành viên khác | Trưởng nhóm mở trang trả hộ: chỉ thấy tên, giá, QR của ghế, không thấy giấy tờ. Chỉ trả hộ được ghế đã điền xong |
| Admin xác nhận đơn nhóm | Theo từng giao dịch của ghế: "Đã nhận" thành `ThanhCong`, "Từ chối" thành `ThatBai` (ghế về "Đã điền, chưa trả", trả lại được, người trả và trưởng nhóm thấy thông báo). Đơn đã quá hạn thì từ chối xong áp dụng ngay luật hết hạn: còn lượt thì gia hạn, hết lượt thì nhả ghế. Không có nút xác nhận cả đơn. Ghế đã nhả chỉ được từ chối |
| Giờ nghiệp vụ và hiển thị | Luôn là giờ Việt Nam (UTC+7) qua `GioVN`, không dùng giờ máy chủ vì host chạy múi giờ khác |
| Quả địa cầu ở trang chủ | Luôn vẽ và ghi tên Hoàng Sa, Trường Sa cùng màu với Việt Nam. Đường bao là minh họa gần đúng, không phải bản đồ chính xác. Thư viện `three` chỉ tải trên màn hình rộng, sau khi trang đã hiện |
| Hành trình ở trang tour | Tour có điểm xuất phát và điểm tới cuối ngày của mọi ngày: xe chạy trên bản đồ thật, ngày sau đi tiếp từ điểm cuối ngày trước. Dữ liệu từ OpenStreetMap, đã bỏ chữ và ranh giới của bản đồ gốc, khóa vùng xem quanh tour, người xem không tự xoay hay nghiêng. Chưa đủ tọa độ, hoặc dịch vụ bản đồ lỗi: tour từ 2 ngày hiện phong cảnh tự vẽ, tour 1 ngày chỉ có danh sách. Đường đi là tham khảo, không phải lộ trình cam kết |
| Trạng thái thanh toán của đơn nhóm | Tính, không lưu: mọi ghế còn hiệu lực đã có giao dịch thành công thì "Đã thanh toán"; có ghế chờ xác nhận thì "Đang kiểm tra"; còn lại "Chưa thanh toán" |
| Trang link mời | Không hiện tên người đã nhận ghế, chỉ ghi "Đã có người nhận" (ai có link cũng xem được) |
| Ghế trẻ nhỏ | Không nhận qua link; trưởng nhóm điền hộ. Trưởng nhóm cũng điền hộ được ghế người lớn chưa ai nhận (người không có tài khoản) |
| Trả lại ghế | Thành viên tự trả lại ghế khi chưa trả tiền: ghế về trống, xóa thông tin, giữ chỗ ngồi đã chọn |
| Sửa thông tin ghế | Được sửa khi đơn còn giữ chỗ và ghế chưa trả, chưa chờ xác nhận; sau đó liên hệ TravelVN |
| Đổi link mời (N2) | Trưởng nhóm tạo mã mời mới khi đơn còn giữ chỗ; link cũ hết hiệu lực, người đã nhận ghế giữ nguyên ghế |
| Mời ra khỏi ghế (N2) | Trưởng nhóm làm ghế thành viên khác về trống (xóa tài khoản, giấy tờ, giữ chỗ ngồi) khi ghế chưa báo chuyển khoản, chưa trả |
| Bớt ghế (N2) | Nhả một ghế chưa báo chuyển khoản, chưa trả; bỏ chỗ ngồi; trả 1 chỗ cho lịch; đơn còn ít nhất 1 ghế |
| Chốt sớm (N2) | Chỉ khi không còn ghế chờ admin xác nhận và có ít nhất 1 ghế đã trả. Nhả ghế chưa trả, trả chỗ cho lịch, đơn thành `DaXacNhan`, link mời hết hiệu lực. Hàm dùng chung `XuLyDon.ChotDonNhom` (N5 gọi lại) |
| Hủy đơn nhóm (N2) | Trưởng nhóm chỉ hủy được khi chưa ghế nào trả hoặc báo chuyển khoản; đã có tiền thì liên hệ TravelVN để admin hủy và hoàn tiền ngoài hệ thống. Không làm chức năng thêm ghế |
| Hết hạn giữ chỗ (N5) | Dịch vụ chạy nền `XuLyHetHanDonNhom` quét 5 phút một lần (`DonNhom:ChuKyQuetPhut`); các trang đơn nhóm cũng tự xử lý khi mở (host dùng chung có thể cho ứng dụng ngủ). Cả hai gọi `XuLyDon.XuLyDonQuaHan` |
| Luật khi hết hạn (N5) | Mọi ghế đã trả: chốt. Còn ghế chưa trả, chưa gia hạn: gia hạn 6 giờ tính từ lúc xử lý (`DaGiaHan = 1`), không vượt 23:59:59 hôm trước ngày đi; chạm mốc thì coi như hết lượt. Hết lượt: nhả ghế chưa trả, trả chỗ cho lịch; còn ghế chờ admin thì đơn giữ `GiuCho` đợi admin xử lý xong mới chốt hoặc hủy (chốt 31/10/2026); không còn ghế chờ thì chốt nếu có ghế đã trả, hủy nếu không ai trả |
| Nhắc trong web (N5) | Khi gia hạn: trang tiến độ và trang ghế hiện "đã gia hạn, đây là lần cuối", đồng hồ chuyển màu cam. Ghế bị nhả: Lịch sử ghi lý do. Chưa có email, SMS |
| Đơn nhóm ở trang đơn cá nhân | `DatTour/HuyDatTour` và `DatTour/DoiChoNgoi` từ chối đơn nhóm, chuyển về trang đơn nhóm |
| Thứ tự làm | N3 (link mời) trước, rồi N4 (QR từng ghế, gồm trả hộ), N2 (quản lý đơn), N5 (tự động hết hạn), N6 (admin) |

Ràng buộc thêm (kiểm trong code):

| Mã | Ràng buộc |
|---|---|
| RB-21 | Chỉ trưởng nhóm (người tạo đơn) được thu hồi link, trả hộ, chốt sớm, hủy đơn nhóm |
| RB-22 | Ghế đã có giao dịch thành công thì không bị nhả khi hết hạn |
| RB-23 | Mã mời (`MaMoi`) đủ dài và ngẫu nhiên; thu hồi thì đổi mã, link cũ hết hiệu lực |
| RB-24 | Nhận ghế chỉ thành công khi ghế còn trống (chưa có tài khoản, chưa có họ tên, chưa nhả), không phải ghế trẻ nhỏ và đơn còn giữ chỗ chưa quá hạn; kiểm bằng một câu UPDATE có điều kiện nên hai người không nhận trùng một ghế |
| RB-25 | Người xem và sửa một ghế: chủ ghế (`TaiKhoanId`), hoặc trưởng nhóm khi ghế chưa có tài khoản. Trưởng nhóm không mở được ghế thành viên khác đã nhận (cụ thể hóa RB-20) |
| RB-26 | Mỗi ghế có tối đa một giao dịch đang chờ hoặc thành công. Báo chuyển khoản là một câu INSERT có điều kiện (ghế chưa nhả, đã điền họ tên và số giấy tờ, chưa có giao dịch `ChoXacNhan`/`ThanhCong`), nên chủ ghế và trưởng nhóm cùng bấm chỉ tạo một giao dịch. Sửa thông tin và trả lại ghế cũng kiểm điều kiện này trong câu UPDATE |
| RB-27 | Người báo chuyển khoản cho một ghế: chủ ghế, hoặc trưởng nhóm của đơn. Admin xác nhận hay từ chối bằng câu UPDATE có điều kiện `TrangThai = 'ChoXacNhan'`, nên một giao dịch chỉ được xử lý một lần |
| RB-28 | Nhả ghế (bớt ghế, chốt đơn) chỉ áp dụng cho ghế chưa có giao dịch `ChoXacNhan`/`ThanhCong`, kiểm trong cùng câu UPDATE. Báo chuyển khoản chỉ thành công khi đơn còn `GiuCho`. Chốt đổi trạng thái đơn trước rồi mới nhả ghế, trong một giao dịch CSDL, nên không có giao dịch mới chen vào ghế vừa bị nhả |
| RB-29 | Số chỗ trả lại cho lịch bằng đúng số ghế vừa nhả (đếm từ câu UPDATE), lịch đã hủy thì không cộng (cụ thể hóa RB-16, RB-17) |
| RB-30 | Mỗi đơn nhóm gia hạn tối đa một lần: câu UPDATE có điều kiện `DaGiaHan = 0`. Chốt và hủy tự động có điều kiện `TrangThai = 'GiuCho'`, nên dịch vụ nền và lúc mở trang xử lý cùng một đơn thì chỉ một nơi làm được |
| RB-31 | `LichTrinhTours.ViDo`, `KinhDo` (`decimal(9,6)`, Mo10): cùng rỗng, hoặc cùng có giá trị trong khung vĩ độ 8 đến 24, kinh độ 102 đến 110 (`CK_LichTrinh_ToaDo`). Khung này chỉ để bắt lỗi nhập nhầm, không phải ranh giới quốc gia. Hai cột phụ thuộc trực tiếp vào khóa `Id` của từng ngày, không suy ra từ cột khác nên vẫn đạt 3NF |
| RB-32 | `Tours.ViDoXuatPhat`, `KinhDoXuatPhat` (Mo11): cùng quy tắc với RB-31 (`CK_Tours_XuatPhat`). Điểm bắt đầu của ngày đầu tiên; từ ngày 2 điểm bắt đầu suy ra từ điểm của ngày trước nên không lưu |
| RB-34 | `BaiViets` (Mo12): `DuongDan` duy nhất; `TrangThai` thuộc `Nhap`, `DaDang`; bài đã đăng phải có `NgayDang` (`CK_BaiViet_NgayDang`). `TacGiaId` FK Restrict: tài khoản đã viết bài không xóa được. `TourId` FK SetNull: xóa tour thì bài vẫn còn. Tên tác giả và tên tour lấy qua khóa ngoại nên đạt 3NF. Nội dung lưu chữ thuần, không lưu HTML, hiển thị qua Razor nên không chèn được mã độc |
| RB-35 | `Videos` (Mo13): `MaYouTube` duy nhất, đúng 11 ký tự chữ, số, `_`, `-` (`CK_Video_MaYouTube`), chỉ lưu mã chứ không lưu tệp video. `TourId` FK SetNull: xóa tour thì video vẫn còn. Khách chỉ tải trình phát của YouTube sau khi bấm phát |
| RB-33 | `LichTrinhTours.DuongDi` (Mo11, `nvarchar(max)`): **dữ liệu dẫn xuất giữ làm bộ nhớ đệm có chủ đích**, tính được từ tọa độ qua dịch vụ OSRM, lưu lúc admin lưu tọa độ để khách xem không phụ thuộc dịch vụ ngoài. Đổi điểm của một ngày thì xóa đường của ngày đó và ngày kế tiếp rồi lấy lại, nên không bị lệch với tọa độ. Rỗng nghĩa là chưa lấy được, bản đồ nối tạm bằng nét đứt |
| RB-36 | Lịch có sơ đồ xe: mỗi ghế chưa nhả của đơn chưa hủy có đúng một dòng `ChoNgoiChuyenDi`, để số chỗ đã giữ (RB-12) luôn khớp số chỗ có người trên xe. Ghế không được chọn tay thì hệ thống xếp chỗ trống đầu tiên theo hàng (`XepChoNgoi.XepChoConThieu`) lúc tạo đơn nhóm, lúc admin thêm người vào đơn, lúc lịch được gán xe; dịch vụ chạy nền xếp bù cho lịch chưa khởi hành. Người dùng vẫn đổi chỗ được. Kiểm trong code (không có CHECK vì liên quan nhiều bảng), truy vấn đối chiếu ở `docs/sql/doi_chieu.sql` |
| RB-37 | `ChoKhongMoBans` (Mo14): chỗ trong bảng này không bán cho chuyến đó. Khách không chọn được, không được tự xếp vào (`XepChoNgoi.ViTriChonDuoc`, `ViTriTrong`); sơ đồ hiện sọc xám "Không mở bán". Admin bấm chọn ở trang **Chọn chỗ mở bán**; chỗ đã có khách không khóa được; số chỗ mở bán không nhỏ hơn số khách đang giữ. Thêm lịch với số chỗ mở bán nhỏ hơn xe thì khóa trước các chỗ cuối xe. **Số chỗ mở bán không lưu cột** (bỏ `SoChoToiDa` ở Mo15) nên không có dư thừa. Lưu chỗ khóa và tính lại `SoChoCon` trong cùng giao dịch, sau đó kiểm lại không có khách vừa ngồi vào chỗ vừa khóa |
| RB-38 | Đơn cá nhân có `HanGiuCho` lúc đặt, cùng số giờ với đơn nhóm (`DonNhom:SoGioGiuCho`, mặc định 24), muộn nhất cuối ngày trước ngày đi. Quá hạn mà vẫn `GiuCho`: nếu chọn tiền mặt (chưa ra quầy trả) thì dịch vụ chạy nền tự hủy, nhả ghế, bỏ chỗ ngồi, trả chỗ (`XuLyDon.HuyDonCaNhanQuaHan`, câu UPDATE có điều kiện nên không đè lên admin đang xác nhận). Nếu đã báo chuyển khoản hoặc ví thì **không tự hủy** để khách đã chuyển tiền không mất chỗ vì admin chậm; trang Đơn đặt tour hiện cảnh báo "quá hạn, cần kiểm sao kê". Đơn cũ không có hạn thì không tự hủy |
| RB-39 | Khách tự hủy đơn cá nhân chỉ khi đơn còn `GiuCho`, chưa có giao dịch thành công, không có chuyển khoản đang chờ, và chưa tới ngày đi (`DonDatTourDoc.KhachTuHuyDuoc`, kiểm cả ở máy chủ). Còn lại khách liên hệ TravelVN; hoàn tiền xử lý ngoài hệ thống. Admin vẫn hủy được. Giống quy tắc hủy đơn nhóm |
