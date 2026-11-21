# Nguồn ảnh lấy từ bên ngoài

Ảnh tải về và lưu trong `webdulich/wwwroot/images`, không nhúng trực tiếp từ website khác. Mỗi ảnh ghi rõ tác giả, trang gốc và giấy phép.

## Unsplash

Giấy phép Unsplash (https://unsplash.com/license): được dùng miễn phí cho mục đích thương mại và phi thương mại, không cần xin phép, không bắt buộc ghi nguồn (vẫn ghi ở đây để minh bạch). Không được bán lại nguyên ảnh hay gom ảnh để dựng dịch vụ ảnh tương tự Unsplash.

Tải ngày 21/11/2026, khổ 1200 × 525 (Unsplash cắt sẵn), dùng ở khối "6 lý do chọn TravelVN" trên trang chủ (`Views/TrangChu/Index.cshtml`). Đã kiểm trên trang gốc: ghi "Free to use under the Unsplash License", không phải ảnh Unsplash+.

| Tệp (`wwwroot/images/ly-do/`) | Thẻ | Tác giả | Trang gốc |
|---|---|---|---|
| `ly-do-gia.jpg` | Giá rõ ràng theo từng lịch | Frugal Flyer | https://unsplash.com/photos/calendar-JzoQctzaPTA |
| `ly-do-giay-to.jpg` | Giấy tờ riêng tư | Alexander Nrjwolf | https://unsplash.com/photos/a-person-holding-a-red-passport-in-their-hand-X0Osf55s5yk |
| `ly-do-doi-chieu.jpg` | Đối chiếu từng khoản | Markus Winkler | https://unsplash.com/photos/black-android-smartphone-displaying-qr-code-Q6uqw_Hjye8 |
| `ly-do-han-chot.jpg` | Hạn chốt minh bạch | Nathan Dumlao | https://unsplash.com/photos/clear-hour-glass-5Hl5reICevY |
| `ly-do-ban-do.jpg` | Xem trước hành trình trên bản đồ | Tamas Tuzes-Katai | https://unsplash.com/photos/hand-holding-smartphone-with-navigation-map-rEn-AdBr3Ig |
| `ly-do-lich.jpg` | Chọn ngày theo lịch tháng | Towfiqu barbhuiya | https://unsplash.com/photos/a-calendar-with-red-push-buttons-pinned-to-it-bwOAixLG0uc |

## Dịch vụ bản đồ dùng ở trang Khám phá 3D

- Chỉ đường từng bước: máy chủ OSRM công khai (router.project-osrm.org), dữ liệu OpenStreetMap. Máy chủ này dành cho thử nghiệm, không cam kết ổn định; khi chạy thật nhiều người nên tự dựng OSRM hoặc dùng dịch vụ trả phí. Ghi nguồn ngay dưới danh sách chỉ đường.
- Google Maps: chỉ dùng đường link (Maps URLs) và khung nhúng của Google (bản đồ ở tab Google Maps, Street View ở tab Đi dạo 360°). Không tải ảnh Google về lưu; ảnh 360 hiện trong khung của Google, đúng như khi nhúng từ nút "Chia sẻ, nhúng" của Google Maps.
- Khung nhúng hiện dùng **không cần khóa** (địa chỉ `maps.google.com/maps?...&output=embed` và `output=svembed`, Google tự chuyển sang trang `/maps/embed`). Đây là cách Google không công bố tài liệu, có thể đổi bất cứ lúc nào. Cách chính thức là **Maps Embed API** (miễn phí, không giới hạn, nhưng cần khóa và tài khoản thanh toán Google Cloud): code đã hỗ trợ sẵn, chỉ cần đặt khóa vào cấu hình `GoogleMaps:KhoaNhung` thì tab 360 tự chuyển sang cách chính thức. Khóa phải giới hạn theo trang web (localhost và travelvn.runasp.net) và chỉ cho Maps Embed API, vì khóa nhúng luôn lộ trong mã trang. Điều khoản Google Maps chưa được đọc kỹ, cần xem lại trước khi chạy thương mại.

## Ảnh nền vệ tinh trên bản đồ

Nền "Vệ tinh" của bản đồ hành trình lấy ảnh Esri World Imagery qua mạng (không lưu về máy), có dòng ghi nguồn hiển thị ngay trên bản đồ. Điều khoản sử dụng đầy đủ của Esri chưa được kiểm, cần xem lại trước khi chạy thương mại.
