BEGIN TRANSACTION;
GO

UPDATE TaiKhoans
SET Email = LTRIM(RTRIM(TaiKhoanDangNhap))
WHERE Email IS NULL
  AND LTRIM(RTRIM(TaiKhoanDangNhap)) LIKE '%_@_%._%';
UPDATE TaiKhoans
SET SoDienThoai = LTRIM(RTRIM(TaiKhoanDangNhap))
WHERE SoDienThoai IS NULL
  AND LTRIM(RTRIM(TaiKhoanDangNhap)) LIKE '0[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]';
GO

UPDATE t
SET SoDienThoai = LTRIM(RTRIM(n.SoDienThoai))
FROM TaiKhoans t
JOIN NhanViens n ON n.TaiKhoanId = t.Id
WHERE t.SoDienThoai IS NULL
  AND NULLIF(LTRIM(RTRIM(n.SoDienThoai)), '') IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM TaiKhoans x WHERE x.SoDienThoai = LTRIM(RTRIM(n.SoDienThoai)))
  AND LTRIM(RTRIM(n.SoDienThoai)) NOT IN (
        SELECT LTRIM(RTRIM(SoDienThoai)) FROM NhanViens
        GROUP BY LTRIM(RTRIM(SoDienThoai)) HAVING COUNT(*) > 1);
GO

UPDATE DatTours
SET NgayDat = TRY_CONVERT(datetime2,
        STUFF(STUFF(STUFF(STUFF(STUFF(RIGHT(MaChuyenDi, 14), 13, 0, ':'), 11, 0, ':'), 9, 0, ' '), 7, 0, '-'), 5, 0, '-'))
WHERE MaChuyenDi LIKE 'TRAVELVN-%'
  AND RIGHT(MaChuyenDi, 14) NOT LIKE '%[^0-9]%'
  AND TRY_CONVERT(datetime2,
        STUFF(STUFF(STUFF(STUFF(STUFF(RIGHT(MaChuyenDi, 14), 13, 0, ':'), 11, 0, ':'), 9, 0, ' '), 7, 0, '-'), 5, 0, '-')) IS NOT NULL;
GO

UPDATE DatTours
SET TrangThai = CASE
        WHEN TrangThaiThanhToan = N'Đã hủy' THEN 'DaHuy'
        WHEN TrangThaiThanhToan = N'Đã thanh toán' THEN 'DaXacNhan'
        ELSE 'GiuCho'
    END;
GO

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
JOIN So ON So.k <= t.SoThieu;
GO

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
JOIN Don ON Don.Id = h.DatTourId;
GO

UPDATE h
SET NgayNha = d.NgayDat
FROM HanhKhachDatTours h
JOIN DatTours d ON d.Id = h.DatTourId
WHERE d.TrangThai = 'DaHuy' AND h.NgayNha IS NULL;
GO

UPDATE l
SET SoChoToiDa = l.SoChoCon + ISNULL((
        SELECT SUM(d.SoNguoi) FROM DatTours d
        WHERE d.LichKhoiHanhTourId = l.Id AND d.TrangThai <> 'DaHuy'), 0)
FROM LichKhoiHanhTours l
WHERE l.SoChoToiDa = 0;
GO

INSERT INTO GiaoDichThanhToans (DatTourId, HanhKhachDatTourId, NguoiXacNhanId, SoTien, PhuongThuc, TrangThai, MaThamChieuNganHang, NgayTao, NgayXacNhan)
SELECT d.Id, NULL, NULL, d.TongTien, LEFT(d.PhuongThucThanhToan, 50),
       CASE WHEN d.TrangThaiThanhToan = N'Đã thanh toán' THEN 'ThanhCong' ELSE 'ChoXacNhan' END,
       NULL, d.NgayDat,
       CASE WHEN d.TrangThaiThanhToan = N'Đã thanh toán' THEN d.NgayDat ELSE NULL END
FROM DatTours d
WHERE d.TrangThaiThanhToan IN (N'Đã thanh toán', N'Đang kiểm tra')
  AND d.TongTien > 0
  AND NOT EXISTS (SELECT 1 FROM GiaoDichThanhToans g WHERE g.DatTourId = d.Id);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261027153449_Mo2_ChuyenDuLieuMoHinh', N'8.0.0');
GO

COMMIT;
GO

