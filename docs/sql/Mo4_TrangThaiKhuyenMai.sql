BEGIN TRANSACTION;
GO

UPDATE KhuyenMais
SET TrangThai = CASE WHEN TrangThai = 'TamAn' THEN 'TamNgung' ELSE 'HoatDong' END
WHERE TrangThai NOT IN ('HoatDong', 'TamNgung');
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261027164736_Mo4_TrangThaiKhuyenMai', N'8.0.0');
GO

COMMIT;
GO

