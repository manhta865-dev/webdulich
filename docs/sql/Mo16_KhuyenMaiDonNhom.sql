-- Mo16: khuyến mãi cho đơn nhóm (RB-40). Thêm cột HanhKhachDatTours.TienGiam (phần giảm của từng ghế, mặc định 0)
-- và hai ràng buộc: 0 <= TienGiam <= GiaLucDat; đơn nhóm không lưu tiền giảm ở đơn (SoTienGiam = 0).
-- Chạy trên host SAU Mo14_Mo15_ChoKhongMoBan.sql và TRƯỚC khi đưa code mới lên.
--
-- Bước 0. Sao lưu database trên trang quản lý host.
-- Bước 1. Chạy riêng câu kiểm tra dưới đây, kết quả phải bằng 0 (không có đơn nhóm nào đang lưu tiền giảm ở đơn).
--   SELECT COUNT(*) FROM DatTours WHERE LoaiDon = 'Nhom' AND SoTienGiam <> 0;
-- Bước 2. Chạy toàn bộ file này. Chạy lại cũng không sao (có kiểm __EFMigrationsHistory).

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107171901_Mo16_KhuyenMaiDonNhom'
)
BEGIN
    ALTER TABLE [HanhKhachDatTours] ADD [TienGiam] decimal(18,2) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107171901_Mo16_KhuyenMaiDonNhom'
)
BEGIN
    EXEC(N'ALTER TABLE [HanhKhachDatTours] ADD CONSTRAINT [CK_HanhKhach_TienGiam] CHECK ([TienGiam] >= 0 AND [TienGiam] <= [GiaLucDat])');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107171901_Mo16_KhuyenMaiDonNhom'
)
BEGIN
    EXEC(N'ALTER TABLE [DatTours] ADD CONSTRAINT [CK_DatTours_GiamDonNhom] CHECK ([LoaiDon] = ''CaNhan'' OR [SoTienGiam] = 0)');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107171901_Mo16_KhuyenMaiDonNhom'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261107171901_Mo16_KhuyenMaiDonNhom', N'8.0.0');
END;
GO

COMMIT;
GO

