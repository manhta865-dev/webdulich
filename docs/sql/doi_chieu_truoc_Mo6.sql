-- Truy vấn đối chiếu dùng TRƯỚC khi chạy Mo6 (còn cột cũ). Chạy trên host sau Mo2 đến Mo5 để biết dữ liệu nào cần xử lý.
-- Sau Mo6 dùng docs/sql/doi_chieu.sql.
-- Mỗi truy vấn trả về các dòng CÓ VẤN ĐỀ; không có dòng nào là tốt.

-- A. Tài khoản chưa tách được sang Email hoặc SoDienThoai
SELECT Id, TaiKhoanDangNhap FROM TaiKhoans WHERE Email IS NULL AND SoDienThoai IS NULL;

-- B. Nhân viên có số điện thoại nhưng tài khoản chưa nhận số đó (trùng hoặc tài khoản đã có số khác)
SELECT n.Id, n.TaiKhoanId, n.SoDienThoai, t.SoDienThoai AS SoCuaTaiKhoan
FROM NhanViens n JOIN TaiKhoans t ON t.Id = n.TaiKhoanId
WHERE NULLIF(LTRIM(RTRIM(n.SoDienThoai)), '') IS NOT NULL
  AND ISNULL(t.SoDienThoai, '') <> LTRIM(RTRIM(n.SoDienThoai));

-- C. Đơn chưa có lịch khởi hành (phải xử lý trước bước thu hẹp)
SELECT Id, TourId, NgayDi, MaChuyenDi FROM DatTours WHERE LichKhoiHanhTourId IS NULL;

-- D. Số ghế chưa nhả khác SoNguoi của đơn chưa hủy
SELECT d.Id, d.SoNguoi, COUNT(h.Id) AS SoGheChuaNha
FROM DatTours d
LEFT JOIN HanhKhachDatTours h ON h.DatTourId = d.Id AND h.NgayNha IS NULL
WHERE d.TrangThai <> 'DaHuy'
GROUP BY d.Id, d.SoNguoi
HAVING COUNT(h.Id) <> d.SoNguoi;

-- E. Tổng giá ghế khác tổng tiền cũ (tổng trước giảm − giảm phải bằng TongTien)
SELECT d.Id, d.TongTien, SUM(h.GiaLucDat) - d.SoTienGiam AS TinhLai
FROM DatTours d JOIN HanhKhachDatTours h ON h.DatTourId = d.Id AND h.NgayNha IS NULL
WHERE d.TrangThai <> 'DaHuy'
GROUP BY d.Id, d.TongTien, d.SoTienGiam
HAVING SUM(h.GiaLucDat) - d.SoTienGiam <> d.TongTien;

-- F. RB-12: SoChoCon phải bằng SoChoToiDa trừ số ghế chưa nhả của đơn chưa hủy
SELECT l.Id, l.SoChoToiDa, l.SoChoCon,
       l.SoChoToiDa - ISNULL(x.SoGhe, 0) AS SoChoConTinhLai
FROM LichKhoiHanhTours l
LEFT JOIN (
    SELECT d.LichKhoiHanhTourId, COUNT(*) AS SoGhe
    FROM DatTours d JOIN HanhKhachDatTours h ON h.DatTourId = d.Id
    WHERE d.TrangThai <> 'DaHuy' AND h.NgayNha IS NULL
    GROUP BY d.LichKhoiHanhTourId
) x ON x.LichKhoiHanhTourId = l.Id
WHERE l.SoChoCon <> l.SoChoToiDa - ISNULL(x.SoGhe, 0);

-- G. Đơn đã thanh toán nhưng chưa có giao dịch ThanhCong
SELECT d.Id, d.TongTien FROM DatTours d
WHERE d.TrangThaiThanhToan = N'Đã thanh toán'
  AND NOT EXISTS (SELECT 1 FROM GiaoDichThanhToans g WHERE g.DatTourId = d.Id AND g.TrangThai = 'ThanhCong');

-- H. Lượt dùng khuyến mãi lưu sẵn khác số đơn chưa hủy đang dùng mã
SELECT k.Id, k.SoLuotDaDung,
       (SELECT COUNT(*) FROM DatTours d WHERE d.KhuyenMaiId = k.Id AND d.TrangThai <> 'DaHuy') AS DemThucTe
FROM KhuyenMais k
WHERE k.SoLuotDaDung <> (SELECT COUNT(*) FROM DatTours d WHERE d.KhuyenMaiId = k.Id AND d.TrangThai <> 'DaHuy');
