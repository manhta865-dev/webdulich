-- Truy vấn đối chiếu sau khi thu hẹp (Mo6). Chỉ đọc, chạy được trên local và host.
-- Mỗi truy vấn trả về các dòng CÓ VẤN ĐỀ; không có dòng nào là tốt.
-- Các ràng buộc đã có trong CSDL (khóa, CHECK, chỉ mục duy nhất) không cần đối chiếu lại ở đây;
-- file này kiểm các ràng buộc chỉ code giữ được (docs/MO_HINH_DU_LIEU.md, mục 6).
-- Chạy bằng sqlcmd thì thêm -I (bật QUOTED_IDENTIFIER, cần cho chỉ mục có lọc).

-- RB-12. SoChoCon phải bằng SoChoToiDa trừ số ghế chưa nhả của đơn chưa hủy (lịch đã hủy: bằng SoChoToiDa)
SELECT l.Id, l.TrangThai, l.SoChoToiDa, l.SoChoCon,
       l.SoChoToiDa - ISNULL(x.SoGhe, 0) AS SoChoConTinhLai
FROM LichKhoiHanhTours l
LEFT JOIN (
    SELECT d.LichKhoiHanhTourId, COUNT(*) AS SoGhe
    FROM DatTours d JOIN HanhKhachDatTours h ON h.DatTourId = d.Id
    WHERE d.TrangThai <> 'DaHuy' AND h.NgayNha IS NULL
    GROUP BY d.LichKhoiHanhTourId
) x ON x.LichKhoiHanhTourId = l.Id
WHERE l.SoChoCon <> l.SoChoToiDa - ISNULL(x.SoGhe, 0);

-- RB-17. Đơn đã hủy thì không còn ghế chưa nhả
SELECT d.Id, COUNT(*) AS GheChuaNha
FROM DatTours d JOIN HanhKhachDatTours h ON h.DatTourId = d.Id
WHERE d.TrangThai = 'DaHuy' AND h.NgayNha IS NULL
GROUP BY d.Id;

-- Đơn chưa hủy mà không còn ghế nào
SELECT d.Id, d.MaChuyenDi FROM DatTours d
WHERE d.TrangThai <> 'DaHuy'
  AND NOT EXISTS (SELECT 1 FROM HanhKhachDatTours h WHERE h.DatTourId = d.Id AND h.NgayNha IS NULL);

-- Trạng thái đơn khớp giao dịch: DaXacNhan phải có giao dịch ThanhCong; có giao dịch ThanhCong thì không còn GiuCho
SELECT d.Id, d.TrangThai,
       (SELECT COUNT(*) FROM GiaoDichThanhToans g WHERE g.DatTourId = d.Id AND g.TrangThai = 'ThanhCong') AS SoGdThanhCong
FROM DatTours d
WHERE (d.TrangThai = 'DaXacNhan' AND NOT EXISTS (SELECT 1 FROM GiaoDichThanhToans g WHERE g.DatTourId = d.Id AND g.TrangThai = 'ThanhCong'))
   OR (d.TrangThai = 'GiuCho' AND EXISTS (SELECT 1 FROM GiaoDichThanhToans g WHERE g.DatTourId = d.Id AND g.TrangThai = 'ThanhCong'));

-- Đơn đã hủy không còn giao dịch đang chờ
SELECT g.Id, g.DatTourId FROM GiaoDichThanhToans g JOIN DatTours d ON d.Id = g.DatTourId
WHERE d.TrangThai = 'DaHuy' AND g.TrangThai = 'ChoXacNhan';

-- Số tiền giảm không vượt tổng giá các ghế còn trong đơn
SELECT d.Id, d.SoTienGiam, SUM(h.GiaLucDat) AS TongGhe
FROM DatTours d JOIN HanhKhachDatTours h ON h.DatTourId = d.Id AND h.NgayNha IS NULL
WHERE d.TrangThai <> 'DaHuy'
GROUP BY d.Id, d.SoTienGiam
HAVING d.SoTienGiam > SUM(h.GiaLucDat);

-- RB-18. Chỗ ngồi: lịch của chỗ ngồi trùng lịch của đơn chứa ghế, vị trí thuộc sơ đồ của lịch, ghế chưa nhả
SELECT c.LichKhoiHanhTourId, c.ViTriChoNgoiId, c.HanhKhachDatTourId
FROM ChoNgoiChuyenDis c
JOIN HanhKhachDatTours h ON h.Id = c.HanhKhachDatTourId
JOIN DatTours d ON d.Id = h.DatTourId
JOIN LichKhoiHanhTours l ON l.Id = c.LichKhoiHanhTourId
JOIN ViTriChoNgois v ON v.Id = c.ViTriChoNgoiId
WHERE d.LichKhoiHanhTourId <> c.LichKhoiHanhTourId
   OR l.SoDoChoNgoiId IS NULL OR v.SoDoChoNgoiId <> l.SoDoChoNgoiId
   OR h.NgayNha IS NOT NULL;

-- RB-15. Phân công đúng vai trò
SELECT p.Id, p.HuongDanVienId, p.TaiXeId FROM PhanCongTours p
LEFT JOIN NhanViens h ON h.Id = p.HuongDanVienId LEFT JOIN TaiKhoans th ON th.Id = h.TaiKhoanId
LEFT JOIN NhanViens x ON x.Id = p.TaiXeId LEFT JOIN TaiKhoans tx ON tx.Id = x.TaiKhoanId
WHERE th.VaiTro <> 'huongdanvien' OR (p.TaiXeId IS NOT NULL AND tx.VaiTro <> 'taixe');
