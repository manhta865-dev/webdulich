-- Mo28: đồng bộ 4 cột giữa code và CSDL (lệch từ đồ án cơ sở).
--   DatTours.TenKhach  -> nvarchar(100) NOT NULL
--   DatTours.DienThoai -> nvarchar(20)  NOT NULL
--   Tours.MoTa, Tours.HinhAnh -> nvarchar(max) NOT NULL (trên máy đã bắt buộc sẵn)
-- Chạy trên host SAU Mo27_XoaKhoaNgoaiTrungTours.sql. Đưa code mới lên SAU khi chạy file này
-- (code cũ vẫn chạy được với cột mới vì code cũ cũng luôn ghi đủ 4 cột).
-- Bước 0. Sao lưu database trên trang quản lý host.
-- Bước 1. Chạy host_kiem_truoc_Mo28.sql: mọi SoDongRong, SoDongQuaDai phải bằng 0.
-- Bước 2. Chạy toàn bộ file này. Nếu còn dòng rỗng hoặc quá dài, script báo lỗi và hoàn tác hết, không sửa gì.
--         Chạy lại cũng không sao (có kiểm __EFMigrationsHistory).
-- Cả file là MỘT lô lệnh (không có GO ở giữa) để khi báo lỗi thì không lệnh nào phía sau chạy tiếp.
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126115805_Mo28_DongBoCotLienHeVaTour'
)
BEGIN
    IF EXISTS (SELECT 1 FROM [DatTours] WHERE [TenKhach] IS NULL OR [DienThoai] IS NULL
                                           OR LEN([TenKhach]) > 100 OR LEN([DienThoai]) > 20)
        THROW 50028, N'Mo28: còn đơn có TenKhach hoặc DienThoai rỗng hoặc quá dài. Chạy host_kiem_truoc_Mo28.sql để xem.', 1;
    IF EXISTS (SELECT 1 FROM [Tours] WHERE [MoTa] IS NULL OR [HinhAnh] IS NULL)
        THROW 50028, N'Mo28: còn tour thiếu MoTa hoặc HinhAnh. Chạy host_kiem_truoc_Mo28.sql để xem.', 1;

    -- Bỏ ràng buộc DEFAULT nếu host có (máy không có) trước khi đổi cột, như EF sinh ra
    DECLARE @ten sysname;
    SELECT @ten = d.name FROM sys.default_constraints d
        JOIN sys.columns c ON c.object_id = d.parent_object_id AND c.column_id = d.parent_column_id
        WHERE d.parent_object_id = OBJECT_ID(N'[Tours]') AND c.name = N'MoTa';
    IF @ten IS NOT NULL EXEC(N'ALTER TABLE [Tours] DROP CONSTRAINT [' + @ten + N'];');
    ALTER TABLE [Tours] ALTER COLUMN [MoTa] nvarchar(max) NOT NULL;

    SET @ten = NULL;
    SELECT @ten = d.name FROM sys.default_constraints d
        JOIN sys.columns c ON c.object_id = d.parent_object_id AND c.column_id = d.parent_column_id
        WHERE d.parent_object_id = OBJECT_ID(N'[Tours]') AND c.name = N'HinhAnh';
    IF @ten IS NOT NULL EXEC(N'ALTER TABLE [Tours] DROP CONSTRAINT [' + @ten + N'];');
    ALTER TABLE [Tours] ALTER COLUMN [HinhAnh] nvarchar(max) NOT NULL;

    SET @ten = NULL;
    SELECT @ten = d.name FROM sys.default_constraints d
        JOIN sys.columns c ON c.object_id = d.parent_object_id AND c.column_id = d.parent_column_id
        WHERE d.parent_object_id = OBJECT_ID(N'[DatTours]') AND c.name = N'TenKhach';
    IF @ten IS NOT NULL EXEC(N'ALTER TABLE [DatTours] DROP CONSTRAINT [' + @ten + N'];');
    ALTER TABLE [DatTours] ALTER COLUMN [TenKhach] nvarchar(100) NOT NULL;

    SET @ten = NULL;
    SELECT @ten = d.name FROM sys.default_constraints d
        JOIN sys.columns c ON c.object_id = d.parent_object_id AND c.column_id = d.parent_column_id
        WHERE d.parent_object_id = OBJECT_ID(N'[DatTours]') AND c.name = N'DienThoai';
    IF @ten IS NOT NULL EXEC(N'ALTER TABLE [DatTours] DROP CONSTRAINT [' + @ten + N'];');
    ALTER TABLE [DatTours] ALTER COLUMN [DienThoai] nvarchar(20) NOT NULL;

    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261126115805_Mo28_DongBoCotLienHeVaTour', N'8.0.0');
END;

COMMIT;
