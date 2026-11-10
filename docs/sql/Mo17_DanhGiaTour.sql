BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261109143646_Mo17_DanhGiaTour'
)
BEGIN
    CREATE TABLE [DanhGiaTours] (
        [Id] int NOT NULL IDENTITY,
        [LichKhoiHanhTourId] int NOT NULL,
        [TaiKhoanId] int NOT NULL,
        [SoSao] int NOT NULL,
        [NoiDung] nvarchar(1000) NULL,
        [NgayTao] datetime2 NOT NULL,
        [NgaySua] datetime2 NULL,
        [DangHien] bit NOT NULL,
        CONSTRAINT [PK_DanhGiaTours] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_DanhGia_SoSao] CHECK ([SoSao] BETWEEN 1 AND 5),
        CONSTRAINT [FK_DanhGiaTours_LichKhoiHanhTours_LichKhoiHanhTourId] FOREIGN KEY ([LichKhoiHanhTourId]) REFERENCES [LichKhoiHanhTours] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_DanhGiaTours_TaiKhoans_TaiKhoanId] FOREIGN KEY ([TaiKhoanId]) REFERENCES [TaiKhoans] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261109143646_Mo17_DanhGiaTour'
)
BEGIN
    CREATE INDEX [IX_DanhGiaTours_LichKhoiHanhTourId] ON [DanhGiaTours] ([LichKhoiHanhTourId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261109143646_Mo17_DanhGiaTour'
)
BEGIN
    CREATE UNIQUE INDEX [IX_DanhGiaTours_TaiKhoanId_LichKhoiHanhTourId] ON [DanhGiaTours] ([TaiKhoanId], [LichKhoiHanhTourId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261109143646_Mo17_DanhGiaTour'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261109143646_Mo17_DanhGiaTour', N'8.0.0');
END;
GO

COMMIT;
GO

