/*
  Chỉ chạy trên host, MỘT LẦN, ngay sau khi đưa bản "giờ Việt Nam" lên. Sao lưu trước khi chạy.

  Vì sao: bản cũ tính hạn giữ chỗ theo giờ máy chủ host, chậm hơn giờ Việt Nam 6 tiếng (suy từ mốc thời gian
  đã đối chiếu trên host). Bản mới so hạn với giờ Việt Nam, nên đơn đang giữ chỗ phải cộng thêm 6 tiếng,
  nếu không đơn sẽ hết hạn sớm 6 tiếng. Hạn mới không vượt 23:59:59 hôm trước ngày đi (luật giữ chỗ).

  Không đụng tới: đơn đã chốt hoặc đã hủy, và các mốc đã qua (ngày đặt, ngày giao dịch). Các mốc cũ này vẫn
  hiện sớm hơn thực tế 6 tiếng.

  Hoàn tác: khôi phục bản sao lưu. Chạy lần hai sẽ cộng thêm 6 tiếng nữa, nên chỉ chạy một lần.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

UPDATE d
SET HanGiuCho = CASE WHEN DATEADD(HOUR, 6, d.HanGiuCho) > m.MuonNhat THEN m.MuonNhat ELSE DATEADD(HOUR, 6, d.HanGiuCho) END
FROM DatTours d
JOIN LichKhoiHanhTours l ON l.Id = d.LichKhoiHanhTourId
CROSS APPLY (SELECT DATEADD(SECOND, -1, CAST(CAST(l.NgayKhoiHanh AS date) AS datetime2)) AS MuonNhat) m
WHERE d.LoaiDon = 'Nhom' AND d.TrangThai = 'GiuCho' AND d.HanGiuCho IS NOT NULL;

SELECT @@ROWCOUNT AS SoDonDaDoi;

-- Xem kết quả: hạn mới phải là giờ Việt Nam
SELECT d.Id, d.MaChuyenDi, d.HanGiuCho AS HanMoi, l.NgayKhoiHanh
FROM DatTours d
JOIN LichKhoiHanhTours l ON l.Id = d.LichKhoiHanhTourId
WHERE d.LoaiDon = 'Nhom' AND d.TrangThai = 'GiuCho';

COMMIT TRANSACTION;
