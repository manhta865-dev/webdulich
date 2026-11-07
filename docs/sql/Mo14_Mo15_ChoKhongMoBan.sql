-- Mo14 + Mo15: chỗ không mở bán theo từng lịch (RB-37), admin tự chọn xe (RB-14), bỏ cột SoChoToiDa.
-- Chạy trên host SAU Mo10, Mo11, Mo12, Mo13, và TRƯỚC khi đưa code mới lên.
--
-- Bước 0. Sao lưu database trên trang quản lý host.
-- Bước 1. Chạy riêng câu kiểm tra dưới đây. Kết quả phải RỖNG (mọi lịch đã có xe).
--         Nếu có dòng: báo lại để gán xe cho các lịch đó trước. Mo15 cũng tự dừng nếu còn lịch chưa có xe.
--   SELECT Id, TourId, NgayKhoiHanh, SoChoToiDa FROM LichKhoiHanhTours WHERE SoDoChoNgoiId IS NULL;
-- Bước 2. Chạy toàn bộ file này. Chạy lại lần nữa cũng không sao (có kiểm __EFMigrationsHistory).
-- Bước 3. Chạy phần RB-12 và RB-37 trong doi_chieu.sql, kết quả phải rỗng.

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107160021_Mo14_ChoKhongMoBan'
)
BEGIN
    CREATE TABLE [ChoKhongMoBans] (
        [LichKhoiHanhTourId] int NOT NULL,
        [ViTriChoNgoiId] int NOT NULL,
        CONSTRAINT [PK_ChoKhongMoBans] PRIMARY KEY ([LichKhoiHanhTourId], [ViTriChoNgoiId]),
        CONSTRAINT [FK_ChoKhongMoBans_LichKhoiHanhTours_LichKhoiHanhTourId] FOREIGN KEY ([LichKhoiHanhTourId]) REFERENCES [LichKhoiHanhTours] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ChoKhongMoBans_ViTriChoNgois_ViTriChoNgoiId] FOREIGN KEY ([ViTriChoNgoiId]) REFERENCES [ViTriChoNgois] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107160021_Mo14_ChoKhongMoBan'
)
BEGIN
    CREATE INDEX [IX_ChoKhongMoBans_ViTriChoNgoiId] ON [ChoKhongMoBans] ([ViTriChoNgoiId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107160021_Mo14_ChoKhongMoBan'
)
BEGIN
    -- Chạy qua EXEC để khi chạy lại file (cột SoChoToiDa đã bỏ ở Mo15) không bị lỗi biên dịch
    EXEC(N'
    INSERT INTO ChoKhongMoBans (LichKhoiHanhTourId, ViTriChoNgoiId)
    SELECT x.LichId, x.ViTriId
    FROM (
        SELECT l.Id AS LichId, v.Id AS ViTriId, l.SoChoToiDa,
               ROW_NUMBER() OVER (PARTITION BY l.Id ORDER BY v.Hang DESC, v.Cot DESC) AS ThuTu,
               (SELECT COUNT(*) FROM ViTriChoNgois v2 WHERE v2.SoDoChoNgoiId = l.SoDoChoNgoiId) AS SoViTri
        FROM LichKhoiHanhTours l
        JOIN ViTriChoNgois v ON v.SoDoChoNgoiId = l.SoDoChoNgoiId
        WHERE NOT EXISTS (SELECT 1 FROM ChoNgoiChuyenDis c WHERE c.LichKhoiHanhTourId = l.Id AND c.ViTriChoNgoiId = v.Id)
          AND NOT EXISTS (SELECT 1 FROM ChoKhongMoBans k WHERE k.LichKhoiHanhTourId = l.Id)
    ) x
    WHERE x.ThuTu <= x.SoViTri - x.SoChoToiDa;');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107160021_Mo14_ChoKhongMoBan'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261107160021_Mo14_ChoKhongMoBan', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107160828_Mo15_BoSoChoToiDa'
)
BEGIN
    -- Các khối Mo15 bên dưới đều có điều kiện "không còn lịch chưa có xe", nên nếu báo lỗi ở đây thì không khối nào chạy
    IF EXISTS (SELECT 1 FROM LichKhoiHanhTours WHERE SoDoChoNgoiId IS NULL)
        THROW 50037, N'Còn lịch khởi hành chưa có xe (SoDoChoNgoiId rỗng). Gán xe cho các lịch đó rồi chạy lại.', 1;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107160828_Mo15_BoSoChoToiDa'
) AND NOT EXISTS (SELECT 1 FROM LichKhoiHanhTours WHERE SoDoChoNgoiId IS NULL)
BEGIN
    ALTER TABLE [LichKhoiHanhTours] DROP CONSTRAINT [CK_Lich_SoCho];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107160828_Mo15_BoSoChoToiDa'
) AND NOT EXISTS (SELECT 1 FROM LichKhoiHanhTours WHERE SoDoChoNgoiId IS NULL)
BEGIN
    DECLARE @var0 sysname;
    SELECT @var0 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[LichKhoiHanhTours]') AND [c].[name] = N'SoChoToiDa');
    IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [LichKhoiHanhTours] DROP CONSTRAINT [' + @var0 + '];');
    ALTER TABLE [LichKhoiHanhTours] DROP COLUMN [SoChoToiDa];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107160828_Mo15_BoSoChoToiDa'
) AND NOT EXISTS (SELECT 1 FROM LichKhoiHanhTours WHERE SoDoChoNgoiId IS NULL)
BEGIN
    DROP INDEX [IX_LichKhoiHanhTours_SoDoChoNgoiId] ON [LichKhoiHanhTours];
    DECLARE @var1 sysname;
    SELECT @var1 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[LichKhoiHanhTours]') AND [c].[name] = N'SoDoChoNgoiId');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [LichKhoiHanhTours] DROP CONSTRAINT [' + @var1 + '];');
    EXEC(N'UPDATE [LichKhoiHanhTours] SET [SoDoChoNgoiId] = 0 WHERE [SoDoChoNgoiId] IS NULL');
    ALTER TABLE [LichKhoiHanhTours] ALTER COLUMN [SoDoChoNgoiId] int NOT NULL;
    ALTER TABLE [LichKhoiHanhTours] ADD DEFAULT 0 FOR [SoDoChoNgoiId];
    CREATE INDEX [IX_LichKhoiHanhTours_SoDoChoNgoiId] ON [LichKhoiHanhTours] ([SoDoChoNgoiId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107160828_Mo15_BoSoChoToiDa'
) AND NOT EXISTS (SELECT 1 FROM LichKhoiHanhTours WHERE SoDoChoNgoiId IS NULL)
BEGIN
    EXEC(N'ALTER TABLE [LichKhoiHanhTours] ADD CONSTRAINT [CK_Lich_SoCho] CHECK ([SoChoCon] >= 0)');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107160828_Mo15_BoSoChoToiDa'
) AND NOT EXISTS (SELECT 1 FROM LichKhoiHanhTours WHERE SoDoChoNgoiId IS NULL)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261107160828_Mo15_BoSoChoToiDa', N'8.0.0');
END;
GO

COMMIT;
GO

