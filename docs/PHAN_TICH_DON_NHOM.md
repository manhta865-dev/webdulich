# Phân tích thiết kế: đặt tour nhóm, thanh toán chia phần

Tài liệu cho phần chính của đề tài, khớp code sau các bước N1 đến N6 (commit ngày 01/11/2026). Sơ đồ viết bằng PlantUML trong `docs/diagrams/`. Xuất ảnh PNG vào `docs/img/` bằng extension PlantUML của VS Code (Alt+D xem trước, chuột phải → Export). Máy đã có Java, chưa có `plantuml.jar`.

Đánh số theo khung báo cáo: Chương 2 (mô hình dữ liệu, ràng buộc toàn vẹn), Chương 3 (phân tích thiết kế hướng đối tượng). Khi ghép báo cáo, đổi số cho liền mạch với các hình, bảng khác.

Mục nào **chưa có trong code** đều ghi rõ "dự kiến".

## Danh mục hình

| Hình | Nội dung | File |
|---|---|---|
| Hình 3.1 | Use case đơn nhóm | `diagrams/usecase-don-nhom.puml` |
| Hình 3.2 | Activity: quy trình đặt tour nhóm từ tạo đơn tới chốt | `diagrams/act-don-nhom.puml` |
| Hình 3.3 | Sequence: nhận ghế qua link, điền thông tin, trả tiền, admin xác nhận | `diagrams/seq-nhan-ghe-tra-tien.puml` |
| Hình 3.4 | Sequence: xử lý đơn hết hạn giữ chỗ | `diagrams/seq-het-han.puml` |
| Hình 3.5 | State: vòng đời đơn nhóm | `diagrams/state-don-nhom.puml` |
| Hình 3.6 | State: vòng đời một ghế | `diagrams/state-ghe.puml` |

## Danh mục bảng

| Bảng | Nội dung |
|---|---|
| Bảng 3.1 đến 3.7 | Đặc tả use case chính |
| Bảng 2.1 | Ràng buộc toàn vẹn của đơn nhóm và bảng tầm ảnh hưởng (R20 đến R26) |
| Bảng 2.2 | Quy tắc phân quyền của đơn nhóm (kiểm trong code) |

---

## 1. Tác nhân

| Tác nhân | Mô tả | Trong code |
|---|---|---|
| Khách vãng lai | Chưa đăng nhập, mở được trang mời để xem chuyến | Có |
| Khách hàng | Đã đăng nhập (vai trò `user`) | Có |
| Trưởng nhóm | Khách hàng tạo đơn nhóm (`DatTours.TaiKhoanId`) | Có |
| Thành viên nhóm | Khách hàng nhận một ghế trong đơn của người khác (`HanhKhachDatTour.TaiKhoanId`) | Có |
| Quản trị viên | Vai trò `admin`, đối chiếu sao kê, xác nhận tiền, hủy đơn | Có |
| Dịch vụ hết hạn | `Services/XuLyHetHanDonNhom` chạy nền 5 phút một lần | Có |
| Dịch vụ xác nhận chuyển khoản | Webhook ngân hàng hoặc cổng trung gian báo tiền về | **Dự kiến**, giai đoạn này admin xác nhận tay |

## 2. Use case (Hình 3.1)

Trưởng nhóm và thành viên đều là khách hàng (quan hệ kế thừa), nên các use case trên một ghế (điền thông tin, đổi chỗ, trả lại ghế, thanh toán) dùng chung cho cả hai.

**Bảng 3.1. Đặc tả use case Tạo đơn nhóm**
```
Use Case: Tạo đơn nhóm
Tác nhân: Trưởng nhóm
Mục tiêu: Giữ chỗ có hạn cho cả nhóm, mỗi ghế có mã thanh toán riêng, có link mời
Tiền điều kiện: Đã đăng nhập vai trò user; lịch đang mở bán, chưa tới ngày khởi hành, còn chỗ
Hậu điều kiện: Đơn LoaiDon = Nhom, TrangThai = GiuCho, HanGiuCho đã tính; SoChoCon của lịch giảm đúng số ghế;
               mỗi ghế có MaThanhToan = MaChuyenDi-n; đơn có MaMoi ngẫu nhiên
Luồng xử lý chính:
 1. Trưởng nhóm chọn lịch, bấm "Đặt cho nhóm"
 2. Nhập số người lớn, trẻ nhỏ (ít nhất 2 ghế), thông tin liên hệ, chọn có đi cùng hay không
 3. Hệ thống hiện hạn giữ chỗ dự kiến = min(bây giờ + 24 giờ, 23:59:59 hôm trước ngày đi)
 4. Lịch có sơ đồ: trưởng nhóm chọn sẵn chỗ cho các ghế (không bắt buộc)
 5. Hệ thống trừ chỗ có điều kiện, tạo đơn, ghế, chỗ ngồi trong một giao dịch CSDL
 6. Chuyển tới trang tiến độ có lời mời soạn sẵn
Luồng thay thế:
 3a. Ngày đi là hôm nay: báo "đơn nhóm phải tạo trước ngày khởi hành", dừng
 5a. Lịch vừa hết chỗ: hủy giao dịch, báo giảm số ghế hoặc chọn lịch khác
 5b. Chỗ vừa có người chọn: hủy giao dịch, quay lại bước 4
```

**Bảng 3.2. Đặc tả use case Nhận ghế qua link**
```
Use Case: Nhận ghế qua link
Tác nhân: Thành viên nhóm (bao gồm Đăng nhập)
Mục tiêu: Thành viên giữ một ghế đứng tên mình
Tiền điều kiện: Link có MaMoi đúng; đơn GiuCho, chưa quá hạn, chưa tới ngày đi, lịch chưa hủy
Hậu điều kiện: Ghế có TaiKhoanId của thành viên; mỗi tài khoản tối đa một ghế trong một đơn
Luồng xử lý chính:
 1. Thành viên mở link, thấy tour, ngày đi, hạn trả, danh sách ghế (không có tên người đã nhận)
 2. Bấm "Nhận ghế này"
 3. Hệ thống chạy một câu UPDATE có điều kiện "ghế còn trống, không phải trẻ nhỏ, chưa có ghế trong đơn"
 4. Chuyển tới trang "Ghế của tôi"
Luồng thay thế:
 1a. Link sai, đã đổi, đơn đã chốt hoặc hủy: trang "Link mời không còn hiệu lực"
 1b. Đã có ghế trong đơn: chuyển thẳng tới ghế của mình
 2a. Chưa đăng nhập: chuyển tới đăng nhập hoặc đăng ký, xong quay lại đúng link
 3a. Ghế vừa có người nhận: báo, quay lại danh sách
```

**Bảng 3.3. Đặc tả use case Điền thông tin hành khách**
```
Use Case: Điền thông tin hành khách
Tác nhân: Khách hàng (chủ ghế), Trưởng nhóm (ghế không có tài khoản)
Mục tiêu: Ghế có họ tên, loại và số giấy tờ đúng giấy tờ mang theo
Tiền điều kiện: Đơn còn nhận thay đổi; ghế chưa báo chuyển khoản, chưa trả
Hậu điều kiện: HoTen, LoaiGiayTo, SoGiayTo của ghế được lưu
Luồng xử lý chính:
 1. Mở "Ghế của tôi" (chủ ghế) hoặc bấm "Điền hộ" ở trang tiến độ (trưởng nhóm)
 2. Nhập họ tên, chọn loại giấy tờ, nhập số giấy tờ
 3. Hệ thống ghi bằng UPDATE có điều kiện "đúng chủ ghế, chưa có giao dịch chờ hoặc thành công"
Luồng thay thế:
 2a. Thiếu thông tin: báo lỗi từng ô
 3a. Ghế vừa có thành viên nhận hoặc vừa báo chuyển khoản: không lưu, báo lý do
```

**Bảng 3.4. Đặc tả use case Thanh toán phần của mình**
```
Use Case: Thanh toán phần của mình (mở rộng: Trả hộ ghế khác)
Tác nhân: Khách hàng (chủ ghế), Trưởng nhóm (trả hộ)
Mục tiêu: Mỗi người tự chuyển khoản đúng phần của ghế mình
Tiền điều kiện: Đơn còn nhận thay đổi; ghế đã điền đủ họ tên và số giấy tờ; chưa có giao dịch chờ hoặc thành công
Hậu điều kiện: Có GiaoDichThanhToan(HanhKhachDatTourId, NguoiTraId, SoTien = GiaLucDat, ChoXacNhan); thông tin ghế bị khóa
Luồng xử lý chính:
 1. Trang ghế hiện QR VietQR (tài khoản ACB trong cấu hình), số tiền và nội dung = MaThanhToan đã điền sẵn
 2. Người trả quét QR bằng app ngân hàng và chuyển tiền
 3. Bấm "Tôi đã chuyển khoản", xác nhận trong hộp thoại
 4. Hệ thống tạo giao dịch chờ bằng một câu INSERT có điều kiện
Luồng thay thế:
 1a. Chọn tab "Ví MoMo": QR cố định, người trả tự gõ số tiền và nội dung; giao dịch ghi PhuongThuc = MoMo
 1b. Trưởng nhóm trả hộ ghế thành viên: trang riêng chỉ có tên, giá, QR, không có giấy tờ
 4a. Ghế vừa được báo chuyển khoản (bấm hai lần, hoặc chủ ghế và trưởng nhóm cùng bấm): không tạo thêm, báo lý do
```

**Bảng 3.5. Đặc tả use case Theo dõi và quản lý đơn nhóm**
```
Use Case: Theo dõi tiến độ đơn nhóm (mở rộng: đổi link mời, mời ra khỏi ghế, bớt ghế, chốt sớm, hủy)
Tác nhân: Trưởng nhóm
Mục tiêu: Biết ai đã nhận, đã điền, đã trả; điều chỉnh đơn khi cần
Tiền điều kiện: Là người tạo đơn
Hậu điều kiện: Tùy thao tác (xem Hình 3.5, 3.6)
Luồng xử lý chính:
 1. Mở trang tiến độ: hệ thống xử lý hết hạn trước (nếu đơn đã quá hạn) rồi hiện trạng thái
 2. Thấy tên, trạng thái, chỗ ngồi từng ghế; không thấy giấy tờ của người khác
 3. Chọn thao tác: đổi link mời, mời ra, bớt ghế, chốt sớm, hủy; xác nhận trong hộp thoại
Luồng thay thế:
 1a. Đơn không còn nhận thay đổi: ẩn các nút, hiện lý do (tới ngày đi, hết hạn đợi admin, đã chốt, đã hủy)
 3a. Chốt khi còn ghế chờ xác nhận hoặc chưa ai trả: không cho chốt, báo lý do
 3b. Hủy khi đã có người trả hoặc báo chuyển khoản: không cho hủy, hướng dẫn liên hệ TravelVN
```

**Bảng 3.6. Đặc tả use case Xác nhận tiền từng ghế**
```
Use Case: Xác nhận, từ chối tiền từng ghế
Tác nhân: Quản trị viên (dự kiến thêm: Dịch vụ xác nhận chuyển khoản)
Mục tiêu: Ghế chỉ thành "Đã trả" khi tiền thật đã về
Tiền điều kiện: Giao dịch của ghế đang ChoXacNhan
Hậu điều kiện: Giao dịch ThanhCong (ghế Đã trả) hoặc ThatBai (ghế về Đã điền, chưa trả); ghi người và giờ xác nhận
Luồng xử lý chính:
 1. Admin đọc sao kê, dán nội dung chuyển khoản (TV-XXXXXX-n) vào ô tìm ở danh sách đơn
 2. Mở đơn, xem bảng thanh toán: ghế, người trả (trả hộ hay không), phương thức, số tiền
 3. Bấm "Đã nhận" hoặc "Từ chối", xác nhận trong hộp thoại
 4. Hệ thống đổi trạng thái giao dịch bằng UPDATE có điều kiện "đang ChoXacNhan"
Luồng thay thế:
 3a. Ghế đã bị nhả: chỉ được từ chối, hoàn tiền ngoài hệ thống
 4a. Giao dịch vừa được xử lý (hai admin cùng bấm): báo "đã xử lý trước đó"
 4b. Ghế đã có giao dịch thành công khác (chỉ mục duy nhất chặn): báo, đề nghị từ chối
```

**Bảng 3.7. Đặc tả use case Hết hạn giữ chỗ**
```
Use Case: Hết hạn giữ chỗ (gia hạn, chốt hoặc hủy)
Tác nhân: Dịch vụ hết hạn (5 phút một lần); cũng chạy khi có người mở trang đơn nhóm
Mục tiêu: Không đơn nào giữ chỗ mãi; ghế chưa trả được trả lại cho người khác đặt
Tiền điều kiện: Đơn Nhom, GiuCho, HanGiuCho <= bây giờ
Hậu điều kiện: Một trong: gia hạn (DaGiaHan = 1), nhả ghế chưa trả và đợi admin, DaXacNhan, DaHuy
Luồng xử lý chính:
 1. Đếm ghế còn hiệu lực theo trạng thái: đã trả, đang chờ, chưa trả
 2. Mọi ghế đã trả: chốt
 3. Còn ghế chưa trả, chưa gia hạn: gia hạn 6 giờ tính từ lúc xử lý, không vượt 23:59:59 hôm trước ngày đi
 4. Hết lượt gia hạn: nhả ghế chưa trả, trả chỗ cho lịch; có ghế đã trả thì chốt, không ai trả thì hủy
Luồng thay thế:
 3a. Đã chạm mốc trước ngày đi: không gia hạn được, làm như bước 4
 4a. Còn ghế chờ admin: nhả phần chưa trả, đơn giữ GiuCho; lần xử lý sau khi admin xong mới chốt hoặc hủy
 *a. Hai nơi xử lý cùng lúc: câu UPDATE có điều kiện chỉ cho một nơi làm được
```

## 3. Activity (Hình 3.2)

Quy trình đi qua bốn làn: Trưởng nhóm, Hệ thống, Thành viên, Quản trị viên. Ba nhánh quyết định chính:
- **Đủ tiền hay chưa:** mọi ghế đã trả thì chốt ngay khi hết hạn.
- **Hết hạn:** gia hạn một lần, sau đó nhả, rồi chốt hoặc hủy.
- **Trưởng nhóm chốt sớm hoặc hủy:** có điều kiện chặn.

## 4. Sequence (Hình 3.3, Hình 3.4)

- **Hình 3.3** đi theo đúng thứ tự action:
  - `DonNhomController.Moi` → `NhanGhe` → `ThongTinGhe` → `DaChuyenKhoan`.
  - Phía admin: `AdminController.XacNhanGiaoDichGhe`.
  - Mỗi bước ghi dữ liệu là một câu SQL có điều kiện, kèm mã ràng buộc nó bảo đảm.
- **Hình 3.4** cho thấy hai nơi cùng gọi `XuLyDon.XuLyDonQuaHan`: dịch vụ nền và controller khi có người mở trang. Bốn nhánh xử lý ứng với Bảng 3.7.
- **Dự kiến, chưa vẽ:** sequence xác nhận qua webhook (kiểm khóa bí mật, số tiền, chống xử lý trùng theo `MaThamChieuNganHang`). Chỉ làm nếu còn thời gian.

## 5. State (Hình 3.5, Hình 3.6)

- **Đơn nhóm:** lưu ở `DatTours.TrangThai`, CHECK chỉ nhận `GiuCho`, `DaXacNhan`, `DaHuy`, và `DatTours.DaGiaHan`. Các trạng thái con của `GiuCho` (trong hạn, đã gia hạn, đợi admin) được tính từ `HanGiuCho`, `DaGiaHan` và giao dịch.
- **Ghế:** không có cột trạng thái. `TrangThaiGhe.Tinh` (trong `Models/DonNhomViewModel.cs`) tính theo thứ tự ưu tiên:
  1. `NgayNha` có giá trị: Đã nhả.
  2. Có giao dịch `ThanhCong`: Đã trả.
  3. Có giao dịch `ChoXacNhan`: Chờ xác nhận thanh toán.
  4. Có họ tên và số giấy tờ: Đã điền, chưa trả.
  5. Có `TaiKhoanId`: Đã nhận, chưa điền thông tin.
  6. Còn lại: Chưa có người nhận.
- **Chưa có trạng thái "Đã hoàn":** hoàn tiền làm ngoài hệ thống. Ghế đã trả mà đơn bị admin hủy thì chuyển sang Đã nhả, giao dịch `ThanhCong` vẫn giữ để đối chiếu.

---

## 6. Ràng buộc toàn vẹn của đơn nhóm (Bảng 2.1)

Tiếp nối R1 đến R19 ở `MO_HINH_DU_LIEU.md` mục 6. Mã RB-xx trong code và tài liệu cũ được ghi kèm để tra.

Ký hiệu:
- **HK** = `HanhKhachDatTour` (ghế)
- **GD** = `GiaoDichThanhToan`
- **D** = `DatTours`
- **L** = `LichKhoiHanhTour`
- **don(h)** = đơn chứa ghế h
- **lich(d)** = lịch của đơn d

Trong bảng tầm ảnh hưởng:
- **+** nghĩa là thao tác đó có thể vi phạm ràng buộc, phải kiểm tra.
- **-** nghĩa là không ảnh hưởng.
- Cột "Kiểm ở đâu" nói rõ ràng buộc do CSDL chặn hay do code chặn.

### 6.1. Ràng buộc liên bộ

**R20: Mã mời của đơn duy nhất** (RB-02, RB-23)
```
Nội dung: ∀ d1, d2 ∈ D: d1 ≠ d2 ∧ d1.MaMoi ≠ null ⇒ d1.MaMoi ≠ d2.MaMoi
Bối cảnh: DatTours
Kiểm ở đâu: chỉ mục duy nhất IX_DatTours_MaMoi (lọc IS NOT NULL); code sinh 18 byte ngẫu nhiên và thử lại tối đa 20 lần
```
| R20 | Thêm | Xóa | Sửa |
|---|---|---|---|
| DatTours | + | - | +(MaMoi) |

**R21: Mỗi ghế tối đa một giao dịch đang chờ hoặc thành công** (RB-05, RB-26)
```
Nội dung: ∀ g1, g2 ∈ GD: g1 ≠ g2 ∧ g1.HanhKhachDatTourId = g2.HanhKhachDatTourId ≠ null
          ⇒ ¬(g1.TrangThai ∈ {ChoXacNhan, ThanhCong} ∧ g2.TrangThai ∈ {ChoXacNhan, ThanhCong})
Bối cảnh: GiaoDichThanhToans
Kiểm ở đâu: phần ThanhCong do chỉ mục duy nhất có lọc chặn; phần ChoXacNhan do câu INSERT có điều kiện NOT EXISTS
            trong DonNhom/DaChuyenKhoan
```
| R21 | Thêm | Xóa | Sửa |
|---|---|---|---|
| GiaoDichThanhToans | + | - | +(TrangThai, HanhKhachDatTourId) |

**R22: Mỗi tài khoản tối đa một ghế trong một đơn** (RB-03)
```
Nội dung: ∀ h1, h2 ∈ HK: h1 ≠ h2 ∧ h1.DatTourId = h2.DatTourId ∧ h1.TaiKhoanId ≠ null ⇒ h1.TaiKhoanId ≠ h2.TaiKhoanId
Bối cảnh: HanhKhachDatTours
Kiểm ở đâu: chỉ mục duy nhất có lọc (DatTourId, TaiKhoanId); câu UPDATE nhận ghế có thêm NOT EXISTS
```
| R22 | Thêm | Xóa | Sửa |
|---|---|---|---|
| HanhKhachDatTours | + | - | +(TaiKhoanId, DatTourId) |

### 6.2. Ràng buộc liên thuộc tính

**R23: Ghế có tài khoản không phải ghế trẻ nhỏ** (RB-24)
```
Nội dung: ∀ h ∈ HK, don(h).LoaiDon = Nhom: h.TaiKhoanId ≠ null ⇒ h.LoaiHanhKhach ≠ 'Trẻ nhỏ'
Bối cảnh: HanhKhachDatTours
Kiểm ở đâu: code (câu UPDATE nhận ghế loại trừ trẻ nhỏ; ghế của trưởng nhóm là ghế người lớn đầu tiên)
```
| R23 | Thêm | Xóa | Sửa |
|---|---|---|---|
| HanhKhachDatTours | + | - | +(TaiKhoanId, LoaiHanhKhach) |

### 6.3. Ràng buộc liên thuộc tính, liên quan hệ

**R24: Ghế có giao dịch đang chờ hoặc thành công thì đã điền đủ thông tin**, và thông tin đó không đổi nữa (RB-26)
```
Nội dung: ∀ h ∈ HK: (∃ g ∈ GD: g.HanhKhachDatTourId = h.Id ∧ g.TrangThai ∈ {ChoXacNhan, ThanhCong})
          ⇒ h.HoTen ≠ '' ∧ h.SoGiayTo ≠ ''
Bối cảnh: HanhKhachDatTours, GiaoDichThanhToans
Kiểm ở đâu: code (INSERT giao dịch kiểm HoTen, SoGiayTo; UPDATE thông tin, trả lại ghế, mời ra kiểm NOT EXISTS giao dịch)
```
| R24 | Thêm | Xóa | Sửa |
|---|---|---|---|
| HanhKhachDatTours | - | - | +(HoTen, SoGiayTo, TaiKhoanId) |
| GiaoDichThanhToans | + | - | +(TrangThai) |

**R25: Ghế đã nhả của đơn chưa hủy không có giao dịch đang chờ hoặc thành công** (RB-22, RB-28)
```
Nội dung: ∀ h ∈ HK: h.NgayNha ≠ null ∧ don(h).TrangThai ≠ DaHuy
          ⇒ ¬∃ g ∈ GD: g.HanhKhachDatTourId = h.Id ∧ g.TrangThai ∈ {ChoXacNhan, ThanhCong}
Ý nghĩa: không bao giờ nhả ghế đã có tiền hoặc đang chờ xác nhận tiền, trừ khi cả đơn bị hủy (hoàn tiền ngoài hệ thống)
Bối cảnh: HanhKhachDatTours, GiaoDichThanhToans, DatTours
Kiểm ở đâu: code (mọi câu nhả ghế đều có NOT EXISTS giao dịch; báo chuyển khoản cần đơn GiuCho và ghế chưa nhả;
            chốt đổi trạng thái đơn trước rồi mới nhả, trong một giao dịch CSDL)
```
| R25 | Thêm | Xóa | Sửa |
|---|---|---|---|
| HanhKhachDatTours | - | - | +(NgayNha) |
| GiaoDichThanhToans | + | - | +(TrangThai) |
| DatTours | - | - | +(TrangThai) |

**R26: Hạn giữ chỗ trước ngày đi, gia hạn tối đa một lần** (RB-30, quyết định 31/10/2026)
```
Nội dung: ∀ d ∈ D, d.LoaiDon = Nhom: d.HanGiuCho < lich(d).NgayKhoiHanh (tính theo ngày, muộn nhất 23:59:59 hôm trước)
          và DaGiaHan chỉ đổi từ 0 sang 1, không đổi ngược
Bối cảnh: DatTours, LichKhoiHanhTours
Kiểm ở đâu: code (XuLyDon.TinhHanGiuCho khi tạo đơn và khi gia hạn; UPDATE gia hạn có điều kiện DaGiaHan = 0;
            Admin/CapNhatLichKhoiHanh không cho đổi ngày của lịch đã có khách giữ chỗ)
```
| R26 | Thêm | Xóa | Sửa |
|---|---|---|---|
| DatTours | + | - | +(HanGiuCho, DaGiaHan) |
| LichKhoiHanhTours | - | - | +(NgayKhoiHanh) |

> **Sửa `NgayKhoiHanh` (sửa ngày 01/11/2026):** lịch có `SoChoToiDa - SoChoCon > 0` thì không đổi được ngày đi. Ô ngày bị khóa ở trang Lịch đi, máy chủ cũng chặn nếu yêu cầu gửi thẳng. Giờ đi, giá, số chỗ vẫn sửa được. Muốn dời ngày thì hủy lịch (các đơn bị hủy, chỗ trả lại) rồi tạo lịch mới. Luật này giữ R26, và bảo vệ cả đơn cá nhân: khách đặt đi ngày nào thì không bị dời ngày mà không biết.

### 6.4. Ràng buộc thuộc tính tổng hợp

**R12 (nhắc lại, cụ thể hóa cho đơn nhóm):** `SoChoCon` của lịch bằng `SoChoToiDa` trừ số ghế chưa nhả của các đơn chưa hủy (RB-12, RB-16, RB-29).
- Mọi thao tác nhả ghế của đơn nhóm (bớt ghế, chốt, hết hạn, hủy) cộng lại đúng số dòng mà câu UPDATE nhả ghế vừa đổi, trong cùng giao dịch CSDL.
- Lịch đã hủy thì không cộng.
- Kiểm định kỳ bằng `docs/sql/doi_chieu.sql`. Sau mọi lần test N1 đến N6, truy vấn này đều trả về rỗng.

### 6.5. Quy tắc phân quyền (Bảng 2.2, kiểm trong code, không phải ràng buộc dữ liệu)

| Mã | Quy tắc | Chỗ kiểm |
|---|---|---|
| RB-20, RB-25 | Trưởng nhóm thấy tên và trạng thái mọi ghế, **không thấy giấy tờ** của ghế thành viên khác đã nhận. Chỉ chủ ghế, hoặc trưởng nhóm với ghế chưa có tài khoản, mới mở được trang ghế | `DonNhomController.LayGheDuocMo`, trang trả hộ không có giấy tờ |
| RB-21 | Chỉ người tạo đơn được đổi link mời, mời ra khỏi ghế, bớt ghế, trả hộ, chốt sớm, hủy đơn nhóm | `LayDonCuaTruongNhom`, `LayGheDuocTra` |
| RB-27 | Người báo chuyển khoản: chủ ghế hoặc trưởng nhóm. Người xác nhận hay từ chối tiền: chỉ admin, mỗi giao dịch xử lý một lần | `DaChuyenKhoan`, `AdminController.XacNhanGiaoDichGhe` |
| Trang mời | Ai có link cũng xem được nên không hiện tên người đã nhận ghế | `DonNhom/Moi` |
| Trang đơn cá nhân | `DatTour/HuyDatTour`, `DatTour/DoiChoNgoi`, `Admin/DoiSoNguoiDon`, `Admin/XacNhanDaNhanTien` từ chối đơn nhóm | các action tương ứng |

---

## 7. Đưa lên web host

Từ N2 tới N6 **không đổi cấu trúc CSDL**. Script cuối cùng cho host là `docs/sql/Mo9_DonNhom.sql`.

1. **Sao lưu database host** trước, như mọi lần.
2. Nếu host **chưa chạy** `Mo9_DonNhom.sql` thì chạy script đó trước (thêm `NguoiTraId`, `DaGiaHan`). Kiểm tra:
   ```sql
   SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'DatTours' AND COLUMN_NAME = 'DaGiaHan';
   ```
   Kết quả 1 là đã có cột.
3. Publish code mới. **Chép kèm `appsettings.json`**: có mục `DonNhom` (24 giờ, 6 giờ, quét 5 phút) và `ThanhToan` (ACB, MoMo). Không ghi đè chuỗi kết nối của host bằng chuỗi kết nối máy local.
4. Chép thêm thư mục mới `wwwroot/js/xac-nhan.js` nếu cách publish không tự chép.
5. **Kiểm tra sau khi đăng:**
   - Tạo một đơn nhóm 2 ghế ở lịch còn xa ngày đi. Lời mời có link `http://travelvn.runasp.net/DonNhom/Moi?ma=...`.
   - Mở link ở trình duyệt ẩn danh: xem được, nhận ghế thì đòi đăng nhập.
   - Trang ghế hiện QR ACB đúng tên, số tiền và nội dung. Tab MoMo hiện ảnh.
   - Báo chuyển khoản, rồi admin tìm theo nội dung chuyển khoản và xác nhận được.
   - Hủy đơn thử: `doi_chieu.sql` chạy trên host trả về rỗng.
6. **Lưu ý về dịch vụ nền:** host dùng chung có thể cho ứng dụng "ngủ" khi không ai truy cập. Đây là giả thuyết, chưa kiểm chứng trên asp.monster. Lúc ngủ, dịch vụ quét 5 phút không chạy, nhưng mọi trang đơn nhóm và trang chi tiết đơn của admin đều tự xử lý hết hạn khi mở, nên người dùng vẫn thấy đúng trạng thái.
7. Host đang chạy **HTTP** (lỗi 9 trong `CLAUDE.md`), nên nút "Chia sẻ qua ứng dụng" và Clipboard API có thể không chạy. Nút "Sao chép lời mời" đã có cách chép dự phòng.

## 8. Kiểm tra theo skill uml-docs

- [x] Tên bảng, cột, trạng thái trong sơ đồ khớp code: `GiuCho`, `DaXacNhan`, `DaHuy`; `ChoXacNhan`, `ThanhCong`, `ThatBai`; sáu trạng thái ghế của `TrangThaiGhe`.
- [x] Mỗi luồng chính trong `CLAUDE.md` có sơ đồ:
  - Tạo đơn và giữ chỗ: Hình 3.2.
  - Nhận ghế: Hình 3.3.
  - Thanh toán QR, xác nhận: Hình 3.3.
  - Hết hạn: Hình 3.4.
  - Webhook: dự kiến, chưa vẽ.
- [x] Thứ chưa có trong code (dịch vụ xác nhận chuyển khoản, trạng thái Đã hoàn, email nhắc) đều ghi "dự kiến" hoặc "chưa có".
- [x] Danh mục hình, danh mục bảng ở đầu tài liệu.
- [ ] Xuất ảnh PNG vào `docs/img/` (cần PlantUML, xem đầu tài liệu).
