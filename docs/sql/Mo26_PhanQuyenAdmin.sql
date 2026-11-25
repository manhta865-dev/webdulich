-- Mo26: phân quyền quản trị theo nhóm chức năng (RB-51). Thêm bảng TaiKhoanQuyens(TaiKhoanId, MaQuyen),
-- rồi cấp đủ 6 nhóm quyền cho mọi tài khoản đang là admin để không ai mất quyền đang có.
-- Chạy trên host SAU Mo25_AnhDiemThamQuan.sql và TRƯỚC khi đưa code có phân quyền lên.
-- Bước 0. Sao lưu database trên trang quản lý host.
-- Bước 1. Chạy toàn bộ file này. Chạy lại cũng không sao (có kiểm __EFMigrationsHistory).
BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261125131539_Mo26_PhanQuyenAdmin'
)
BEGIN
    CREATE TABLE [TaiKhoanQuyens] (
        [TaiKhoanId] int NOT NULL,
        [MaQuyen] varchar(30) NOT NULL,
        CONSTRAINT [PK_TaiKhoanQuyens] PRIMARY KEY ([TaiKhoanId], [MaQuyen]),
        CONSTRAINT [CK_TaiKhoanQuyen_MaQuyen] CHECK ([MaQuyen] IN ('TourLich','DonThanhToan','DoanhThuTienVao','NoiDung','KhachNhanSu','TaiKhoanQuyen')),
        CONSTRAINT [FK_TaiKhoanQuyens_TaiKhoans_TaiKhoanId] FOREIGN KEY ([TaiKhoanId]) REFERENCES [TaiKhoans] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261125131539_Mo26_PhanQuyenAdmin'
)
BEGIN
    INSERT INTO [TaiKhoanQuyens] ([TaiKhoanId], [MaQuyen])
    SELECT t.[Id], q.[Ma]
    FROM [TaiKhoans] t
    CROSS JOIN (VALUES ('TourLich'), ('DonThanhToan'), ('DoanhThuTienVao'), ('NoiDung'), ('KhachNhanSu'), ('TaiKhoanQuyen')) q([Ma])
    WHERE t.[VaiTro] = 'admin'
      AND NOT EXISTS (SELECT 1 FROM [TaiKhoanQuyens] x WHERE x.[TaiKhoanId] = t.[Id] AND x.[MaQuyen] = q.[Ma]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261125131539_Mo26_PhanQuyenAdmin'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261125131539_Mo26_PhanQuyenAdmin', N'8.0.0');
END;
GO

COMMIT;
GO

