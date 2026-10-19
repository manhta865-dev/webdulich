---
name: safe-change
description: Quy trình sửa code an toàn cho dự án TravelVN (ASP.NET Core MVC, EF Core): hỏi trước khi sửa, tìm mọi chỗ bị ảnh hưởng, sửa từng bước nhỏ, build, báo cách kiểm tra và cách hoàn tác. Dùng cho mọi yêu cầu sửa, thêm, xóa, đổi tên code, view, model, migration hoặc cấu hình, kể cả khi yêu cầu có vẻ nhỏ. Use for any code, schema or config change, refactor, bug fix or feature work in this repo.
---

# Sửa code an toàn

Mục tiêu: một thay đổi không được sinh ra lỗi ở nơi khác. Người dùng là sinh viên, không muốn bị bất ngờ, nên luôn xin phép trước và làm từng bước nhỏ.

## Quy trình

1. **Làm rõ.** Nhắc lại yêu cầu bằng một hai câu. Nếu thiếu thông tin quan trọng thì hỏi (vài câu ngắn), đừng đoán.

2. **Khảo sát, chưa sửa gì.** Đọc file sẽ sửa và mọi file liên quan: controller, view, model, DbContext, migration. Tìm mọi chỗ dùng thứ sắp đổi:
   - `grep -rn "TenCot" --include=*.cs --include=*.cshtml .`
   - `grep -rn "ViewBag.TenKey" Controllers Views`
   - `grep -rn "asp-action=\"TenAction\"" Views`
   - `grep -rn "Session.*\"tenKey\"" .`

   Lý do: ViewBag, Session key, tên action và route là chuỗi, trình biên dịch không bắt được. Build xanh chưa chắc chạy đúng.

3. **Báo kế hoạch và xin phép.** Viết ngắn gọn rồi dừng chờ người dùng đồng ý:
   - Mục tiêu
   - Sẽ sửa (từng file và nội dung)
   - Sẽ không đụng
   - Ảnh hưởng và rủi ro (liệt kê các chỗ tìm được ở bước 2)
   - Cách hoàn tác
   - Câu hỏi còn lại

4. **Sửa theo bước nhỏ.** Mỗi bước độc lập và build được. Sau mỗi bước chạy `dotnet build`, xử lý hết lỗi và cảnh báo mới do mình gây ra rồi mới sang bước sau. Thấy lỗi khác ngoài kế hoạch thì ghi lại và báo, chờ xin phép, không tự sửa lan.

5. **Kiểm tra.** Chạy test nếu có. Nếu không, đưa checklist test tay mà người dùng làm được trong vài phút: luồng chính, trường hợp biên, dữ liệu rỗng.

6. **Bàn giao.** Tóm tắt đã đổi gì (file, lý do), kết quả build, cách test, cách hoàn tác (`git revert`, script đảo ngược), việc còn lại. Commit nhỏ, thông điệp tiếng Việt, sau khi người dùng đồng ý.

## Đổi cấu trúc dữ liệu: mở rộng, chuyển, thu hẹp

1. **Mở rộng:** thêm cột hoặc bảng mới (cho phép rỗng). Code cũ vẫn chạy.
2. **Chuyển:** tính hoặc sao dữ liệu sang chỗ mới, đổi code đọc ghi sang chỗ mới, kiểm tra.
3. **Thu hẹp:** chỉ khi bước 2 xong và người dùng xác nhận mới bỏ cột cũ.

Mỗi migration chỉ làm một việc. Chi tiết xem skill `db-3nf-review`.

## Những thứ dễ gây lỗi dây chuyền ở dự án này

- Cột của `DatTours` đang được dùng ở controller đặt tour, lịch sử, các trang báo cáo doanh thu và quản lý đơn của admin.
- Session key (`user`, `role`, `taiKhoanId`, các key `DatTour_...`, `MaChuyenDi`) dùng rải rác nhiều controller.
- Tên action và controller được view gọi bằng chuỗi (`asp-action`, `RedirectToAction`).
- Vai trò là chuỗi (`admin`, `user`, `huongdanvien`, `taixe`), gõ lệch là mất quyền.
- Quan hệ xóa dây chuyền (Cascade) có thể xóa dữ liệu đơn đặt ngoài ý muốn.

## Dừng và hỏi khi

- Thao tác không hoàn tác được: xóa dữ liệu, cột, bảng; chạy script lên database host.
- Cần quyết định nghiệp vụ (luật giữ chỗ, cách chia tiền, hoàn tiền).
- Kế hoạch cần mở rộng hơn phạm vi đã được duyệt.
- Không chạy được build hoặc test.

## Không bao giờ

- Sửa database host trực tiếp, in chuỗi kết nối, khóa API hay mật khẩu. Chỉ viết script để người dùng tự chạy sau khi sao lưu.
- Đổi tên hoặc xóa thứ đang được dùng khi chưa tìm hết chỗ dùng.
- Gộp nhiều việc không liên quan vào một thay đổi.
- Nói "đã xong, chạy tốt" khi chưa build, hoặc không nói rõ phần nào chưa kiểm chứng được.
