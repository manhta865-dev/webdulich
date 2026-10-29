BEGIN TRANSACTION;
GO

UPDATE LichKhoiHanhTours
SET TrangThai = CASE WHEN TrangThai = N'Đã hủy' THEN 'DaHuy' ELSE 'MoBan' END
WHERE TrangThai IS NULL OR TrangThai NOT IN ('MoBan', 'TamNgung', 'DaHuy');
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261027170837_Mo5_TrangThaiLich', N'8.0.0');
GO

COMMIT;
GO

