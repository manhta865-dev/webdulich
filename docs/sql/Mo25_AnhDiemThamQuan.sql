-- Mo25: thêm cột DiemThamQuans.HinhAnh (tên tệp ảnh của điểm tham quan trong wwwroot/images/diem), cho phép rỗng, không đổi dữ liệu cũ (RB-49).
-- Chạy trên host SAU Mo24_LinkNhung360.sql và TRƯỚC khi đưa code có ảnh điểm tham quan lên.
-- Bước 0. Sao lưu database trên trang quản lý host.
-- Bước 1. Chạy toàn bộ file này. Chạy lại cũng không sao (có kiểm __EFMigrationsHistory).

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122110402_Mo25_AnhDiemThamQuan'
)
BEGIN
    ALTER TABLE [DiemThamQuans] ADD [HinhAnh] nvarchar(260) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122110402_Mo25_AnhDiemThamQuan'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261122110402_Mo25_AnhDiemThamQuan', N'8.0.0');
END;
GO

COMMIT;
GO

