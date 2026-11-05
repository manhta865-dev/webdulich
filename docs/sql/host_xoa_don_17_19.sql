-- CHỈ CHẠY TRÊN DB HOST, MỘT LẦN, SAU KHI SAO LƯU, TRƯỚC Mo0 VÀ Mo1.
-- Xóa 2 đơn thử cũ Id 17 và 19 (chưa gắn lịch khởi hành, thanh toán lúc đó còn giả lập), quyết định ngày 04/11/2026.
-- Không trả chỗ cho lịch vì 2 đơn này không gắn lịch nào; lúc này host chưa có khuyến mãi.
-- Chỉ HanhKhachDatTours tham chiếu tới DatTours ở phiên bản này của host.
SET XACT_ABORT ON;
BEGIN TRANSACTION;

-- Dừng nếu không đúng 2 đơn chưa gắn lịch này
IF (SELECT COUNT(*) FROM DatTours WHERE Id IN (17, 19) AND LichKhoiHanhTourId IS NULL) <> 2
    THROW 50101, N'Không đúng 2 đơn 17 và 19 chưa gắn lịch. Dừng, chưa xóa gì.', 1;

DELETE FROM HanhKhachDatTours WHERE DatTourId IN (17, 19);
DELETE FROM DatTours WHERE Id IN (17, 19);

COMMIT;

SELECT (SELECT COUNT(*) FROM DatTours) AS TongDonConLai,
       (SELECT COUNT(*) FROM DatTours WHERE LichKhoiHanhTourId IS NULL) AS DonChuaGanLich;
