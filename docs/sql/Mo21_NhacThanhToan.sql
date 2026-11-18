-- Mo21: bảng NhacThanhToans, lịch sử nhắc thanh toán đơn nhóm qua email (RB-47). Chỉ thêm bảng mới, không đổi bảng cũ.
-- Chạy trên host SAU Mo20 và TRƯỚC khi đưa code có tính năng nhắc lên.
-- Bước 0. Sao lưu database trên trang quản lý host.
-- Bước 1. Chạy toàn bộ file này. Chạy lại cũng không sao (có kiểm __EFMigrationsHistory).

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118124149_Mo21_NhacThanhToan'
)
BEGIN
    CREATE TABLE [NhacThanhToans] (
        [Id] int NOT NULL IDENTITY,
        [DatTourId] int NOT NULL,
        [TaiKhoanId] int NOT NULL,
        [HanDuocNhac] datetime2 NOT NULL,
        [EmailNhan] nvarchar(256) NOT NULL,
        [NgayTao] datetime2 NOT NULL,
        [NgayGui] datetime2 NULL,
        [TrangThai] nvarchar(10) NOT NULL,
        [SoLanThu] int NOT NULL,
        [Loi] nvarchar(500) NULL,
        CONSTRAINT [PK_NhacThanhToans] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Nhac_NgayGui] CHECK (([TrangThai] = 'DaGui' AND [NgayGui] IS NOT NULL) OR ([TrangThai] <> 'DaGui' AND [NgayGui] IS NULL)),
        CONSTRAINT [CK_Nhac_SoLanThu] CHECK ([SoLanThu] BETWEEN 1 AND 3),
        CONSTRAINT [CK_Nhac_TrangThai] CHECK ([TrangThai] IN ('ChoGui','DaGui','Loi')),
        CONSTRAINT [FK_NhacThanhToans_DatTours_DatTourId] FOREIGN KEY ([DatTourId]) REFERENCES [DatTours] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_NhacThanhToans_TaiKhoans_TaiKhoanId] FOREIGN KEY ([TaiKhoanId]) REFERENCES [TaiKhoans] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118124149_Mo21_NhacThanhToan'
)
BEGIN
    CREATE UNIQUE INDEX [IX_NhacThanhToans_DatTourId_TaiKhoanId_HanDuocNhac] ON [NhacThanhToans] ([DatTourId], [TaiKhoanId], [HanDuocNhac]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118124149_Mo21_NhacThanhToan'
)
BEGIN
    CREATE INDEX [IX_NhacThanhToans_TaiKhoanId] ON [NhacThanhToans] ([TaiKhoanId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118124149_Mo21_NhacThanhToan'
)
BEGIN
    CREATE INDEX [IX_NhacThanhToans_TrangThai] ON [NhacThanhToans] ([TrangThai]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118124149_Mo21_NhacThanhToan'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261118124149_Mo21_NhacThanhToan', N'8.0.0');
END;
GO

COMMIT;
GO

