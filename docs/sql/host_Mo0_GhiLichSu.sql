-- CHỈ CHẠY TRÊN DB HOST, MỘT LẦN, SAU host_xoa_don_17_19.sql VÀ TRƯỚC Mo1.
-- Host đã có cột LichKhoiHanhTours.TrangThai và bảng KhuyenMais nhưng lịch sử migration chưa ghi 2 migration này
-- (đối chiếu từ bản xuất của host ngày 04/11/2026). Chỉ ghi bổ sung lịch sử, không đổi cấu trúc.
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID('KhuyenMais') IS NULL OR COL_LENGTH('LichKhoiHanhTours', 'TrangThai') IS NULL
    THROW 50102, N'Host không có bảng KhuyenMais hoặc cột TrangThai của lịch. Dừng, chưa ghi gì.', 1;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
SELECT v.Ten, N'8.0.0'
FROM (VALUES (N'20260604160610_ThemTrangThaiLichKhoiHanh'), (N'20260605152818_ThemBangKhuyenMai')) v(Ten)
WHERE v.Ten NOT IN (SELECT MigrationId FROM [__EFMigrationsHistory]);

COMMIT;
