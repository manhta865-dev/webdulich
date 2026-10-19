---
name: uml-docs
description: Vẽ và viết tài liệu phân tích thiết kế cho TravelVN theo mẫu báo cáo đồ án cơ sở: use case và đặc tả, class diagram, sequence diagram, activity diagram, state diagram, ERD, lược đồ quan hệ, và ràng buộc toàn vẹn dạng R1, R2 kèm bảng tầm ảnh hưởng. Dùng khi người dùng nhắc sơ đồ, UML, use case, diagram, ERD, ràng buộc toàn vẹn, hoặc sau khi sửa code làm thay đổi luồng nghiệp vụ hay cấu trúc dữ liệu.
---

# Tài liệu phân tích thiết kế (UML, ERD, ràng buộc)

Mọi sơ đồ phải **khớp với code hiện tại**. Trước khi vẽ, đọc controller, model và migration liên quan. Khi code đổi, cập nhật sơ đồ cùng lúc. Nếu sơ đồ mô tả thứ chưa làm, ghi rõ "thiết kế dự kiến".

## Công cụ và vị trí file

- Mặc định dùng **PlantUML**: mỗi sơ đồ một file `docs/diagrams/<ten>.puml`, xuất ảnh PNG vào `docs/img/` (VS Code có extension PlantUML, hoặc dùng trang PlantUML online). PlantUML vẽ được cả use case chuẩn UML mà Mermaid không có. Nếu người dùng muốn công cụ khác, hỏi trước.
- Tên file ngắn, không dấu: `usecase-tong-quat.puml`, `seq-nhan-ghe.puml`, `act-don-nhom.puml`.
- Đánh số hình theo chương như báo cáo cũ ("Hình 3.1", "Bảng 2.1") và ghi vào danh mục hình, danh mục bảng.

## Các tác nhân

Khách vãng lai, Khách hàng, Trưởng nhóm, Thành viên nhóm, Quản trị viên, Nhân viên (hướng dẫn viên, tài xế), và hệ thống ngoài **Dịch vụ xác nhận chuyển khoản**. Tác nhân nào chỉ có trong thiết kế dự kiến thì đánh dấu.

## 1. Use case

- Một sơ đồ tổng quát và vài sơ đồ chi tiết theo nhóm chức năng (khách, đơn nhóm, thanh toán, quản trị).
- Đặc tả từng use case chính theo đúng mẫu báo cáo cũ:

```
Use Case: <tên>
Tác nhân:
Mục tiêu:
Tiền điều kiện:
Hậu điều kiện:
Luồng xử lý chính: 1. ... 2. ...
Luồng thay thế:
```

- Use case chính của đề tài: Tạo đơn nhóm, Mời và nhận ghế, Điền thông tin hành khách, Thanh toán phần của mình, Theo dõi tiến độ đơn nhóm, Hết hạn giữ chỗ (chốt hoặc hủy), Nhận kết quả thanh toán (hệ thống ngoài).

## 2. Class diagram và ERD

- Class diagram từ các model trong `Models/`, thêm các lớp mới. Ghi thuộc tính chính, khóa, quan hệ và bội số.
- ERD thể hiện thực thể, khóa, quan hệ, số lượng (1-n, n-n).
- **Lược đồ quan hệ** dạng `TenBang(PK, cot, FK → Bang)`, một dòng mỗi bảng, đúng với migration mới nhất.

## 3. Sequence diagram

Vẽ cho các luồng quan trọng, mỗi luồng một sơ đồ, có đủ đối tượng: Người dùng, Trình duyệt, Controller, Service hoặc DbContext, Cơ sở dữ liệu, Dịch vụ ngoài.
- Tạo đơn nhóm và giữ chỗ
- Thành viên nhận ghế qua link mời
- Thanh toán QR và xác nhận qua webhook (kiểm tra khóa, kiểm tra số tiền, chống xử lý trùng)
- Hết hạn giữ chỗ (tác vụ nền nhả ghế)

## 4. Activity diagram và state diagram

- Activity: quy trình đặt tour nhóm từ lúc tạo đến lúc chốt, có nhánh quyết định (đủ tiền, hết hạn, trưởng nhóm hủy).
- State: trạng thái ghế (chưa có chủ, đã điền thông tin, đã trả, đã nhả, đã hoàn) và trạng thái đơn nhóm. Mỗi mũi tên ghi sự kiện gây chuyển trạng thái. Trạng thái trong sơ đồ phải khớp với giá trị cho phép trong CSDL.

## 5. Ràng buộc toàn vẹn theo mẫu báo cáo cũ

Chia sáu loại: miền giá trị, liên thuộc tính, liên bộ, phụ thuộc tồn tại, liên thuộc tính - liên quan hệ, thuộc tính tổng hợp. Mỗi ràng buộc theo mẫu:

```
R<n>: <mô tả ngắn>
Nội dung: <công thức logic, ví dụ ∀ t ∈ Tours: t.Gia > 0 ∧ t.SoNgay > 0>
Bối cảnh: <bảng>
Bảng tầm ảnh hưởng:
| R<n> | Thêm | Xóa | Sửa |
| <Bảng> | + | - | +(cột) |
```

Dấu `+` nghĩa là thao tác đó có thể vi phạm ràng buộc và cần kiểm tra, dấu `-` nghĩa là không ảnh hưởng.

Ví dụ cho đề tài mới (viết lại theo cấu trúc bảng thật khi làm):
- Miền giá trị: giá lúc đặt của ghế > 0; trạng thái ghế ∈ {ChuaCoChu, DaDien, DaTra, DaNha, DaHoan}.
- Liên bộ: mã mời của đơn nhóm duy nhất; mã tham chiếu ngân hàng của giao dịch duy nhất.
- Phụ thuộc tồn tại: mỗi ghế thuộc một đơn; mỗi giao dịch thuộc một ghế.
- Liên thuộc tính - liên quan hệ: ghế ở trạng thái DaTra thì tồn tại giao dịch thành công với số tiền khớp giá ghế; số ghế đang giữ của một lịch không vượt `SoChoToiDa`.
- Thuộc tính tổng hợp: số chỗ còn lại của lịch bằng `SoChoToiDa` trừ số ghế chưa nhả (nếu giữ như bộ nhớ đệm).

## Kiểm tra trước khi giao

- [ ] Tên bảng, cột, trạng thái trong sơ đồ khớp code và migration.
- [ ] Mỗi luồng chính trong `CLAUDE.md` đều có sequence hoặc activity tương ứng.
- [ ] Không có tác nhân hay chức năng "ma" (có trong hình nhưng không có trong code mà không ghi dự kiến).
- [ ] Đã cập nhật danh mục hình, danh mục bảng.
