-- Mo22: bảng GiaoDichNganHangs, các khoản tiền vào do SePay báo về để tự xác nhận chuyển khoản (RB-48). Chỉ thêm bảng mới.
-- Chạy trên host SAU Mo21 và TRƯỚC khi đưa code có tính năng tự xác nhận lên.
-- Bước 0. Sao lưu database trên trang quản lý host.
-- Bước 1. Chạy toàn bộ file này. Chạy lại cũng không sao (có kiểm __EFMigrationsHistory).

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118144719_Mo22_GiaoDichNganHang'
)
BEGIN
    CREATE TABLE [GiaoDichNganHangs] (
        [Id] int NOT NULL IDENTITY,
        [Nguon] nvarchar(20) NOT NULL,
        [MaGiaoDichNguon] nvarchar(64) NOT NULL,
        [NganHang] nvarchar(50) NULL,
        [SoTaiKhoan] nvarchar(30) NULL,
        [ThoiGianGiaoDich] datetime2 NOT NULL,
        [SoTien] decimal(18,2) NOT NULL,
        [NoiDung] nvarchar(500) NOT NULL,
        [MaThamChieu] nvarchar(100) NULL,
        [NgayNhan] datetime2 NOT NULL,
        [KetQua] nvarchar(20) NOT NULL,
        [GiaoDichThanhToanId] int NULL,
        [NguoiXuLyId] int NULL,
        [NgayXuLy] datetime2 NULL,
        [GhiChuXuLy] nvarchar(300) NULL,
        CONSTRAINT [PK_GiaoDichNganHangs] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_TienVao_KetQua] CHECK ([KetQua] IN ('DaKhop','KhongTimThayMa','SaiSoTien','DaTraRoi','GheDaNha','DonDaHuy')),
        CONSTRAINT [CK_TienVao_Khop] CHECK (([KetQua] = 'DaKhop' AND [GiaoDichThanhToanId] IS NOT NULL) OR ([KetQua] <> 'DaKhop' AND [GiaoDichThanhToanId] IS NULL)),
        CONSTRAINT [CK_TienVao_Nguon] CHECK ([Nguon] IN ('SePay','GiaLap')),
        CONSTRAINT [CK_TienVao_SoTien] CHECK ([SoTien] > 0),
        CONSTRAINT [CK_TienVao_XuLy] CHECK (([NguoiXuLyId] IS NULL AND [NgayXuLy] IS NULL) OR ([NguoiXuLyId] IS NOT NULL AND [NgayXuLy] IS NOT NULL)),
        CONSTRAINT [FK_GiaoDichNganHangs_GiaoDichThanhToans_GiaoDichThanhToanId] FOREIGN KEY ([GiaoDichThanhToanId]) REFERENCES [GiaoDichThanhToans] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_GiaoDichNganHangs_TaiKhoans_NguoiXuLyId] FOREIGN KEY ([NguoiXuLyId]) REFERENCES [TaiKhoans] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118144719_Mo22_GiaoDichNganHang'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_GiaoDichNganHangs_GiaoDichThanhToanId] ON [GiaoDichNganHangs] ([GiaoDichThanhToanId]) WHERE [GiaoDichThanhToanId] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118144719_Mo22_GiaoDichNganHang'
)
BEGIN
    CREATE INDEX [IX_GiaoDichNganHangs_KetQua_NgayNhan] ON [GiaoDichNganHangs] ([KetQua], [NgayNhan]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118144719_Mo22_GiaoDichNganHang'
)
BEGIN
    CREATE INDEX [IX_GiaoDichNganHangs_NguoiXuLyId] ON [GiaoDichNganHangs] ([NguoiXuLyId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118144719_Mo22_GiaoDichNganHang'
)
BEGIN
    CREATE UNIQUE INDEX [IX_GiaoDichNganHangs_Nguon_MaGiaoDichNguon] ON [GiaoDichNganHangs] ([Nguon], [MaGiaoDichNguon]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118144719_Mo22_GiaoDichNganHang'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261118144719_Mo22_GiaoDichNganHang', N'8.0.0');
END;
GO

COMMIT;
GO

