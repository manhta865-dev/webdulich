BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111151203_Mo20_LuongNhanVien'
)
BEGIN
    CREATE TABLE [BangLuongs] (
        [Id] int NOT NULL IDENTITY,
        [NhanVienId] int NOT NULL,
        [Nam] int NOT NULL,
        [Thang] int NOT NULL,
        [LuongCoBan] decimal(18,2) NOT NULL,
        [TienCongNgay] decimal(18,2) NOT NULL,
        [SoNgayDiTour] int NOT NULL,
        [SoNgayNghiTruLuong] int NOT NULL,
        [SoNgayCongChuan] int NOT NULL,
        [NguoiChotId] int NOT NULL,
        [NgayChot] datetime2 NOT NULL,
        [NguoiTraId] int NULL,
        [NgayTra] datetime2 NULL,
        CONSTRAINT [PK_BangLuongs] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_BangLuong_SoLieu] CHECK ([LuongCoBan] >= 0 AND [TienCongNgay] >= 0 AND [SoNgayDiTour] >= 0 AND [SoNgayNghiTruLuong] >= 0 AND [SoNgayCongChuan] > 0),
        CONSTRAINT [CK_BangLuong_Thang] CHECK ([Thang] BETWEEN 1 AND 12 AND [Nam] BETWEEN 2000 AND 2100),
        CONSTRAINT [CK_BangLuong_Tra] CHECK (([NguoiTraId] IS NULL AND [NgayTra] IS NULL) OR ([NguoiTraId] IS NOT NULL AND [NgayTra] IS NOT NULL)),
        CONSTRAINT [FK_BangLuongs_NhanViens_NhanVienId] FOREIGN KEY ([NhanVienId]) REFERENCES [NhanViens] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_BangLuongs_TaiKhoans_NguoiChotId] FOREIGN KEY ([NguoiChotId]) REFERENCES [TaiKhoans] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_BangLuongs_TaiKhoans_NguoiTraId] FOREIGN KEY ([NguoiTraId]) REFERENCES [TaiKhoans] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111151203_Mo20_LuongNhanVien'
)
BEGIN
    CREATE TABLE [KhoanLuongs] (
        [Id] int NOT NULL IDENTITY,
        [NhanVienId] int NOT NULL,
        [Nam] int NOT NULL,
        [Thang] int NOT NULL,
        [SoTien] decimal(18,2) NOT NULL,
        [NoiDung] nvarchar(300) NOT NULL,
        [NguoiTaoId] int NOT NULL,
        [NgayTao] datetime2 NOT NULL,
        CONSTRAINT [PK_KhoanLuongs] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_KhoanLuong_SoTien] CHECK ([SoTien] <> 0),
        CONSTRAINT [CK_KhoanLuong_Thang] CHECK ([Thang] BETWEEN 1 AND 12 AND [Nam] BETWEEN 2000 AND 2100),
        CONSTRAINT [FK_KhoanLuongs_NhanViens_NhanVienId] FOREIGN KEY ([NhanVienId]) REFERENCES [NhanViens] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_KhoanLuongs_TaiKhoans_NguoiTaoId] FOREIGN KEY ([NguoiTaoId]) REFERENCES [TaiKhoans] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111151203_Mo20_LuongNhanVien'
)
BEGIN
    CREATE TABLE [MucLuongNhanViens] (
        [Id] int NOT NULL IDENTITY,
        [NhanVienId] int NOT NULL,
        [LuongCoBan] decimal(18,2) NOT NULL,
        [TienCongNgay] decimal(18,2) NOT NULL,
        [ApDungTu] date NOT NULL,
        [GhiChu] nvarchar(300) NULL,
        [NgayTao] datetime2 NOT NULL,
        CONSTRAINT [PK_MucLuongNhanViens] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_MucLuong_SoTien] CHECK ([LuongCoBan] >= 0 AND [TienCongNgay] >= 0),
        CONSTRAINT [FK_MucLuongNhanViens_NhanViens_NhanVienId] FOREIGN KEY ([NhanVienId]) REFERENCES [NhanViens] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111151203_Mo20_LuongNhanVien'
)
BEGIN
    CREATE INDEX [IX_BangLuongs_NguoiChotId] ON [BangLuongs] ([NguoiChotId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111151203_Mo20_LuongNhanVien'
)
BEGIN
    CREATE INDEX [IX_BangLuongs_NguoiTraId] ON [BangLuongs] ([NguoiTraId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111151203_Mo20_LuongNhanVien'
)
BEGIN
    CREATE UNIQUE INDEX [IX_BangLuongs_NhanVienId_Nam_Thang] ON [BangLuongs] ([NhanVienId], [Nam], [Thang]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111151203_Mo20_LuongNhanVien'
)
BEGIN
    CREATE INDEX [IX_KhoanLuongs_NguoiTaoId] ON [KhoanLuongs] ([NguoiTaoId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111151203_Mo20_LuongNhanVien'
)
BEGIN
    CREATE INDEX [IX_KhoanLuongs_NhanVienId_Nam_Thang] ON [KhoanLuongs] ([NhanVienId], [Nam], [Thang]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111151203_Mo20_LuongNhanVien'
)
BEGIN
    CREATE UNIQUE INDEX [IX_MucLuongNhanViens_NhanVienId_ApDungTu] ON [MucLuongNhanViens] ([NhanVienId], [ApDungTu]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111151203_Mo20_LuongNhanVien'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261111151203_Mo20_LuongNhanVien', N'8.0.0');
END;
GO

COMMIT;
GO

