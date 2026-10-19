# Kế hoạch 10 tuần: TravelVN đặt tour theo nhóm

Bám lịch giáo viên: **25/10** duyệt bài toán và yêu cầu, **08/11** duyệt mô hình dữ liệu và hệ thống, **29/11** bản MVP, **20/12** hoàn thiện, **21–27/12** nộp báo cáo lên LMS. Hằng tuần nộp tiến độ lên LMS và phiếu in có ký.

Nguyên tắc của giáo viên: bài toán nghiệp vụ, dữ liệu, hệ thống, giá trị đo được đi trước. **Giao diện làm sau khi có MVP.** Không làm kiểu "web CRUD + chatbot". Đây là kế hoạch đề xuất, chỉnh khi giáo viên yêu cầu thêm.

## Theo tuần

| Tuần | Ngày | Mốc | Sản phẩm | Tài liệu |
|---|---|---|---|---|
| 1 | 12–18/10 | Nộp phiếu đề tài | (xong) | Phiếu đề xuất, 6 câu hỏi bước 1 |
| 2 | 19–25/10 | **25/10: duyệt bài toán và yêu cầu** | Sao lưu DB local và host; chạy truy vấn kiểm tra trên host; sửa an toàn: dữ liệu lịch khởi hành thật trên host, HTTPS | Gửi khảo sát và thu phiếu; mô tả bài toán, phạm vi, yêu cầu chức năng và phi chức năng; bảng so sánh web tour; use case tổng quát |
| 3 | 26/10–01/11 | | | ERD hiện trạng, phụ thuộc hàm, phân tích 3NF, lược đồ quan hệ mới, ràng buộc toàn vẹn; đặc tả use case; số liệu khảo sát vào chương 1 |
| 4 | 02–08/11 | **08/11: duyệt mô hình dữ liệu và hệ thống** | Migration 3NF cho `DatTours` (mở rộng, chuyển, thu hẹp); xử lý đơn 17 và 19; tạo bảng đơn nhóm, ghế, giao dịch | Class diagram, sequence (tạo đơn, nhận ghế), activity (giữ chỗ và nhả ghế) |
| 5 | 09–15/11 | | Đơn nhóm: tạo đơn, link mời, nhận ghế, tự điền thông tin, quyền riêng tư; sửa lỗi lõi (mã chuyến, so khớp theo tên, kiểm tra lịch hủy) | Cập nhật sơ đồ theo code |
| 6 | 16–22/11 | | Hạn giữ chỗ, tác vụ nền nhả ghế, bảng tiến độ trưởng nhóm; QR chuyển khoản và **admin xác nhận thủ công** | Sequence thanh toán và hết hạn |
| 7 | 23–29/11 | **29/11: MVP** | Chạy trọn luồng từ tạo đơn đến thu đủ tiền; bảo mật (cookie, giới hạn đăng nhập sai, link mời); test tay | Checklist test, bản demo |
| 8 | 30/11–06/12 | | Dùng thử 2–3 nhóm thật, thu chỉ số (thời gian thu đủ tiền, số lần nhắc, tỉ lệ ghế nhả). Bài phụ KPI nhẹ nếu kịp. Xác nhận tự động qua webhook chỉ làm nếu dư thời gian | Số liệu dùng thử |
| 9 | 07–13/12 | | **Thiết kế lại giao diện toàn bộ** (sau MVP), kiểm tra trên điện thoại | Chụp ảnh trước và sau |
| 10 | 14–20/12 | **20/12: hoàn thiện** | Sửa lỗi cuối, đồng bộ DB host, build sạch | Hoàn thiện báo cáo, slide, tập demo |
| | 21–27/12 | Nộp LMS | | |

Ghi chú: trang đơn nhóm (tuần 5 đến 7) dùng giao diện tối thiểu đủ dùng và gọn, để tuần 9 làm đẹp đồng bộ cả web.

## Việc chạy song song

- **Khảo sát** gửi ngay (script `docs/TaoFormKhaoSat.gs`), thu đến cuối tuần 2 để kịp viết chương 1 và buổi duyệt 25/10. Mục tiêu 30–50 phiếu hợp lệ.
- **Báo cáo viết dần**, mỗi tuần một ít.
- Nếu làm xác nhận tự động: đăng ký dịch vụ ngay tuần 6 vì có thể mất vài ngày.

## Cổng kiểm tra cuối mỗi tuần

Build sạch, chạy được trên máy, việc đã duyệt đã xong, tài liệu tuần đã cập nhật, đã nộp LMS. Chưa đạt thì làm bù trước khi sang việc mới.

## Nếu trễ: thứ tự cắt giảm

1. Bài phụ (KPI).
2. Xác nhận thanh toán tự động (giữ xác nhận thủ công).
3. Trau chuốt giao diện trang quản trị (giữ phần dùng được).

**Không cắt:** chuẩn hóa 3NF, ràng buộc toàn vẹn, các sơ đồ chính, khảo sát, phần báo cáo cho bài toán chính.

## Quy tắc xuyên suốt

- Mọi thay đổi theo skill `safe-change`: hỏi trước, kiểm tra kỹ, bước nhỏ.
- Mọi thay đổi cấu trúc dữ liệu theo skill `db-3nf-review`.
- Chưa xác nhận điều gì thì ghi rõ là giả thuyết.
