-- Mo24: thêm cột DiemThamQuans.LinkNhung360 (địa chỉ nhúng đúng tấm ảnh 360 của điểm, lấy từ nút Nhúng bản đồ của Google Maps)
-- và CHECK chỉ nhận địa chỉ bắt đầu bằng https://www.google.com/maps/embed? (RB-49). Cột cho phép rỗng, không đổi dữ liệu cũ.
-- Chạy trên host SAU Mo23_DiemThamQuan.sql và TRƯỚC khi đưa code có ô mã nhúng 360 lên.
-- Bước 0. Sao lưu database trên trang quản lý host.
-- Bước 1. Chạy toàn bộ file này. Chạy lại cũng không sao (có kiểm __EFMigrationsHistory).

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121163110_Mo24_LinkNhung360'
)
BEGIN
    ALTER TABLE [DiemThamQuans] ADD [LinkNhung360] varchar(1000) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121163110_Mo24_LinkNhung360'
)
BEGIN
    EXEC(N'ALTER TABLE [DiemThamQuans] ADD CONSTRAINT [CK_DiemThamQuan_LinkNhung360] CHECK ([LinkNhung360] IS NULL OR [LinkNhung360] LIKE ''https://www.google.com/maps/embed?%'')');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121163110_Mo24_LinkNhung360'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261121163110_Mo24_LinkNhung360', N'8.0.0');
END;
GO

COMMIT;
GO

