-- Kiểm tra trên DB host TRƯỚC khi chạy Mo10 đến Mo20. Chỉ đọc, không sửa gì, chạy bao nhiêu lần cũng được.
--
-- Thứ tự đưa bản Mo20 lên host (đợt gom một lần):
--   1. Sao lưu database host (trang quản lý host, mục Backups), tải bản .bak về máy.
--   2. Chạy file này. Đọc ba bảng kết quả:
--      (a) Danh sách script: chạy LẦN LƯỢT từ trên xuống các dòng ghi "CHUA CHAY", bỏ qua dòng "da chay".
--          Mo10 đến Mo13 không tự kiểm lịch sử, chạy lại sẽ lỗi, nên chỉ chạy khi dòng đó ghi "CHUA CHAY".
--          Mo14 và Mo15 nằm chung file Mo14_Mo15_ChoKhongMoBan.sql.
--      (b) Lịch chưa gán xe: phải RỖNG trước khi chạy Mo14_Mo15. Có dòng thì dừng, gán xe cho các lịch đó trước.
--      (c) Số đơn nhóm đang lưu tiền giảm ở đơn: phải bằng 0 trước khi chạy Mo16.
--   3. Chạy các script còn thiếu, đúng thứ tự, mỗi file một lần. Sau đó chạy lại file này: mọi dòng phải là "da chay".
--   4. Chạy doi_chieu.sql: mọi truy vấn phải không có dòng nào.
--   5. Mới đưa code (gói zip) lên. Code mới đọc các bảng và cột mới, đưa code lên trước khi chạy script sẽ báo lỗi.
--      Làm bước 3 đến 5 liền nhau, lúc ít người dùng: Mo15 bỏ cột SoChoToiDa mà code cũ đang đọc,
--      nên trong khoảng giữa lúc chạy Mo15 và lúc đưa code mới lên, trang đặt tour trên host sẽ báo lỗi.
--   Không chạy lại host_doi_han_sang_gio_VN.sql, host_Mo0_GhiLichSu.sql, host_xoa_don_17_19.sql: đã chạy ở các đợt trước.
SET NOCOUNT ON;

-- (a) Script nào đã chạy, script nào chưa
SELECT ds.ThuTu, ds.TepScript,
       CASE WHEN h.MigrationId IS NULL THEN 'CHUA CHAY' ELSE 'da chay' END AS TinhTrang
FROM (VALUES
    (1,  N'20261105172301_Mo10_ToaDoLichTrinh',     N'Mo10_ToaDoLichTrinh.sql'),
    (2,  N'20261106124145_Mo11_XuatPhatVaDuongDi',  N'Mo11_XuatPhatVaDuongDi.sql'),
    (3,  N'20261106125521_Mo12_BaiViet',            N'Mo12_BaiViet.sql'),
    (4,  N'20261106145952_Mo13_Video',              N'Mo13_Video.sql'),
    (5,  N'20261107160021_Mo14_ChoKhongMoBan',      N'Mo14_Mo15_ChoKhongMoBan.sql (phan Mo14)'),
    (6,  N'20261107160828_Mo15_BoSoChoToiDa',       N'Mo14_Mo15_ChoKhongMoBan.sql (phan Mo15)'),
    (7,  N'20261107171901_Mo16_KhuyenMaiDonNhom',   N'Mo16_KhuyenMaiDonNhom.sql'),
    (8,  N'20261109143646_Mo17_DanhGiaTour',        N'Mo17_DanhGiaTour.sql'),
    (9,  N'20261109154239_Mo18_ToaDoDiaDiem',       N'Mo18_ToaDoDiaDiem.sql'),
    (10, N'20261111145648_Mo19_DonXinNghi',         N'Mo19_DonXinNghi.sql'),
    (11, N'20261111151203_Mo20_LuongNhanVien',      N'Mo20_LuongNhanVien.sql')
) AS ds(ThuTu, MigrationId, TepScript)
LEFT JOIN [__EFMigrationsHistory] h ON h.MigrationId = ds.MigrationId
ORDER BY ds.ThuTu;

-- Migration cuối cùng host đã ghi (phải là Mo9_DonNhom nếu chưa chạy gì của đợt này)
SELECT TOP (1) MigrationId AS MigrationCuoiCung FROM [__EFMigrationsHistory] ORDER BY MigrationId DESC;

-- (b) Lịch chưa gán xe: phải rỗng trước Mo14_Mo15
SELECT Id, TourId, NgayKhoiHanh FROM LichKhoiHanhTours WHERE SoDoChoNgoiId IS NULL;

-- (c) Đơn nhóm đang lưu tiền giảm ở đơn: phải bằng 0 trước Mo16
SELECT COUNT(*) AS DonNhomCoTienGiamODon FROM DatTours WHERE LoaiDon = 'Nhom' AND SoTienGiam <> 0;

-- Bảng của Mo12, Mo13 đã có sẵn mà lịch sử chưa ghi (lần chạy trước bị dừng giữa chừng): phải không có dòng nào
SELECT ds.Bang AS BangCoSanMaChuaGhiLichSu
FROM (VALUES (N'BaiViets', N'20261106125521_Mo12_BaiViet'), (N'Videos', N'20261106145952_Mo13_Video')) AS ds(Bang, MigrationId)
WHERE OBJECT_ID(ds.Bang) IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] h WHERE h.MigrationId = ds.MigrationId);
