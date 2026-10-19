# Đề tài, phạm vi và yêu cầu

Tài liệu gốc để Claude Code hiểu hệ thống cần làm gì. Khi mâu thuẫn với code cũ, tài liệu này đúng hơn, trừ khi người dùng nói khác.

## Đề tài (giáo viên đã duyệt)

Giải quyết khó khăn thu thông tin và thu tiền khi đặt tour theo nhóm: xây dựng hệ thống đặt tour nhóm với thanh toán chia phần trên ASP.NET Core MVC và SQL Server. Phát triển tiếp trên web TravelVN, không làm app di động.

## Bài toán (giả thuyết, đang kiểm chứng bằng khảo sát)

Khi cả nhóm đi tour, một người đứng ra đặt, xin giấy tờ từng người, ứng tiền rồi đi đòi lại. Không biết ai đã trả, không có hạn chốt rõ ràng. Các web đặt tour thường coi một đơn là của một người đặt, nên không hỗ trợ thu phần của từng thành viên (chưa kiểm chứng, cần bảng so sánh đối thủ).

## Giải pháp

Mỗi chỗ là một "ghế có chủ riêng".

1. Trưởng nhóm tạo đơn nhóm cho một lịch khởi hành, chọn số ghế, hệ thống giữ chỗ có hạn (đề xuất 24 giờ).
2. Trưởng nhóm gửi link mời. Mỗi thành viên mở link, nhận một ghế, tự điền thông tin của mình.
3. Mỗi thành viên tự trả phần của mình bằng QR chuyển khoản. Giai đoạn đầu **admin xác nhận thủ công**; xác nhận tự động qua webhook là phần mở rộng nếu dư thời gian.
4. Ghế quá hạn chưa trả thì được nhả. Khi hết hạn, đơn được chốt với các ghế đã trả hoặc hủy theo luật chốt.
5. Trưởng nhóm xem tiến độ (ai đã điền, ai đã trả) nhưng **không xem được số giấy tờ của người khác**.

## Phạm vi

Có làm:
- Đơn nhóm, ghế, link mời, hạn giữ chỗ, nhả ghế, bảng tiến độ.
- Thanh toán chia phần bằng QR, admin xác nhận.
- Chuẩn hóa CSDL 3NF, ràng buộc toàn vẹn, migration và script cho host.
- Sửa các lỗi nghiêm trọng trong code hiện có (xem `CLAUDE.md`).
- Thiết kế lại giao diện sau MVP.

Không làm:
- App di động.
- Hoàn thiện quy trình hoàn tiền (đã loại khỏi bài toán).
- Chatbot hoặc AI không có phương pháp đánh giá.
- Thanh toán thẻ hoặc ví điện tử thật.

Chưa chốt: bài phụ (gợi ý: bảng KPI và cảnh báo cho admin).

## Tác nhân

Khách chưa đăng nhập (xem tour), Thành viên (nhận ghế, tự điền, tự trả), Trưởng nhóm (tạo đơn, mời, xem tiến độ), Admin (xác nhận thanh toán, quản lý tour, lịch), Hướng dẫn viên và Tài xế (xem lịch làm việc, đã có sẵn).

## Yêu cầu chức năng (rút gọn)

| Mã | Yêu cầu | Ưu tiên |
|---|---|---|
| F1 | Trưởng nhóm tạo đơn nhóm cho một lịch khởi hành, chọn số ghế | Phải có |
| F2 | Hệ thống giữ chỗ có hạn, hiện đồng hồ còn lại | Phải có |
| F3 | Tạo link mời có token khó đoán, có thể thu hồi | Phải có |
| F4 | Thành viên nhận ghế, tự điền thông tin của mình | Phải có |
| F5 | Mỗi ghế có mã thanh toán riêng và QR riêng | Phải có |
| F6 | Admin xác nhận đã nhận tiền cho từng ghế | Phải có |
| F7 | Tự động nhả ghế quá hạn chưa trả | Phải có |
| F8 | Trưởng nhóm xem tiến độ, không xem giấy tờ người khác | Phải có |
| F9 | Luật chốt khi hết hạn (chốt các ghế đã trả hoặc hủy) | Phải có |
| F10 | Xác nhận thanh toán tự động qua webhook | Nên có, nếu dư thời gian |
| F11 | Bảng KPI cho admin (thời gian thu đủ tiền, tỉ lệ ghế nhả) | Bài phụ |

## Yêu cầu phi chức năng

- Bảo mật: HTTPS, cookie phiên an toàn, giới hạn đăng nhập sai, không lộ giấy tờ giữa người dùng, kiểm tra quyền trên mọi action.
- Đúng đắn: không bán vượt chỗ khi nhiều người đặt cùng lúc; giao dịch xác nhận không bị tính hai lần.
- Dữ liệu: đạt 3NF, mọi thay đổi cấu trúc có migration, script cho host và sao lưu trước.
- Ổn định: build sạch, sửa theo bước nhỏ, mỗi bước hoàn tác được.

## Cách đo giá trị (so với hiện trạng)

Đo bằng khảo sát và dùng thử 2–3 nhóm thật: thời gian từ lúc chốt đến thu đủ tiền, số lần phải nhắc, tỉ lệ ghế nhả do chưa trả, mức muốn dùng (thang 1–5). Ghi rõ cỡ mẫu và nhóm người khảo sát khi báo cáo; không suy ra cho cả thị trường.
