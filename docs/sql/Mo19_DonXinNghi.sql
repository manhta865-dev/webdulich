BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111145648_Mo19_DonXinNghi'
)
BEGIN
    CREATE TABLE [DonXinNghis] (
        [Id] int NOT NULL IDENTITY,
        [NhanVienId] int NOT NULL,
        [TuNgay] date NOT NULL,
        [DenNgay] date NOT NULL,
        [LyDo] nvarchar(500) NOT NULL,
        [TrangThai] nvarchar(20) NOT NULL,
        [NgayGui] datetime2 NOT NULL,
        [NguoiDuyetId] int NULL,
        [NgayDuyet] datetime2 NULL,
        [GhiChuDuyet] nvarchar(300) NULL,
        CONSTRAINT [PK_DonXinNghis] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_DonNghi_Duyet] CHECK (([TrangThai] IN ('ChoDuyet','DaRut') AND [NguoiDuyetId] IS NULL AND [NgayDuyet] IS NULL) OR ([TrangThai] IN ('DaDuyet','TuChoi') AND [NguoiDuyetId] IS NOT NULL AND [NgayDuyet] IS NOT NULL)),
        CONSTRAINT [CK_DonNghi_Ngay] CHECK ([DenNgay] >= [TuNgay]),
        CONSTRAINT [CK_DonNghi_TrangThai] CHECK ([TrangThai] IN ('ChoDuyet','DaDuyet','TuChoi','DaRut')),
        CONSTRAINT [FK_DonXinNghis_NhanViens_NhanVienId] FOREIGN KEY ([NhanVienId]) REFERENCES [NhanViens] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_DonXinNghis_TaiKhoans_NguoiDuyetId] FOREIGN KEY ([NguoiDuyetId]) REFERENCES [TaiKhoans] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111145648_Mo19_DonXinNghi'
)
BEGIN
    CREATE INDEX [IX_DonXinNghis_NguoiDuyetId] ON [DonXinNghis] ([NguoiDuyetId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111145648_Mo19_DonXinNghi'
)
BEGIN
    CREATE INDEX [IX_DonXinNghis_NhanVienId_TuNgay] ON [DonXinNghis] ([NhanVienId], [TuNgay]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111145648_Mo19_DonXinNghi'
)
BEGIN
    CREATE INDEX [IX_DonXinNghis_TrangThai] ON [DonXinNghis] ([TrangThai]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111145648_Mo19_DonXinNghi'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261111145648_Mo19_DonXinNghi', N'8.0.0');
END;
GO

COMMIT;
GO

