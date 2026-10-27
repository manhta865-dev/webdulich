BEGIN TRANSACTION;
GO

ALTER TABLE [Tours] ADD [NgungBan] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

ALTER TABLE [TaiKhoans] ADD [Email] nvarchar(256) NULL;
GO

ALTER TABLE [TaiKhoans] ADD [SoDienThoai] nvarchar(20) NULL;
GO

ALTER TABLE [LichKhoiHanhTours] ADD [SoChoToiDa] int NOT NULL DEFAULT 0;
GO

ALTER TABLE [LichKhoiHanhTours] ADD [SoDoChoNgoiId] int NULL;
GO

ALTER TABLE [HanhKhachDatTours] ADD [GiaLucDat] decimal(18,2) NOT NULL DEFAULT 0.0;
GO

ALTER TABLE [HanhKhachDatTours] ADD [MaThanhToan] nvarchar(30) NULL;
GO

ALTER TABLE [HanhKhachDatTours] ADD [NgayNha] datetime2 NULL;
GO

ALTER TABLE [HanhKhachDatTours] ADD [TaiKhoanId] int NULL;
GO

ALTER TABLE [DatTours] ADD [HanGiuCho] datetime2 NULL;
GO

ALTER TABLE [DatTours] ADD [LoaiDon] nvarchar(20) NOT NULL DEFAULT N'CaNhan';
GO

ALTER TABLE [DatTours] ADD [MaMoi] nvarchar(30) NULL;
GO

ALTER TABLE [DatTours] ADD [NgayDat] datetime2 NOT NULL DEFAULT (GETDATE());
GO

ALTER TABLE [DatTours] ADD [TrangThai] nvarchar(20) NOT NULL DEFAULT N'DaXacNhan';
GO

CREATE TABLE [GiaoDichThanhToans] (
    [Id] int NOT NULL IDENTITY,
    [DatTourId] int NULL,
    [HanhKhachDatTourId] int NULL,
    [NguoiXacNhanId] int NULL,
    [SoTien] decimal(18,2) NOT NULL,
    [PhuongThuc] nvarchar(50) NOT NULL,
    [TrangThai] nvarchar(20) NOT NULL,
    [MaThamChieuNganHang] nvarchar(100) NULL,
    [NgayTao] datetime2 NOT NULL,
    [NgayXacNhan] datetime2 NULL,
    CONSTRAINT [PK_GiaoDichThanhToans] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_GiaoDich_MotDoiTuong] CHECK (([DatTourId] IS NOT NULL AND [HanhKhachDatTourId] IS NULL) OR ([DatTourId] IS NULL AND [HanhKhachDatTourId] IS NOT NULL)),
    CONSTRAINT [CK_GiaoDich_SoTien] CHECK ([SoTien] > 0),
    CONSTRAINT [CK_GiaoDich_TrangThai] CHECK ([TrangThai] IN ('ChoXacNhan','ThanhCong','ThatBai')),
    CONSTRAINT [FK_GiaoDichThanhToans_DatTours_DatTourId] FOREIGN KEY ([DatTourId]) REFERENCES [DatTours] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_GiaoDichThanhToans_HanhKhachDatTours_HanhKhachDatTourId] FOREIGN KEY ([HanhKhachDatTourId]) REFERENCES [HanhKhachDatTours] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_GiaoDichThanhToans_TaiKhoans_NguoiXacNhanId] FOREIGN KEY ([NguoiXacNhanId]) REFERENCES [TaiKhoans] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [SoDoChoNgois] (
    [Id] int NOT NULL IDENTITY,
    [TenSoDo] nvarchar(100) NOT NULL,
    [PhuongTienId] int NULL,
    [SoHang] int NOT NULL,
    [SoCot] int NOT NULL,
    CONSTRAINT [PK_SoDoChoNgois] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_SoDoChoNgoi_KichThuoc] CHECK ([SoHang] > 0 AND [SoCot] > 0),
    CONSTRAINT [FK_SoDoChoNgois_PhuongTiens_PhuongTienId] FOREIGN KEY ([PhuongTienId]) REFERENCES [PhuongTiens] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [ViTriChoNgois] (
    [Id] int NOT NULL IDENTITY,
    [SoDoChoNgoiId] int NOT NULL,
    [MaViTri] nvarchar(10) NOT NULL,
    [Hang] int NOT NULL,
    [Cot] int NOT NULL,
    CONSTRAINT [PK_ViTriChoNgois] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ViTriChoNgois_SoDoChoNgois_SoDoChoNgoiId] FOREIGN KEY ([SoDoChoNgoiId]) REFERENCES [SoDoChoNgois] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [ChoNgoiChuyenDis] (
    [LichKhoiHanhTourId] int NOT NULL,
    [ViTriChoNgoiId] int NOT NULL,
    [HanhKhachDatTourId] int NOT NULL,
    CONSTRAINT [PK_ChoNgoiChuyenDis] PRIMARY KEY ([LichKhoiHanhTourId], [ViTriChoNgoiId]),
    CONSTRAINT [FK_ChoNgoiChuyenDis_HanhKhachDatTours_HanhKhachDatTourId] FOREIGN KEY ([HanhKhachDatTourId]) REFERENCES [HanhKhachDatTours] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ChoNgoiChuyenDis_LichKhoiHanhTours_LichKhoiHanhTourId] FOREIGN KEY ([LichKhoiHanhTourId]) REFERENCES [LichKhoiHanhTours] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ChoNgoiChuyenDis_ViTriChoNgois_ViTriChoNgoiId] FOREIGN KEY ([ViTriChoNgoiId]) REFERENCES [ViTriChoNgois] ([Id]) ON DELETE NO ACTION
);
GO

CREATE INDEX [IX_LichKhoiHanhTours_SoDoChoNgoiId] ON [LichKhoiHanhTours] ([SoDoChoNgoiId]);
GO

CREATE INDEX [IX_HanhKhachDatTours_TaiKhoanId] ON [HanhKhachDatTours] ([TaiKhoanId]);
GO

CREATE UNIQUE INDEX [IX_ChoNgoiChuyenDis_HanhKhachDatTourId] ON [ChoNgoiChuyenDis] ([HanhKhachDatTourId]);
GO

CREATE INDEX [IX_ChoNgoiChuyenDis_ViTriChoNgoiId] ON [ChoNgoiChuyenDis] ([ViTriChoNgoiId]);
GO

CREATE UNIQUE INDEX [IX_GiaoDichThanhToans_DatTourId] ON [GiaoDichThanhToans] ([DatTourId]) WHERE [TrangThai] = 'ThanhCong' AND [DatTourId] IS NOT NULL;
GO

CREATE UNIQUE INDEX [IX_GiaoDichThanhToans_HanhKhachDatTourId] ON [GiaoDichThanhToans] ([HanhKhachDatTourId]) WHERE [TrangThai] = 'ThanhCong' AND [HanhKhachDatTourId] IS NOT NULL;
GO

CREATE UNIQUE INDEX [IX_GiaoDichThanhToans_MaThamChieuNganHang] ON [GiaoDichThanhToans] ([MaThamChieuNganHang]) WHERE [MaThamChieuNganHang] IS NOT NULL;
GO

CREATE INDEX [IX_GiaoDichThanhToans_NguoiXacNhanId] ON [GiaoDichThanhToans] ([NguoiXacNhanId]);
GO

CREATE INDEX [IX_SoDoChoNgois_PhuongTienId] ON [SoDoChoNgois] ([PhuongTienId]);
GO

CREATE UNIQUE INDEX [IX_ViTriChoNgois_SoDoChoNgoiId_Hang_Cot] ON [ViTriChoNgois] ([SoDoChoNgoiId], [Hang], [Cot]);
GO

CREATE UNIQUE INDEX [IX_ViTriChoNgois_SoDoChoNgoiId_MaViTri] ON [ViTriChoNgois] ([SoDoChoNgoiId], [MaViTri]);
GO

ALTER TABLE [HanhKhachDatTours] ADD CONSTRAINT [FK_HanhKhachDatTours_TaiKhoans_TaiKhoanId] FOREIGN KEY ([TaiKhoanId]) REFERENCES [TaiKhoans] ([Id]);
GO

ALTER TABLE [LichKhoiHanhTours] ADD CONSTRAINT [FK_LichKhoiHanhTours_SoDoChoNgois_SoDoChoNgoiId] FOREIGN KEY ([SoDoChoNgoiId]) REFERENCES [SoDoChoNgois] ([Id]) ON DELETE NO ACTION;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261027151953_Mo1_MoRongMoHinhDuLieu', N'8.0.0');
GO

COMMIT;
GO

