---
name: bao-cao-do-an
description: Viết các chương báo cáo đồ án chuyên ngành TravelVN theo khung báo cáo đồ án cơ sở: mô tả bài toán, khảo sát người dùng và đối thủ, hiện trạng, yêu cầu, mô hình dữ liệu, phân tích thiết kế, cài đặt và demo, kết luận. Dùng khi người dùng nhắc báo cáo, chương, khảo sát, form khảo sát, so sánh đối thủ, mục lục, hoặc cần viết nội dung để nộp cho giáo viên.
---

# Viết báo cáo đồ án

Khung bám theo báo cáo đồ án cơ sở (Mở đầu, 5 chương, tài liệu tham khảo). Viết bằng markdown trong `docs/bao-cao/` (mỗi chương một file) để người dùng ghép vào Word.

## Nguyên tắc

- **Không bịa số liệu.** Số liệu khảo sát phải đến từ dữ liệu thật của người dùng. Luôn nêu cỡ mẫu và cách khảo sát. Khảo sát cũ của đồ án cơ sở chỉ có 18 người, không dùng như số liệu chung.
- **Phân biệt sự thật và giả thuyết.** Điều chưa kiểm chứng (khó khăn của người dùng, đối thủ Việt Nam) ghi rõ là giả thuyết cho đến khi có khảo sát.
- **Mô tả đúng cái đã làm.** Phần cài đặt chỉ viết những gì đã chạy. Chưa làm thì ghi vào hướng phát triển. Chức năng còn giả lập thì nói rõ là giả lập.
- Trích nguồn tham khảo bằng tên và địa chỉ, tự diễn đạt, không chép nguyên văn.
- Tên bảng và cột trong báo cáo khớp CSDL thật. Số hình, số bảng theo chương.

## Khung chương

**Mở đầu.** Lý do chọn đề tài, mục tiêu, phạm vi, phương pháp (khảo sát, phân tích thiết kế, cài đặt, thử nghiệm).

**Chương 1. Mô tả chi tiết yêu cầu của đề tài.**
1.1 Giới thiệu bài toán. 1.2 Danh sách biểu mẫu. 1.3 Khảo sát người dùng (mới, cho bài toán nhóm). 1.4 Khảo sát giải pháp hiện có (bảng so sánh, xem bên dưới). 1.5 Hiện trạng và vấn đề cần giải quyết (hạn chế của TravelVN cũ, lấy từ code). 1.6 Bài toán cần giải quyết. 1.7 Yêu cầu người dùng (khách, trưởng nhóm, quản trị viên). 1.8 Yêu cầu chức năng. 1.9 Yêu cầu phi chức năng. 1.10 Quy trình nghiệp vụ chính. 1.11 Kiểm tra và xác thực thông tin.

**Chương 2. Xây dựng mô hình dữ liệu.**
2.1 Mục tiêu thiết kế. 2.2 Các thực thể. 2.3 ERD. 2.4 Chuyển ERD sang mô hình quan hệ (lược đồ quan hệ). 2.5 Ràng buộc toàn vẹn (sáu loại, có bảng tầm ảnh hưởng, xem skill `uml-docs`). 2.6 Chuẩn hóa 3NF: nêu phụ thuộc hàm từng bảng, phần đã chuẩn hóa, phần snapshot giá lúc đặt được giữ có chủ đích. 2.7 Cài đặt CSDL và migration.

**Chương 3. Phân tích và thiết kế hệ thống hướng đối tượng.**
3.1 Tác nhân. 3.2 Chức năng. 3.3 Use case. 3.4 Đặc tả use case chính. 3.5 Class diagram. 3.6 Sequence diagram. 3.7 Activity diagram và state diagram. 3.8 Chức năng đã làm và hướng phát triển.

**Chương 4. Cài đặt và triển khai.**
4.1 Môi trường. 4.2 Cấu trúc hệ thống. 4.3 Kết nối CSDL và triển khai lên host. 4.4 Giao diện mới (ảnh trước và sau, token thiết kế). 4.5 Chức năng khách hàng và đơn nhóm. 4.6 Chức năng quản trị. 4.7 Thử nghiệm với nhóm thật và kết quả (thời gian đủ tiền, số lần nhắc, tỷ lệ hoàn tất, tỷ lệ ghế bị nhả). 4.8 Đánh giá kết quả và hạn chế.

**Chương 5. Kết luận và hướng phát triển.**
5.1 Kết quả đạt được. 5.2 Hạn chế (thành thật). 5.3 Hướng phát triển (ghép khách đi lẻ, nhu cầu đặc biệt, chuyển nhượng suất, ứng dụng di động, trợ lý AI tư vấn).

**Tài liệu tham khảo.** Các giải pháp tương tự đã tìm hiểu: TourRadar (chia thanh toán), Airbnb (thanh toán nhóm), Splitcart, Vacation Labs (Flexi Payment), PHPTRAVELS (module tour đoàn), cộng với tài liệu kỹ thuật dùng (ASP.NET Core, EF Core, dịch vụ xác nhận chuyển khoản).

## Khảo sát người dùng (chương 1.3)

Form 40 đến 100 người (lớp, câu lạc bộ, bạn bè) cộng phỏng vấn 3 đến 5 người từng làm trưởng nhóm. Gợi ý câu hỏi:
1. Lần gần nhất bạn đi du lịch hoặc tour theo nhóm là khi nào, nhóm bao nhiêu người?
2. Ai đứng ra đặt và trả trước?
3. Khó khăn nào gặp nhiều nhất (ứng tiền, thu giấy tờ, nhắc người chưa trả, chốt ngày, chia tiền)? Mức phiền từ 1 đến 5.
4. Mất bao lâu để thu đủ tiền cả nhóm? Từng bị trả chậm hoặc không trả chưa?
5. Hiện bạn làm bằng cách nào (chat nhóm, bảng tính, chuyển khoản)?
6. Bạn có dùng thử công cụ giữ chỗ và chia tiền theo từng người không?

Kết quả trình bày: cỡ mẫu, nhóm đối tượng, biểu đồ, nhận xét, và nêu hạn chế (mẫu lấy thuận tiện, không đại diện).

## Bảng so sánh đối thủ (chương 1.4)

Tự kiểm tra 6 đến 8 web tour (ví dụ Traveloka, Klook, Vietravel, Saigontourist, Fiditour, Vntrip, Mytour) qua trang trợ giúp và luồng đặt. Cột: tên, có đặt cho nhóm không, có chia tiền từng người không, có link mời không, có hạn giữ chỗ không, thanh toán chuyển khoản QR không, ghi chú, ngày kiểm tra. Kết luận về "khoảng trống" chỉ dựa trên bảng này, không dựa trên phỏng đoán.

## Trước khi giao một chương

- [ ] Số liệu có nguồn, cỡ mẫu rõ.
- [ ] Giả thuyết được ghi là giả thuyết.
- [ ] Mô tả khớp code và CSDL hiện tại.
- [ ] Hình, bảng đánh số theo chương và có trong danh mục.
- [ ] Không chép nguyên văn nguồn ngoài.
