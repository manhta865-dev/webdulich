---
name: ui-redesign
description: Thiết kế lại toàn bộ giao diện web TravelVN (Razor view, CSS, layout) cho đẹp, nhất quán và không mang dấu hiệu giao diện làm bằng AI: lập brief và token thiết kế, wireframe, xây CSS dùng chung, bỏ emoji làm icon, viết lại nội dung tiếng Việt, responsive ưu tiên điện thoại. Dùng khi người dùng nhắc giao diện, UI, thiết kế lại, đẹp hơn, CSS, layout, trang chủ, hoặc sửa bất kỳ file .cshtml nào về mặt hiển thị.
---

# Thiết kế lại giao diện TravelVN

Chỉ đổi **cách hiển thị** (markup, CSS, nội dung chữ). Không đổi logic controller, tên model, tên ViewBag, tên action. Nếu một thay đổi giao diện cần đổi logic, dừng lại và báo theo skill `safe-change`.

## Người dùng và việc chính của giao diện

- Người dùng chính: nhóm bạn, lớp, gia đình đặt tour cho cả nhóm, phần lớn dùng điện thoại, nói tiếng Việt.
- Việc chính của giao diện: giúp chọn tour nhanh, và với đơn nhóm thì cho thấy ngay ai đã điền, ai đã trả, còn bao lâu.
- Điểm nhớ duy nhất của thiết kế nên nằm ở **đơn nhóm** (sơ đồ ghế và tiến độ). Các trang còn lại giữ yên tĩnh, rõ ràng.

## Hiện trạng cần sửa (từ khảo sát trang đang chạy)

- Icon là emoji (✈️, 🤖, 🎁, ❌, 💳, 📞), tên thương hiệu "Luxury Journeys", băng chữ chạy ưu đãi.
- Trang đăng nhập lẫn tiếng Anh ("Sign in to Website", "SIGN IN", "Hello Friend!").
- Mọi tour đều gắn nhãn "Nổi bật"; thẻ tour giống hệt nhau, tiêu đề trang đều là "Travel Website".
- Số liệu và lời khen viết cứng; ảnh lấy trực tiếp từ website khác; liên kết trỏ về `#`.
- Quảng cáo phương thức thanh toán chưa có thật.

## Tránh các dấu hiệu "giao diện làm bằng AI"

Đây là những lựa chọn mặc định xuất hiện bất kể chủ đề. Chỉ dùng khi có lý do cụ thể của riêng TravelVN:
- Nền kem ấm kèm chữ serif tương phản cao và màu nhấn đất nung; hoặc nền gần đen với một màu nhấn chói.
- Mọi nội dung bị chia thành thẻ bo góc giống nhau, cùng một bóng đổ xám nhạt, nền chuyển màu làm trang trí.
- Nhãn viết hoa giãn chữ phía trên mỗi tiêu đề, chuỗi chú thích nối bằng dấu chấm giữa, nhãn dạng "TỪ — mảnh ghép".
- Tô màu hoặc in nghiêng đúng một từ trong tiêu đề; đánh số 01 / 02 / 03 cho trang trí.
- Chuyển động xuất hiện trên mọi khối và mọi lần di chuột.
- Emoji làm biểu tượng.

## Quy trình

1. **Chọn hướng và lập token (làm trước, đưa người dùng duyệt).** Ghi một bảng gọn:
   - Màu: 4 đến 6 màu có tên và mã hex (nền, chữ, nhấn, trạng thái thành công, cảnh báo, lỗi).
   - Chữ: một hoặc hai họ phông có vai trò rõ, hỗ trợ tiếng Việt đầy đủ dấu; thang cỡ chữ; độ dài dòng dưới 80 ký tự.
   - Khoảng cách, bo góc (có phân cấp, không dùng một giá trị cho mọi thứ), bóng đổ.
   - Bố cục: mô tả một câu mỗi trang kèm wireframe ASCII.
   - Soát lại: nếu đoạn nào giống mặc định của mọi trang du lịch hay của danh sách "dấu hiệu AI" ở trên thì chọn lại.
2. **Cài token thành biến CSS** trong một file dùng chung (ví dụ `wwwroot/css/tokens.css`) rồi dựng `_Layout.cshtml` và các partial (header, footer, thẻ tour, thông báo). Không viết style inline. Cẩn thận độ ưu tiên selector để các lớp không triệt tiêu nhau.
3. **Làm từng trang một**, mỗi trang một thay đổi riêng, build và chạy thử rồi mới sang trang sau. Thứ tự: `_Layout`, Trang chủ, Danh sách tour, Chi tiết tour, Đăng nhập và đăng ký, Đặt tour, Thanh toán, Lịch sử, Trang nhân viên, các trang đơn nhóm mới, trang quản trị.
4. **Biểu tượng:** dùng bộ icon SVG, ưu tiên lưu sẵn trong `wwwroot` để không phụ thuộc mạng. Không dùng emoji.
5. **Nội dung chữ** (xem bên dưới), thay mọi số liệu viết cứng bằng dữ liệu thật hoặc bỏ.
6. **Ảnh:** tự lưu trong `wwwroot/images`, nén, thêm `alt` và `loading="lazy"`. Không lấy trực tiếp từ website khác.
7. **Đặt tiêu đề riêng cho từng trang** (`ViewData["Title"]`).

## Viết nội dung giao diện

- Viết từ góc nhìn người dùng, dùng từ họ hiểu. Gọi theo việc họ làm, không gọi theo cách hệ thống chạy.
- Động từ chủ động, chữ thường theo câu. Nút nói đúng điều sẽ xảy ra ("Lưu thay đổi", "Gửi link mời"), không dùng "Submit". Cùng một hành động giữ cùng một tên trong cả luồng.
- Lỗi: nói chuyện gì xảy ra và cách sửa, không xin lỗi chung chung, không mơ hồ. Trạng thái trống: chỉ hướng làm tiếp.
- Mỗi dòng chữ làm đúng một việc. Không lời quảng cáo rỗng.

## Chất lượng tối thiểu

- Responsive từ điện thoại lên máy tính, kiểm tra ở chiều rộng khoảng 360px.
- Độ tương phản chữ đủ đọc, trạng thái focus nhìn thấy được, thao tác bằng bàn phím được, có thể bấm bằng ngón tay (vùng bấm đủ lớn).
- Không phát sinh cuộn ngang. Trang tải nhanh (ảnh nén, không thư viện thừa).
- Chuyển động ít và có chủ đích: một khoảnh khắc nổi bật hơn nhiều hiệu ứng rải rác.

## Bàn giao mỗi trang

- Ảnh chụp trước và sau (dùng cho chương 4 của báo cáo).
- Danh sách file đã đổi, xác nhận không đổi logic.
- Checklist test tay: trang tải được, nút và liên kết đúng, hiển thị đúng trên điện thoại, không lỗi console.
