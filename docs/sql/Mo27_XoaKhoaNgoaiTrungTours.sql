-- Mo27: xóa hai khóa ngoại trùng trên bảng Tours còn sót từ đồ án cơ sở (FK_Tours_DanhMucs, FK_Tours_DiaDiems).
-- Hai khóa do EF quản lý (FK_Tours_DanhMucs_DanhMucId, FK_Tours_DiaDiems_DiaDiemId) giữ nguyên nên ràng buộc không đổi.
-- Chạy trên host SAU Mo26_PhanQuyenAdmin.sql. Không cần đưa code mới lên trước hay sau (model không đổi).
-- Bước 0. Sao lưu database trên trang quản lý host.
-- Bước 1. Chạy toàn bộ file này. Chạy lại cũng không sao (có kiểm __EFMigrationsHistory và kiểm khóa còn hay không).
BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126112331_Mo27_XoaKhoaNgoaiTrungTours'
)
BEGIN
    IF OBJECT_ID(N'[FK_Tours_DanhMucs]', N'F') IS NOT NULL ALTER TABLE [Tours] DROP CONSTRAINT [FK_Tours_DanhMucs];
    IF OBJECT_ID(N'[FK_Tours_DiaDiems]', N'F') IS NOT NULL ALTER TABLE [Tours] DROP CONSTRAINT [FK_Tours_DiaDiems];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126112331_Mo27_XoaKhoaNgoaiTrungTours'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261126112331_Mo27_XoaKhoaNgoaiTrungTours', N'8.0.0');
END;
GO

COMMIT;
GO

