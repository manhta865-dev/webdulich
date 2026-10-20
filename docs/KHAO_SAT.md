# Khảo sát: cách dùng và cách đọc kết quả

Form khảo sát tạo bằng `TaoFormKhaoSat.gs` (script.google.com, chạy hàm `taoForm`). 19 câu, ẩn danh, khoảng 3 phút, một mạch không chia theo vai trò.

## Mục tiêu

Kiểm chứng giả thuyết của bài toán. Kết quả chỉ nói về nhóm người khảo sát được (bạn bè, lớp, người quen), không đại diện cho cả thị trường.

## Câu hỏi và ngưỡng đọc kết quả (tự đặt trước khi xem số liệu)

| Nhóm | Câu | Ủng hộ bài toán | Không ủng hộ |
|---|---|---|---|
| Có nhu cầu đi nhóm | 2, 3, 5 | Từ khoảng 60% từng đi nhóm từ 4 người, nhóm thường 4–10 | Dưới 30% từng đi nhóm |
| Có vướng mắc thật | 6, 8, 9, 10, 11 | Có người ứng tiền (câu 6 là "Mình" hoặc "Người khác") và từ khoảng 50% chọn ít nhất 2 khó khăn ở câu 8 | Đa số "không gặp khó khăn" hoặc thu xong trong 1 ngày |
| Mức phiền | 12 | Trung bình từ khoảng 3,5/5 | Dưới 2,5/5 |
| Muốn dùng | 13 | Từ khoảng 50% chọn 4–5 | Dưới 30% chọn 4–5 |
| Tính năng đúng hướng | 14 | Tự trả phần mình, xem tiến độ, hạn giữ chỗ nằm trong top | Tính năng ít liên quan dẫn đầu |
| Thông số thiết kế | 15–17 | Chốt hạn giữ chỗ, cách trả, cách xử lý người trễ | Không có đúng sai |

## Lưu ý khi phân tích

- Chỉ tính câu 6–12 trên người trả lời "Có" ở câu 2.
- Câu 6: "Mình" là người đứng ra đặt (trưởng nhóm trong chuyến gần nhất), "Người khác" là thành viên. Đây là vai trong chuyến gần nhất, không phải loại người cố định. Tách số liệu nhóm "Mình" để đọc mức khó khăn của trưởng nhóm.
- Câu 13 là ý định, không phải hành vi. Báo cáo ghi "ý định sử dụng", không viết "nhu cầu thị trường".
- Kết quả trái giả thuyết vẫn dùng được: ghi rõ và điều chỉnh phạm vi.
- Mục tiêu 30–50 phiếu hợp lệ. Cho 2–3 người thử trước khi gửi rộng.
- Câu 14: "giấy tờ chỉ tôi và hệ thống thấy" nghĩa là trưởng nhóm và thành viên khác không xem được; công ty tour hoặc admin vẫn cần xem để làm thủ tục.

## Kết quả

Dữ liệu: `docs/du_lieu/khao_sat.csv` (ẩn danh). Chạy lại: `py -3 -I -X utf8 docs/tools/phan_tich_khao_sat.py docs/du_lieu/khao_sat.csv`.

- **Số phiếu hợp lệ: 40** (thu 19–20/10/2026; không thiếu câu bắt buộc, không trùng). Câu 6 đến 12 chỉ tính **38 người** trả lời "Có" ở câu 2.
- **Mẫu thuận tiện**: 24/40 phiếu nộp trong giờ đầu, 82,5% dưới 31 tuổi, chủ yếu người quen. Kết luận chỉ đúng cho nhóm này, không suy ra cho cả thị trường.

### So với ngưỡng

| Giả thuyết | Kết quả | Ngưỡng ủng hộ | Kết luận |
|---|---|---|---|
| Có nhu cầu đi nhóm từ 4 người | 38/40 (95%) | từ 60% | Ủng hộ |
| Gặp từ 2 khó khăn trở lên | 34/38 (89,5%); 0/38 chọn "Không gặp khó khăn" | từ 50% | Ủng hộ (xem hạn chế 1) |
| Mức phiền khi thu tiền nhóm (câu 12) | trung bình 4,61/5 (n=38) | từ 3,5 | Ủng hộ |
| Ý định dùng (câu 13) | 28/40 (70%) chọn 4 hoặc 5 | từ 50% | Ủng hộ |

Số liệu thêm (n=38): 52,6% là người đứng ra đặt và ứng tiền; 81,6% phải đi đòi lại tiền; 81,6% gặp người trả trễ hoặc bỏ nhóm; 90% từng gặp người báo bỏ đi sát ngày.

### Gợi ý cho thiết kế (đề xuất, cần giáo viên chốt)

- Hạn giữ chỗ (câu 15): 24 giờ và 48 giờ đều 45%. Làm tham số cấu hình, mặc định 24 giờ.
- Xử lý người trễ (câu 17): 55% muốn nhắc thêm một lần rồi mới nhả, 35% muốn trưởng nhóm tự quyết, 10% muốn nhả ngay.
- Cách trả (câu 16): ví điện tử 60%, QR chuyển khoản 32,5%. Phạm vi hiện tại là QR và admin xác nhận thủ công; ví điện tử là hạn chế và hướng mở rộng.

### Hạn chế

1. **Câu 8:** 24/40 người chọn cả 7 khó khăn nên tỉ lệ từng mục bị thổi phồng. Loại nhóm này (còn 14 người), nổi bật nhất vẫn là "không biết ai đã trả" (10/14) và "không có hạn chốt rõ ràng" (9/14).
2. **Câu 14:** form ghi "tối đa 3" nhưng không chặn; 28/40 chọn nhiều hơn 3. Không dùng để xếp hạng. Chỉ tính 12 người đúng hướng dẫn (cỡ mẫu nhỏ): xem tiến độ 7/12, nhắc tự động 6/12, hạn giữ chỗ 6/12.
3. **Câu 12** có hiệu ứng trần (79% chọn 5). **Câu 13** là ý định, không phải hành vi, và chia hai cực (26 người chọn 5, 8 người chọn 1).
4. 8 người (20%) không muốn dùng, trong đó 6 người rất phiền việc thu tiền nhóm; khảo sát không hỏi lý do, nên hỏi thêm 2–3 người hoặc cho dùng thử.

## Số liệu khảo sát cũ (đồ án cơ sở) từng hiển thị trên trang Gợi ý tour

Bốn con số sau từng được viết cứng trên trang Gợi ý tour, không ghi cỡ mẫu. Đã gỡ khỏi web khi làm lại giao diện. Nếu dùng trong báo cáo thì phải ghi rõ đây là khảo sát cũ của đồ án cơ sở, cỡ mẫu 18 người (các tỉ lệ khớp với 11/18, 16/18, 14/18 và 18/18), không suy ra cho cả thị trường:

- 61,1%: chưa biết nên đi đâu khi muốn du lịch
- 88,9%: gặp khó khăn vì không biết chọn địa điểm
- 77,8%: xem gợi ý tour là chức năng quan trọng
- 100%: muốn hệ thống gợi ý tour theo nhu cầu
