-- Mo23: bảng DiemThamQuans (điểm tham quan của một điểm đến) và LichTrinhDiemThamQuans (ngày nào ghé điểm nào, theo thứ tự),
-- dùng cho chip địa điểm và Street View ở trang Khám phá 3D (RB-49, RB-50). Chỉ thêm bảng mới, không sửa bảng cũ.
-- Chạy trên host SAU Mo22 và TRƯỚC khi đưa code có điểm tham quan lên.
-- Bước 0. Sao lưu database trên trang quản lý host.
-- Bước 1. Chạy toàn bộ file này. Chạy lại cũng không sao (có kiểm __EFMigrationsHistory).

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121155908_Mo23_DiemThamQuan'
)
BEGIN
    CREATE TABLE [DiemThamQuans] (
        [Id] int NOT NULL IDENTITY,
        [DiaDiemId] int NOT NULL,
        [Ten] nvarchar(150) NOT NULL,
        [ViDo] decimal(9,6) NOT NULL,
        [KinhDo] decimal(9,6) NOT NULL,
        CONSTRAINT [PK_DiemThamQuans] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_DiemThamQuan_ToaDo] CHECK ([ViDo] BETWEEN 8 AND 24 AND [KinhDo] BETWEEN 102 AND 110),
        CONSTRAINT [FK_DiemThamQuans_DiaDiems_DiaDiemId] FOREIGN KEY ([DiaDiemId]) REFERENCES [DiaDiems] ([MaDiaDiem]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121155908_Mo23_DiemThamQuan'
)
BEGIN
    CREATE TABLE [LichTrinhDiemThamQuans] (
        [LichTrinhTourId] int NOT NULL,
        [DiemThamQuanId] int NOT NULL,
        [ThuTu] int NOT NULL,
        CONSTRAINT [PK_LichTrinhDiemThamQuans] PRIMARY KEY ([LichTrinhTourId], [DiemThamQuanId]),
        CONSTRAINT [CK_LichTrinhDiem_ThuTu] CHECK ([ThuTu] >= 1),
        CONSTRAINT [FK_LichTrinhDiemThamQuans_DiemThamQuans_DiemThamQuanId] FOREIGN KEY ([DiemThamQuanId]) REFERENCES [DiemThamQuans] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_LichTrinhDiemThamQuans_LichTrinhTours_LichTrinhTourId] FOREIGN KEY ([LichTrinhTourId]) REFERENCES [LichTrinhTours] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121155908_Mo23_DiemThamQuan'
)
BEGIN
    CREATE UNIQUE INDEX [IX_DiemThamQuans_DiaDiemId_Ten] ON [DiemThamQuans] ([DiaDiemId], [Ten]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121155908_Mo23_DiemThamQuan'
)
BEGIN
    CREATE INDEX [IX_LichTrinhDiemThamQuans_DiemThamQuanId] ON [LichTrinhDiemThamQuans] ([DiemThamQuanId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121155908_Mo23_DiemThamQuan'
)
BEGIN
    CREATE UNIQUE INDEX [IX_LichTrinhDiemThamQuans_LichTrinhTourId_ThuTu] ON [LichTrinhDiemThamQuans] ([LichTrinhTourId], [ThuTu]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121155908_Mo23_DiemThamQuan'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261121155908_Mo23_DiemThamQuan', N'8.0.0');
END;
GO

COMMIT;
GO

