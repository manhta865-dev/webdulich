-- CHỈ CHẠY TRÊN DB LOCAL WebDuLich. KHÔNG CHẠY TRÊN HOST.
-- Xóa 7 đơn thử cũ (Id 17, 19, 20, 22, 24, 25, 26) theo quyết định ngày 27/10/2026,
-- trả lại chỗ cho lịch và lượt dùng cho khuyến mãi. Chạy TRƯỚC migration Mo2.
-- Sao lưu trước: WebDuLich_truoc_mo_hinh_20261027.bak
SET XACT_ABORT ON;
BEGIN TRANSACTION;

DECLARE @Don TABLE (Id int PRIMARY KEY);
INSERT INTO @Don VALUES (17), (19), (20), (22), (24), (25), (26);

-- Dừng nếu danh sách không đúng 7 đơn còn tồn tại
IF (SELECT COUNT(*) FROM DatTours WHERE Id IN (SELECT Id FROM @Don)) <> 7
BEGIN
    RAISERROR(N'Không đủ 7 đơn cần xóa, dừng lại.', 16, 1);
    ROLLBACK;
    RETURN;
END

-- Trả chỗ cho lịch (đơn đã hủy thì chỗ đã được trả trước đó)
UPDATE l
SET SoChoCon = l.SoChoCon + x.SoNguoi
FROM LichKhoiHanhTours l
JOIN (
    SELECT LichKhoiHanhTourId, SUM(SoNguoi) AS SoNguoi
    FROM DatTours
    WHERE Id IN (SELECT Id FROM @Don)
      AND LichKhoiHanhTourId IS NOT NULL
      AND TrangThaiThanhToan <> N'Đã hủy'
    GROUP BY LichKhoiHanhTourId
) x ON x.LichKhoiHanhTourId = l.Id;

-- Trả lượt dùng khuyến mãi
UPDATE k
SET SoLuotDaDung = CASE WHEN k.SoLuotDaDung >= x.SoDon THEN k.SoLuotDaDung - x.SoDon ELSE 0 END
FROM KhuyenMais k
JOIN (
    SELECT KhuyenMaiId, COUNT(*) AS SoDon
    FROM DatTours
    WHERE Id IN (SELECT Id FROM @Don) AND KhuyenMaiId IS NOT NULL
    GROUP BY KhuyenMaiId
) x ON x.KhuyenMaiId = k.Id;

DELETE FROM HanhKhachDatTours WHERE DatTourId IN (SELECT Id FROM @Don);
DELETE FROM DatTours WHERE Id IN (SELECT Id FROM @Don);

COMMIT;
