BEGIN TRANSACTION;
GO

UPDATE TaiKhoans
SET Email = LTRIM(RTRIM(TaiKhoanDangNhap))
WHERE Email IS NULL AND SoDienThoai IS NULL
  AND LTRIM(RTRIM(TaiKhoanDangNhap)) LIKE '%_@_%._%';
UPDATE TaiKhoans
SET SoDienThoai = LTRIM(RTRIM(TaiKhoanDangNhap))
WHERE Email IS NULL AND SoDienThoai IS NULL
  AND LTRIM(RTRIM(TaiKhoanDangNhap)) LIKE '0[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]';
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261027160159_Mo3_DongBoTaiKhoan', N'8.0.0');
GO

COMMIT;
GO

