-- Kiểm tra trên DB host TRƯỚC khi chạy Mo28_DongBoCotLienHeVaTour.sql. Chỉ đọc, không sửa gì, chạy bao nhiêu lần cũng được.
-- Mo28 đổi 4 cột thành bắt buộc: DatTours.TenKhach nvarchar(100), DatTours.DienThoai nvarchar(20),
-- Tours.MoTa nvarchar(max), Tours.HinhAnh nvarchar(max).
--
-- Đọc kết quả:
--   (a) Mo27 phải "da chay" và Mo28 "CHUA CHAY". Mo28 đã chạy thì không cần chạy lại.
--   (b) Kiểu hiện tại của 4 cột trên host (để biết host có lệch kiểu với máy không).
--   (c) Các cột SoDongRong, SoDongQuaDai phải bằng 0 hết. Có số khác 0 thì DỪNG: xem (d), sửa dữ liệu
--       các đơn, tour đó trên trang quản trị (điền tên, số điện thoại, mô tả, ảnh) rồi chạy lại file này.
--   (d) Danh sách đơn, tour cụ thể đang rỗng hoặc quá dài (không có dòng nào là tốt).
SET NOCOUNT ON;

-- (a) Script đã chạy hay chưa
SELECT ds.TepScript,
       CASE WHEN h.MigrationId IS NULL THEN 'CHUA CHAY' ELSE 'da chay' END AS TinhTrang
FROM (VALUES
    (1, N'20261126112331_Mo27_XoaKhoaNgoaiTrungTours', N'Mo27_XoaKhoaNgoaiTrungTours.sql'),
    (2, N'20261126115805_Mo28_DongBoCotLienHeVaTour',  N'Mo28_DongBoCotLienHeVaTour.sql')
) AS ds(ThuTu, MigrationId, TepScript)
LEFT JOIN [__EFMigrationsHistory] h ON h.MigrationId = ds.MigrationId
ORDER BY ds.ThuTu;

-- (b) Kiểu hiện tại của 4 cột (-1 nghĩa là max)
SELECT TABLE_NAME AS Bang, COLUMN_NAME AS Cot, DATA_TYPE AS Kieu,
       CHARACTER_MAXIMUM_LENGTH AS DoDai, IS_NULLABLE AS ChoRong
FROM INFORMATION_SCHEMA.COLUMNS
WHERE (TABLE_NAME = 'DatTours' AND COLUMN_NAME IN ('TenKhach', 'DienThoai'))
   OR (TABLE_NAME = 'Tours' AND COLUMN_NAME IN ('MoTa', 'HinhAnh'))
ORDER BY TABLE_NAME, COLUMN_NAME;

-- (c) Số dòng sẽ làm Mo28 thất bại
SELECT N'DatTours.TenKhach' AS Cot,
       SUM(CASE WHEN TenKhach IS NULL THEN 1 ELSE 0 END) AS SoDongRong,
       SUM(CASE WHEN LEN(TenKhach) > 100 THEN 1 ELSE 0 END) AS SoDongQuaDai
FROM DatTours
UNION ALL
SELECT N'DatTours.DienThoai',
       SUM(CASE WHEN DienThoai IS NULL THEN 1 ELSE 0 END),
       SUM(CASE WHEN LEN(DienThoai) > 20 THEN 1 ELSE 0 END)
FROM DatTours
UNION ALL
SELECT N'Tours.MoTa', SUM(CASE WHEN MoTa IS NULL THEN 1 ELSE 0 END), 0 FROM Tours
UNION ALL
SELECT N'Tours.HinhAnh', SUM(CASE WHEN HinhAnh IS NULL THEN 1 ELSE 0 END), 0 FROM Tours;

-- (d) Đơn, tour cụ thể cần sửa trước (không có dòng nào là tốt)
SELECT N'DatTours' AS Bang, Id, MaChuyenDi AS Ma, TenKhach, DienThoai
FROM DatTours
WHERE TenKhach IS NULL OR DienThoai IS NULL OR LEN(TenKhach) > 100 OR LEN(DienThoai) > 20
UNION ALL
SELECT N'Tours', Id, TenTour, NULL, NULL
FROM Tours
WHERE MoTa IS NULL OR HinhAnh IS NULL;
