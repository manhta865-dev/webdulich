using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace webdulich.Migrations
{
    /// <summary>
    /// Bước 2 của đổi mô hình dữ liệu: chỉ điền dữ liệu cho các cột và bảng mới thêm ở bước 1.
    /// Không đổi cấu trúc, không xóa dòng, không sửa cột cũ mà code hiện tại còn dùng.
    /// Mọi câu lệnh đều chạy lại được nhiều lần mà không nhân đôi dữ liệu.
    /// Xem docs/MO_HINH_DU_LIEU.md, mục 7.
    /// </summary>
    public partial class Mo2_ChuyenDuLieuMoHinh : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Tách TaiKhoanDangNhap sang Email hoặc SoDienThoai (giữ nguyên cột cũ)
            migrationBuilder.Sql(@"
UPDATE TaiKhoans
SET Email = LTRIM(RTRIM(TaiKhoanDangNhap))
WHERE Email IS NULL
  AND LTRIM(RTRIM(TaiKhoanDangNhap)) LIKE '%_@_%._%';

UPDATE TaiKhoans
SET SoDienThoai = LTRIM(RTRIM(TaiKhoanDangNhap))
WHERE SoDienThoai IS NULL
  AND LTRIM(RTRIM(TaiKhoanDangNhap)) LIKE '0[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]';");

            // 2. Số điện thoại nhân viên chuyển sang tài khoản, chỉ khi tài khoản chưa có số
            //    và số đó không trùng với tài khoản hay nhân viên khác
            migrationBuilder.Sql(@"
UPDATE t
SET SoDienThoai = LTRIM(RTRIM(n.SoDienThoai))
FROM TaiKhoans t
JOIN NhanViens n ON n.TaiKhoanId = t.Id
WHERE t.SoDienThoai IS NULL
  AND NULLIF(LTRIM(RTRIM(n.SoDienThoai)), '') IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM TaiKhoans x WHERE x.SoDienThoai = LTRIM(RTRIM(n.SoDienThoai)))
  AND LTRIM(RTRIM(n.SoDienThoai)) NOT IN (
        SELECT LTRIM(RTRIM(SoDienThoai)) FROM NhanViens
        GROUP BY LTRIM(RTRIM(SoDienThoai)) HAVING COUNT(*) > 1);");

            // 3. Ngày đặt lấy từ mã chuyến đi cũ dạng TRAVELVN-{tour}-{yyyyMMddHHmmss}
            migrationBuilder.Sql(@"
UPDATE DatTours
SET NgayDat = TRY_CONVERT(datetime2,
        STUFF(STUFF(STUFF(STUFF(STUFF(RIGHT(MaChuyenDi, 14), 13, 0, ':'), 11, 0, ':'), 9, 0, ' '), 7, 0, '-'), 5, 0, '-'))
WHERE MaChuyenDi LIKE 'TRAVELVN-%'
  AND RIGHT(MaChuyenDi, 14) NOT LIKE '%[^0-9]%'
  AND TRY_CONVERT(datetime2,
        STUFF(STUFF(STUFF(STUFF(STUFF(RIGHT(MaChuyenDi, 14), 13, 0, ':'), 11, 0, ':'), 9, 0, ' '), 7, 0, '-'), 5, 0, '-')) IS NOT NULL;");

            // 4. Trạng thái đơn theo trạng thái thanh toán cũ
            migrationBuilder.Sql(@"
UPDATE DatTours
SET TrangThai = CASE
        WHEN TrangThaiThanhToan = N'Đã hủy' THEN 'DaHuy'
        WHEN TrangThaiThanhToan = N'Đã thanh toán' THEN 'DaXacNhan'
        ELSE 'GiuCho'
    END;");

            // 5. Đơn khai SoNguoi nhiều hơn số dòng hành khách: tạo thêm ghế trống cho đủ
            migrationBuilder.Sql(@"
;WITH So AS (
    SELECT a.n * 10 + b.n + 1 AS k
    FROM (VALUES (0),(1),(2),(3),(4),(5),(6),(7),(8),(9)) a(n)
    CROSS JOIN (VALUES (0),(1),(2),(3),(4),(5),(6),(7),(8),(9)) b(n)
),
Thieu AS (
    SELECT d.Id, d.SoNguoi - COUNT(h.Id) AS SoThieu
    FROM DatTours d
    LEFT JOIN HanhKhachDatTours h ON h.DatTourId = d.Id
    GROUP BY d.Id, d.SoNguoi
    HAVING d.SoNguoi > COUNT(h.Id)
)
INSERT INTO HanhKhachDatTours (DatTourId, HoTen, LoaiGiayTo, SoGiayTo, LoaiHanhKhach, GiaLucDat)
SELECT t.Id, N'', N'', N'', N'Người lớn', 0
FROM Thieu t
JOIN So ON So.k <= t.SoThieu;");

            // 6. Giá lúc đặt: chia đều tổng trước giảm cho các ghế của đơn, phần dư dồn vào ghế đầu
            //    để tổng khớp đúng. Chỉ làm với đơn chưa có giá nào.
            migrationBuilder.Sql(@"
;WITH Don AS (
    SELECT d.Id,
           CASE WHEN d.TongTienTruocGiam > 0 THEN d.TongTienTruocGiam ELSE d.TongTien + d.SoTienGiam END AS Tong,
           COUNT(h.Id) AS SoGhe,
           MIN(h.Id) AS GheDau
    FROM DatTours d
    JOIN HanhKhachDatTours h ON h.DatTourId = d.Id
    GROUP BY d.Id, d.TongTienTruocGiam, d.TongTien, d.SoTienGiam
    HAVING SUM(h.GiaLucDat) = 0
)
UPDATE h
SET GiaLucDat = ROUND(Don.Tong / Don.SoGhe, 0, 1)
              + CASE WHEN h.Id = Don.GheDau THEN Don.Tong - ROUND(Don.Tong / Don.SoGhe, 0, 1) * Don.SoGhe ELSE 0 END
FROM HanhKhachDatTours h
JOIN Don ON Don.Id = h.DatTourId;");

            // 7. Ghế của đơn đã hủy coi như đã nhả
            migrationBuilder.Sql(@"
UPDATE h
SET NgayNha = d.NgayDat
FROM HanhKhachDatTours h
JOIN DatTours d ON d.Id = h.DatTourId
WHERE d.TrangThai = 'DaHuy' AND h.NgayNha IS NULL;");

            // 8. Sức chứa của lịch = chỗ còn + số người của các đơn chưa hủy
            migrationBuilder.Sql(@"
UPDATE l
SET SoChoToiDa = l.SoChoCon + ISNULL((
        SELECT SUM(d.SoNguoi) FROM DatTours d
        WHERE d.LichKhoiHanhTourId = l.Id AND d.TrangThai <> 'DaHuy'), 0)
FROM LichKhoiHanhTours l
WHERE l.SoChoToiDa = 0;");

            // 9. Giao dịch cho đơn đã thanh toán (ThanhCong) và đơn đang chờ kiểm tra (ChoXacNhan)
            migrationBuilder.Sql(@"
INSERT INTO GiaoDichThanhToans (DatTourId, HanhKhachDatTourId, NguoiXacNhanId, SoTien, PhuongThuc, TrangThai, MaThamChieuNganHang, NgayTao, NgayXacNhan)
SELECT d.Id, NULL, NULL, d.TongTien, LEFT(d.PhuongThucThanhToan, 50),
       CASE WHEN d.TrangThaiThanhToan = N'Đã thanh toán' THEN 'ThanhCong' ELSE 'ChoXacNhan' END,
       NULL, d.NgayDat,
       CASE WHEN d.TrangThaiThanhToan = N'Đã thanh toán' THEN d.NgayDat ELSE NULL END
FROM DatTours d
WHERE d.TrangThaiThanhToan IN (N'Đã thanh toán', N'Đang kiểm tra')
  AND d.TongTien > 0
  AND NOT EXISTS (SELECT 1 FROM GiaoDichThanhToans g WHERE g.DatTourId = d.Id);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Bước này chỉ điền dữ liệu vào cột và bảng mới. Muốn quay lại thì gỡ migration bước 1
            // (xóa luôn các cột, bảng đó) hoặc khôi phục bản sao lưu. Riêng ghế trống do bước 5 tạo
            // thì xóa thủ công nếu cần.
        }
    }
}
