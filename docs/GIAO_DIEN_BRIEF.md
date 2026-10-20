# Brief thiết kế lại giao diện TravelVN

Phạm vi: chỉ đổi cách hiển thị (markup, CSS, chữ). Không đổi controller, model, ViewBag, tên action, `id`/`name` của form và các phần tử mà JavaScript đang dùng. Chức năng đơn nhóm và thanh toán QR làm sau khi có sơ đồ; giao diện của chúng sẽ dùng lại bộ token dưới đây.

## 1. Hướng thiết kế

- Cảm giác: sáng, gọn, đáng tin, giống một công cụ đặt chỗ hơn là một tờ quảng cáo. Bỏ chất "Luxury Journeys", băng chữ chạy ưu đãi và phông chữ có chân.
- Điểm nhớ duy nhất: **ô ghế**. Mỗi chỗ trong đơn nhóm là một ô nhỏ có trạng thái (trống, đã điền, đã trả, quá hạn). Bộ ô này dùng cho trang đơn nhóm sau này, và dùng luôn cho chỗ còn trống ở thẻ tour và trang chi tiết. Các trang còn lại để yên tĩnh.
- Người dùng chính dùng điện thoại, nên thiết kế từ 360px lên.

## 2. Token

### Màu

| Tên | Hex | Dùng cho |
|---|---|---|
| nen | `#F5F7F6` | nền trang |
| be-mat | `#FFFFFF` | nền khối, ô nhập |
| chu | `#17262E` | chữ chính |
| chu-phu | `#5B6B73` | chữ phụ, nhãn |
| nhan | `#0A6E8A` | nút chính, liên kết, trạng thái đang chọn |
| diem-nhan-phu | `#E4572E` | giá, nhãn giảm giá, chỉ dùng ít |
| thanh-cong | `#1E8A5A` | đã trả, đã xác nhận |
| canh-bao | `#B7791F` | chờ thanh toán, sắp hết hạn |
| loi | `#C0392B` | lỗi, đã hủy, quá hạn |
| vien | `#DCE3E6` | đường viền, kẻ ngang |

Quy tắc: nền xanh nhấn chỉ dùng cho nút chính và liên kết; cam chỉ dùng cho giá. Mỗi trạng thái luôn kèm chữ hoặc icon, không chỉ phân biệt bằng màu. Phải kiểm tra độ tương phản chữ trên nền (tối thiểu 4.5:1).

### Chữ

- Một họ duy nhất: **Be Vietnam Pro** (Google Fonts, thiết kế cho tiếng Việt, đủ dấu), các độ đậm 400, 500, 600, 700. Tải qua CSS, khai báo dự phòng `system-ui`.
- Thang cỡ chữ (rem): 0.8125, 0.9375 (chữ thường), 1.125, 1.375, 1.75, 2.25.
- Độ dài dòng văn bản tối đa 70 ký tự. Tiêu đề không viết hoa toàn bộ, không giãn chữ.

### Khoảng cách, bo góc, bóng

- Khoảng cách theo bội của 4px: 4, 8, 12, 16, 24, 32, 48, 64.
- Bo góc có phân cấp: ô nhập và nút 8px; khối nội dung 12px; ảnh lớn 16px; nhãn nhỏ 999px. Không dùng một giá trị cho mọi thứ.
- Bóng: hai mức. Khối thường dùng viền, không bóng. Chỉ khối nổi (menu thả, hộp thoại) có bóng nhẹ.
- Chuyển động: chỉ phản hồi bấm và mở/đóng menu (150 đến 200ms). Không hiệu ứng rải rác.

### Biểu tượng và ảnh

- Icon SVG nét 24px tự vẽ theo kiểu đường nét đơn giản, lưu dạng sprite trong `Views/Shared/_Icons.cshtml` (dùng `<svg class="tv-icon"><use href="#i-ten" /></svg>`, không phụ thuộc mạng). Không emoji.
- Ảnh tự lưu trong `wwwroot/images`, nén, có `alt` và `loading="lazy"`. 4 ảnh đang lấy từ web khác sẽ thay bằng ảnh có sẵn hoặc ảnh bạn cung cấp. Mã QR VietQR (`img.vietqr.io`) là dịch vụ tạo mã, không phải ảnh trang trí, giữ nguyên.

## 3. Bố cục từng trang

Ký hiệu: `[ ]` là nút hoặc ô nhập, `( )` là nhãn nhỏ.

### Khung chung của khách

```
┌──────────────────────────────────────────────┐
│ TravelVN   Tour  Gợi ý tour  Liên hệ   [Đăng nhập] │  menu dính trên, thu thành nút ☰ ở điện thoại
├──────────────────────────────────────────────┤
│ nội dung                                     │
├──────────────────────────────────────────────┤
│ Địa chỉ + bản đồ | Khám phá | Liên hệ | Thanh toán (minh họa)
└──────────────────────────────────────────────┘
```
Widget chat giữ nguyên chức năng, đổi hình thức nút cho đồng bộ.

### Trang chủ
Đầu trang là ảnh điểm đến phủ kín màn hình, menu trong suốt nằm trên ảnh (đổi nền trắng khi cuộn xuống). Thanh tìm kiếm nổi ở giữa phía trên; góc trái dưới có tên điểm đến lớn, mô tả ngắn và nút "Xem tour"; góc phải dưới là dải thẻ điểm đến, bấm để đổi ảnh (tự chuyển sau 6 giây, dừng khi rê chuột, tắt khi người dùng chọn giảm chuyển động). Ảnh chỉ dùng loại đủ nét trong `wwwroot/images/dulich`. Bên dưới là mã giảm giá (nếu có trong dữ liệu), tour trong nước và nước ngoài dạng thẻ ảnh dọc (tên, số ngày, giá chồng lên ảnh), rồi một dải ảnh phủ kín với ba bước đặt tour. Chân trang nền tối có địa chỉ, bản đồ Google, liên hệ và các loại thanh toán (chỉ để minh họa trong đồ án, chưa kết nối cổng thanh toán). Bỏ số liệu "100+ tour, 500+ khách", ba lời khen viết cứng, băng chữ ưu đãi và nhãn "Nổi bật". Khối "Đi theo nhóm" sẽ thêm khi chức năng đơn nhóm có thật.

### Danh sách tour
Đã làm: khung ảnh bo góc có tiêu đề và thanh tìm kiếm, hàng chip lọc theo địa hình (Tất cả, Biển, Núi, Đồng bằng, Thành phố), lưới thẻ tour ảnh dọc dùng chung `Views/Shared/_TheTour.cshtml` với trang chủ. Bản vẽ dưới đây là ý tưởng ban đầu.

```
[Tìm tên/điểm đến] [Loại ▾] [Giá ▾]      12 tour
┌────────┐ ┌────────┐ ┌────────┐
│ ảnh    │ │ ảnh    │ │ ảnh    │
│ Tên    │ │        │ │        │
│ 3N2Đ   │ │        │ │        │
│ ■■■□□  │ │        │ │        │  (ô ghế còn trống của lịch gần nhất)
│ 2,9 tr │ │        │ │        │
└────────┘ └────────┘ └────────┘
```
Điện thoại: một cột. Bộ lọc thu vào một hàng ngang cuộn được.

### Chi tiết tour
Đã làm: ảnh lớn bo góc kèm dải ảnh nhỏ (bấm để đổi ảnh), cột thông tin (loại tour, tên, giá, mô tả, bảng địa điểm - thời gian - địa hình - phong cách). Bên dưới là lịch trình dạng mở/đóng từng ngày và khung "Chọn ngày khởi hành" nằm bên phải (dính khi cuộn, xếp dưới lịch trình ở điện thoại), có giờ đi và số chỗ còn lại lấy từ dữ liệu. Đã bỏ các câu viết cứng không có trong dữ liệu (khách sạn 3 sao, hướng dẫn viên, ưu đãi, nhãn nổi bật). Bản vẽ dưới đây là ý tưởng ban đầu.

Ảnh lớn đầu trang, bên dưới là hai cột trên máy tính: trái là mô tả, lịch trình theo ngày; phải là hộp đặt tour dính (chọn lịch khởi hành, số người, giá, nút "Đặt tour"). Điện thoại: hộp đặt thành thanh cố định ở đáy màn hình.

### Đăng nhập và đăng ký
Một cột, khung hẹp giữa trang, toàn bộ tiếng Việt. Bỏ chữ "Sign in", "Hello Friend!". Liên kết "Quên mật khẩu" hiện chưa có chức năng nên tạm ẩn cho đến khi làm.

### Đặt tour và Thanh toán
Đã làm: thanh ba bước dùng chung (`_BuocDatTour`), biểu mẫu chia khối, cột tóm tắt dính; Thanh toán có khối thông tin khách, mã khuyến mãi, chọn phương thức (tiền mặt, ACB, MoMo, hiển thị QR và nội dung chuyển khoản). Giữ nguyên toàn bộ JavaScript và id. Bản vẽ dưới đây là ý tưởng ban đầu.

Thanh tiến trình ba bước ở đầu trang: Thông tin, Thanh toán, Hoàn tất. Cột trái là biểu mẫu, cột phải là tóm tắt đơn (tour, ngày, số người, giảm giá, tổng). Giữ nguyên toàn bộ trường và `id` hiện có. Trang thanh toán bỏ quảng cáo thẻ, ví chưa hỗ trợ; luồng vẫn là giả lập cho đến khi làm QR.

### Thành công và Lịch sử
Thành công đã làm (thẻ giữa trang có dấu tích, hai nút). Lịch sử đã làm: bảng 11 cột đổi thành danh sách thẻ đơn (tên tour, mã chuyến đi, nhãn trạng thái bằng chữ kèm màu, lưới thông tin, nút hủy đơn), kèm bốn ô số liệu tổng.

Thành công: dấu tích, mã chuyến đi, tóm tắt, nút xem lịch sử. Lịch sử: danh sách đơn dạng thẻ, mỗi thẻ có nhãn trạng thái (chữ kèm màu), nút Hủy ở đúng chỗ.

### Gợi ý tour
Đã làm: khung ảnh đầu trang, khối chọn tiêu chí (giữ nguyên form và các trường TieuChi), khối "Cách gợi ý hoạt động", kết quả dạng thẻ có thanh mức phù hợp, lý do đề xuất, nút xem chi tiết và đặt tour. Đã gỡ dải số liệu khảo sát viết cứng (xem docs/KHAO_SAT.md) và nhãn "Hot".

Biểu mẫu chọn sở thích ở trên, kết quả là lưới thẻ tour dùng chung với trang danh sách. Thay các emoji bằng icon SVG.

### Nhân viên (hướng dẫn viên, tài xế)
Danh sách lịch làm việc theo ngày, mỗi mục một dòng: tour, ngày, giờ, số khách.

### Admin
Lô B đã làm: Quản lý tour (danh sách dạng bảng gọn có tìm kiếm và chip lọc, Thêm tour, Sửa tour chia khối), dùng thành phần chung trong `admin.css`; `Views/Admin/Them.cshtml` là view cũ không còn action nào dùng, chưa xóa.
Lô A đã làm: trang Tổng quan (`Admin/TongQuan`, chỉ đọc dữ liệu): năm ô số liệu (doanh thu tháng, đơn chờ xác nhận, lịch 30 ngày tới, số tour, tin nhắn chưa đọc), biểu đồ cột doanh thu 12 tháng bằng SVG, danh sách đơn chờ xác nhận, lịch sắp khởi hành và tour chưa có lịch khởi hành sắp tới. Là trang đầu của admin sau đăng nhập. Các lô B đến E (tour, vận hành, đơn và tiền, còn lại) dùng chung `admin.css`.

```
┌────────┬───────────────────────────────────────┐
│ Tổng   │ Tổng quan                              │
│ quan   │ ┌────┐ ┌────┐ ┌────┐ ┌────┐            │
│ Tour   │ │Đơn │ │Doanh│ │Lịch │ │Tour │        │  4 số liệu chính
│ Lịch   │ └────┘ └────┘ └────┘ └────┘            │
│ Đơn    │ [biểu đồ doanh thu 12 tháng]  [Tour bán chạy] │
│ Tài    │ [Đơn mới nhất]  [Việc cần xử lý]       │
│ khoản  │                                        │
│ ...    │                                        │
└────────┴───────────────────────────────────────┘
```
Thanh bên thu thành ngăn kéo ở điện thoại. Các trang quản lý khác dùng chung một kiểu bảng có tìm kiếm trên đầu và nhãn trạng thái.

**Lưu ý dashboard:** các số liệu doanh thu và biểu đồ đã có sẵn ở action `DoanhThu`. Trang tổng quan mới cần thêm một action chỉ đọc để lấy thêm số đơn mới, lịch sắp khởi hành và tour thiếu lịch. Đây là thay đổi code (không đổi bảng, không ghi dữ liệu) nên sẽ xin duyệt riêng ở giai đoạn 4.

## 4. Quy ước nội dung chữ

- Nút nói đúng việc sẽ xảy ra ("Đặt tour", "Lưu thay đổi"), cùng một hành động giữ cùng một tên trong cả luồng.
- Lỗi nói chuyện gì xảy ra và cách sửa. Trạng thái trống chỉ hướng làm tiếp.
- Bỏ lời quảng cáo rỗng, số liệu viết cứng, nhãn "Nổi bật" gắn cho mọi tour.
- Mỗi trang có `ViewData["Title"]` riêng; `_Layout` hiển thị dạng "Tên trang | TravelVN".

## 5. Thứ tự áp dụng

1. Nền chung: `wwwroot/css/tokens.css`, `site.css`, chuyển CSS trong `_Layout` ra file, icon, tiêu đề trang.
2. Trang khách: Trang chủ, Danh sách tour, Chi tiết tour, Đăng nhập/đăng ký/hồ sơ, Đặt tour, Thanh toán, Thành công, Lịch sử, Gợi ý tour.
3. Trang nhân viên.
4. Admin: khung, dashboard, rồi các trang quản lý.

Mỗi trang là một commit riêng. Trước khi sửa: grep `id`, `name`, ViewBag, route mà trang dùng. Sau khi sửa: `dotnet build` sạch, chụp ảnh trước và sau, đưa checklist test tay ở 360px và máy tính, bạn thử xong mới sang trang tiếp.

## 6. Việc ngoài phạm vi, chỉ ghi nhận

- Thanh toán QR thật và đơn nhóm: làm sau khi có sơ đồ.
- `Views/TrangChu/Index1cshtml.cshtml` có vẻ là file thừa; `Privacy` còn tiêu đề tiếng Anh. Sẽ hỏi bạn trước khi xóa hay sửa.
