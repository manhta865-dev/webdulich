# Brief thiết kế lại giao diện TravelVN

Phạm vi: chỉ đổi cách hiển thị (markup, CSS, chữ). Không đổi controller, model, ViewBag, tên action, `id`/`name` của form và các phần tử mà JavaScript đang dùng. Chức năng đơn nhóm và thanh toán QR làm sau khi có sơ đồ; giao diện của chúng sẽ dùng lại bộ token dưới đây.

## 1. Hướng thiết kế

- Cảm giác: sáng, gọn, đáng tin, giống một công cụ đặt chỗ hơn là một tờ quảng cáo. Bỏ chất "Luxury Journeys", băng chữ chạy ưu đãi và phông chữ có chân.
- Điểm nhớ duy nhất: **ô ghế**. Mỗi chỗ trong đơn nhóm là một ô nhỏ có trạng thái (trống, đã điền, đã trả, quá hạn). Bộ ô này dùng cho trang đơn nhóm sau này, và dùng luôn cho chỗ còn trống ở thẻ tour và trang chi tiết. Các trang còn lại để yên tĩnh.
- Người dùng chính dùng điện thoại, nên thiết kế từ 360px lên.

### Bộ nhận diện "tấm vé" (đợt làm lại tháng 11/2026)

Phát triển tiếp ý "ô ghế": **mỗi ghế là một tấm vé có chủ riêng**. Thành phần dùng chung ở `wwwroot/css/ve.css`:

| Lớp | Ý nghĩa |
|---|---|
| `.tv-ve` | khung vé, bo 14px, viền mảnh |
| `.tv-ve-duc-lo` / `.tv-ve-duc-lo-doc` | đường đục lỗ ngang / dọc, hai đầu có vết khuyết tròn (`--nen-sau-ve` = màu phía sau vé) |
| `.tv-ma`, `.tv-ma-vach` | chữ máy in vé (JetBrains Mono, chỉ cho mã vé, mã ghế, mã giảm giá) và mã vạch trang trí |
| `.tv-moc` (+ `-xanh`, `-vang`, `-xam`, `-do`) | dấu mộc nghiêng: "Đã trả", "Chờ xác nhận", "Chưa trả", "Hết hiệu lực" |
| `.tv-moc-dong` | khoảnh khắc chuyển động duy nhất: mộc được đóng xuống (tắt khi người dùng chọn giảm chuyển động) |
| `[data-moc-dong]` + `js/moc-dong.js` | khối có mộc: mộc đóng lần lượt khi khối cuộn tới, một lần |

Đã áp dụng: các trang đơn nhóm (xem mục "Đơn nhóm" ở phần 3), thẻ tour (`_TheTour.cshtml`, trang chủ và danh sách tour), tấm vé lớn đầu trang chủ (vết khuyết khoét thủng bằng mask), hai tấm vé so sánh, mã giảm giá (phiếu xé), bảng vé ghế ở trang chủ (mộc đóng lần lượt khi cuộn tới). Quả địa cầu nằm ở khối "Nhóm bạn muốn đi đâu?" cạnh lưới điểm đến, hiện cả trên điện thoại. Ảnh trước và sau ở `docs/anh_giao_dien/`.

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

- Họ chính: **Be Vietnam Pro** (Google Fonts, thiết kế cho tiếng Việt, đủ dấu), các độ đậm 400, 500, 600, 700. Tải qua CSS, khai báo dự phòng `system-ui`.
- Chữ máy in vé: **JetBrains Mono** 500, 700 (`--tv-font-ve`), chỉ dùng cho mã vé, mã ghế, mã giảm giá, mã điểm đến trên cuống vé.
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
**Bản làm lại (21/11/2026)**, sắp khối theo cách của travelhappytrip.com nhưng chữ, ảnh, màu là của TravelVN và mọi câu chữ chỉ nói tính năng đã chạy thật:
1. Đầu trang nền xanh, một hàng: vé chính (dải "TRAVELVN, VÉ ĐI NHÓM", tiêu đề, 4 nhãn: giữ chỗ N giờ theo cấu hình `DonNhom:SoGioGiuCho`, link mời, QR, biết ai đã trả; ô tìm kiếm khi màn hình dưới 1200px; nút "Chọn tour", "Xem cách đặt nhóm"), cuống vé đứng riêng hiện tour đang ở giữa dải xếp quạt (rê chuột hoặc Tab vào vé thì cuống tách ra, nghiêng nhẹ), rồi tour xếp quạt (tự chuyển 5 giây, bấm thẻ bên để đưa ra giữa; rê chuột vào ảnh nào, kể cả ảnh nghiêng phía sau, thì ảnh đó trồi lên, nghiêng theo con trỏ và có vệt sáng chạy theo, giống tấm vé ở trang Ghế của tôi) và hàng nút điểm đến.
2. "6 lý do chọn TravelVN": thẻ ảnh có biểu tượng và cuống vé ghi chú (giá theo lịch, giấy tờ riêng tư, đối chiếu từng khoản, hạn chốt minh bạch, bản đồ hành trình, lịch tháng); rê chuột thì thẻ nhấc lên, cuống chuyển xanh và nghiêng. Không dùng nhãn "chứng nhận". Ảnh mỗi thẻ khớp tiêu đề (tiền và hộ chiếu, tay giữ hộ chiếu, mã QR, đồng hồ cát, điện thoại chỉ đường, tờ lịch), lấy từ Unsplash, lưu ở `wwwroot/images/ly-do/`, nguồn ghi ở `docs/NGUON_ANH.md`.
3. Hai tấm vé so sánh: kiểu cũ và trên TravelVN.
4. Chọn tour: thẻ vé, lọc Tất cả / Trong nước / Nước ngoài.
5. "Nhóm bạn muốn đi đâu?": quả địa cầu 3D trong ô nền tối bên trái, lưới điểm đến bên phải, viền sọc kiểu phong bì thư máy bay; rê chuột vào ô thì ô nhấc lên, sáng viền xanh, ảnh phóng chậm, vệt sáng lướt qua và hiện "Xem tour".
6. "Đặt nhóm trên TravelVN": 6 thẻ có mẩu giao diện thu nhỏ (đồng hồ giữ chỗ, link mời, ô điền, mã QR, lời nhắc trên web, số giấy tờ bị che).
7. Bảng 5 vé ghế đóng mộc (minh họa), mã giảm giá dạng phiếu xé.
8. "Nhóm muốn đi ngày khác?": nút Lịch khởi hành và Zalo (thay khối tour riêng, visa của trang tham khảo vì TravelVN không có dịch vụ đó).
9. Video, bài viết mới, hỏi đáp nhanh (cùng câu trả lời với trang Giới thiệu).

Bản trước (đến 20/11/2026): đầu trang là ảnh điểm đến phủ kín màn hình, menu trong suốt nằm trên ảnh (đổi nền trắng khi cuộn xuống). Thanh tìm kiếm nổi ở giữa phía trên; góc trái dưới có tên điểm đến lớn, mô tả ngắn và nút "Xem tour"; góc phải dưới là dải thẻ điểm đến, bấm để đổi ảnh (tự chuyển sau 6 giây, dừng khi rê chuột, tắt khi người dùng chọn giảm chuyển động). Ảnh chỉ dùng loại đủ nét trong `wwwroot/images/dulich`. Bên dưới là mã giảm giá (nếu có trong dữ liệu), tour trong nước và nước ngoài dạng thẻ ảnh dọc (tên, số ngày, giá chồng lên ảnh), rồi một dải ảnh phủ kín với ba bước đặt tour. Chân trang nền tối có địa chỉ, bản đồ Google, liên hệ và các loại thanh toán (chỉ để minh họa trong đồ án, chưa kết nối cổng thanh toán). Bỏ số liệu "100+ tour, 500+ khách", ba lời khen viết cứng, băng chữ ưu đãi và nhãn "Nổi bật". Khối "Đi theo nhóm" sẽ thêm khi chức năng đơn nhóm có thật.

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

**Đợt tấm vé (21/11/2026):** mỗi lịch khởi hành là một tấm vé nhỏ (ngày giờ, số chỗ ở phần trên; đường xé; giá và nút đặt ở phần dưới). Tour có điểm xuất phát và tọa độ mọi ngày thì dưới bản đồ hành trình có nút "Khám phá 3D toàn màn hình" và "Cả hành trình trên Google Maps"; mỗi ngày trong lịch trình có link "Chỉ đường ngày N trên Google Maps" (từ điểm ngày trước tới điểm ngày đó). Link Google Maps chỉ là đường dẫn (Maps URLs), không cần khóa API, dựng ở `Services/LienKetBanDo.cs`.

### Khám phá 3D (`DiaDiem/KhamPha/{id}`, 21/11/2026)
Trang toàn màn hình, không dùng `_Layout`. Chỉ đọc, không đổi CSDL; chỉ mở được khi tour có điểm xuất phát và tọa độ mọi ngày, nếu không thì quay về trang tour kèm thông báo.

- Đầu trang: nút quay lại tour, tên tour, ba tab **Bản đồ 3D**, **Google Maps**, **Đi dạo 360°**.
- **Bản đồ 3D** (`js/kham-pha-3d.js`, MapLibre như trang tour): địa hình nổi, nhìn nghiêng, xoay và phóng to tự do, nút Bản đồ / Vệ tinh dùng chung (`js/nen-ve-tinh.js`). Chọn một ngày ở thanh đáy thì camera bay tới đoạn đường của ngày đó và xe chạy hết đoạn. Thẻ bên trái (điện thoại: phía trên thanh đáy) ghi ngày, tiêu đề, bữa ăn, nút Google Maps.
- **Dẫn đường**: danh sách chỉ đường từng bước bằng tiếng Việt kèm tổng quãng đường và thời gian xe chạy ước tính. Máy chủ hỏi OSRM (`DiaDiem/ChiDuong?id=&ngay=`, `DuongXeChay.LayChiDanAsync`) rồi dịch các thao tác (rẽ, vòng xuyến, nhập làn...) sang tiếng Việt; kết quả nhớ tạm trong bộ nhớ 1 ngày, lỗi thì nhớ 10 phút. Ghi rõ nguồn OSRM, OpenStreetMap và khuyên mở thêm Google Maps.
- **Bay vòng**: camera xoay vòng quanh điểm đang chọn, dừng khi bấm lại hoặc khi người xem kéo bản đồ. **Toàn cảnh**: thu về cả hành trình.
- **Google Maps**: nhúng bản đồ Google ngay tại điểm đang chọn (kiểu "nhúng bản đồ" của Google, không cần khóa; chỉ tải khi mở tab), nút Chỉ đường Google Maps và Xem Street View (mở sang Google).
- **Điểm tham quan (Mo23)**: thanh đáy gồm nút từng ngày (bấm để xe chạy đoạn đường của ngày) và sau đó là chip các điểm tham quan của ngày (Hồ Xuân Hương, Ga Đà Lạt...). Mỗi điểm có ghim trắng trên bản đồ, bấm chip hoặc ghim thì camera bay sát xuống điểm (hai chặng để không chui vào địa hình), thẻ thông tin đổi sang tên điểm. Admin thêm, sửa, đổi thứ tự, gỡ điểm ở trang Lịch trình (khối "Điểm tham quan trong ngày").
- **Đi dạo 360°**: nhúng Google Street View tại điểm đang chọn (điểm tham quan, hoặc điểm cuối ngày nếu ngày chưa có điểm): kéo để nhìn quanh, bấm mũi tên trên đường để đi tới. Có khóa Maps Embed API (`GoogleMaps:KhoaNhung`, đặt bằng `dotnet user-secrets` trên máy và `appsettings.Production.json` trên host, không commit) thì dùng cách chính thức; chưa có khóa thì dùng kiểu "Chia sẻ, nhúng" của Google Maps. Luôn có nút "Mở trên Google Maps".
- Người dùng chọn giảm chuyển động thì không bay, không chạy xe, không xoay vòng.

Ảnh lớn đầu trang, bên dưới là hai cột trên máy tính: trái là mô tả, lịch trình theo ngày; phải là hộp đặt tour dính (chọn lịch khởi hành, số người, giá, nút "Đặt tour"). Điện thoại: hộp đặt thành thanh cố định ở đáy màn hình.

### Đăng nhập và đăng ký
Một cột, khung hẹp giữa trang, toàn bộ tiếng Việt. Bỏ chữ "Sign in", "Hello Friend!". Liên kết "Quên mật khẩu" hiện chưa có chức năng nên tạm ẩn cho đến khi làm.

### Đặt tour và Thanh toán
Đã làm: thanh ba bước dùng chung (`_BuocDatTour`), biểu mẫu chia khối, cột tóm tắt dính; Thanh toán có khối thông tin khách, mã khuyến mãi, chọn phương thức (tiền mặt, ACB, MoMo, hiển thị QR và nội dung chuyển khoản). Giữ nguyên toàn bộ JavaScript và id. Bản vẽ dưới đây là ý tưởng ban đầu.

Thanh tiến trình ba bước ở đầu trang: Thông tin, Thanh toán, Hoàn tất. Cột trái là biểu mẫu, cột phải là tóm tắt đơn (tour, ngày, số người, giảm giá, tổng). Giữ nguyên toàn bộ trường và `id` hiện có. Trang thanh toán bỏ quảng cáo thẻ, ví chưa hỗ trợ; luồng vẫn là giả lập cho đến khi làm QR.

### Thành công và Lịch sử
Thành công đã làm (thẻ giữa trang có dấu tích, hai nút). Lịch sử đã làm: bảng 11 cột đổi thành danh sách thẻ đơn (tên tour, mã chuyến đi, nhãn trạng thái bằng chữ kèm màu, lưới thông tin, nút hủy đơn), kèm bốn ô số liệu tổng.

Thành công: dấu tích, mã chuyến đi, tóm tắt, nút xem lịch sử. Lịch sử: danh sách đơn dạng thẻ, mỗi thẻ có nhãn trạng thái (chữ kèm màu), nút Hủy ở đúng chỗ.

**Vé của đơn (21/11/2026):** đơn đặt riêng đã thanh toán, chưa hủy có nút "Xem vé" ở Lịch sử, mở `DatTour/Ve/{id}`. Mỗi hành khách một tấm vé cùng kiểu vé ghế ở trang Ghế của tôi (lật xem mặt sau, nghiêng theo chuột, lưu ảnh PNG): tên tour, ngày, giờ, họ tên, chỗ ngồi (lịch không có sơ đồ thì in thứ tự hành khách); mặt sau có mã chuyến đi và "Hành khách 1/3". Không in số giấy tờ, ghi rõ không dùng để soát vé. Chỉ chủ đơn (theo `taiKhoanId`) xem được; đơn nhóm, đơn chưa trả hoặc đã hủy thì quay về Lịch sử kèm thông báo. Script dùng chung `wwwroot/js/ve-lat.js`: mỗi vé `[data-ve]` nằm trong khối `[data-ve-khoi]` có nút lật, nút lưu riêng.

### Đơn nhóm (21/11/2026)
Mỗi ghế là một tấm vé có chủ riêng. Chỉ đổi giao diện, controller, model, form, route và script giữ nguyên.

- **Trang tiến độ** (`DonNhom/ChiTiet`): đầu trang là vé tập thể (mã đơn, tour, ngày giờ, số ghế, số ghế đã trả); cuống vé là đồng hồ giữ chỗ, hoặc mộc "Đơn đã chốt" / "Đơn đã hủy" / "Chờ chốt". Danh sách ghế là lưới vé ghế: thân vé có số ghế, chỗ ngồi, người giữ ghế, mộc tiền ("Đã trả", "Chờ xác nhận", "Chưa trả", "Đã nhả"); cuống vé có số tiền và nút (Ghế của tôi, Điền hộ, Trả hộ, Xem, Thêm). Ghế trống viền nét đứt, ghế đã nhả mờ. Giữ thanh xe buýt tiến độ và sơ đồ xe 3D. Khối "Mời cả nhóm" có đường xé như phiếu mời.
- **Trang mời** (`DonNhom/Moi`): thư mời dạng vé, cuống là hạn nhận ghế; ghế còn nhận được viền màu nhấn, có nút "Nhận ghế này" ở cuống; ghế đã có người nhận mờ, mộc xám. Không hiện tên ai.
- **Link hết hạn**: tấm vé mộc đỏ "Hết hiệu lực".
- **Đặt tour cho nhóm**: khối Tóm tắt có đường xé giữa tour và tiền.
- **Trả hộ**: đầu trang là vé của đúng ghế (cuống ghi số ghế).
- **Khối "Phần phải trả"** (`_TraTienGhe`, dùng ở Ghế của tôi và Trả hộ): đường xé giữa số tiền và phần QR; ghế đã trả hoặc chờ xác nhận có mộc tương ứng; nội dung chuyển khoản, số tài khoản, số tiền in chữ máy in vé.
- Ảnh trước và sau: `docs/anh_giao_dien/truoc_don_nhom_*`, `sau_don_nhom_*`.

### Gợi ý tour
Đã làm: khung ảnh đầu trang, khối chọn tiêu chí (giữ nguyên form và các trường TieuChi), khối "Cách gợi ý hoạt động", kết quả dạng thẻ có thanh mức phù hợp, lý do đề xuất, nút xem chi tiết và đặt tour. Đã gỡ dải số liệu khảo sát viết cứng (xem docs/KHAO_SAT.md) và nhãn "Hot".

Biểu mẫu chọn sở thích ở trên, kết quả là lưới thẻ tour dùng chung với trang danh sách. Thay các emoji bằng icon SVG.

### Nhân viên (hướng dẫn viên, tài xế)
Danh sách lịch làm việc theo ngày, mỗi mục một dòng: tour, ngày, giờ, số khách.

### Admin
Lô E đã làm: Khuyến mãi, Tài khoản (kèm hộp thoại xóa), Sửa tài khoản, Tin nhắn (danh sách cuộc trò chuyện), Chi tiết tin nhắn (khung chat, JavaScript tải tin giữ nguyên), Nhân sự, Phân công tour, Sửa phân công và Lịch làm việc của nhân viên. Đã bỏ ký tự ```cshtml thừa ở Nhân sự và chú thích // lộ ra ở Tài khoản.
Lô D đã làm: Đơn đặt tour (bảng 7 cột, nhãn trạng thái có màu riêng cho "Đã hủy", ghi chú trang chỉ liệt kê đơn chưa thanh toán), Sửa đơn (khối thông tin, thẻ hành khách, nhãn trạng thái đổi theo ô chọn), Doanh thu (giữ Chart.js, đổi màu biểu đồ sang bộ màu mới). Bản in `BaoCaoDoanhThuPdf` đã làm lại theo bộ màu mới (A4 ngang, đầu báo cáo, ba ô tóm tắt, bảng 9 cột có dòng tổng cộng), giữ nguyên dữ liệu, nút "In / Lưu PDF" và tự mở hộp thoại in.
Lô C đã làm: Lịch đi (danh sách tour, lịch khởi hành có tab trạng thái, thêm lịch, sửa ngay trong bảng, hủy lịch) và Lịch trình (danh sách tour, thêm ngày, dòng thời gian). Nhãn trạng thái bằng chữ kèm màu; tour chưa có lịch hoặc lịch trình được đánh dấu cam. Đã bỏ ký tự ``` thừa ở cuối hai trang danh sách.
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
