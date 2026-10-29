BEGIN TRANSACTION;
GO

IF EXISTS (SELECT 1 FROM DatTours WHERE LichKhoiHanhTourId IS NULL)
    THROW 50001, N'Còn đơn chưa gắn lịch khởi hành (truy vấn C trong doi_chieu.sql).', 1;
IF EXISTS (SELECT MaChuyenDi FROM DatTours GROUP BY MaChuyenDi HAVING COUNT(*) > 1)
    THROW 50002, N'Có mã chuyến đi bị trùng.', 1;
IF EXISTS (SELECT 1 FROM DatTours WHERE LEN(MaChuyenDi) > 30)
    THROW 50003, N'Có mã chuyến đi dài hơn 30 ký tự.', 1;
IF EXISTS (SELECT 1 FROM TinNhanLienHes WHERE TaiKhoanId IS NULL)
    THROW 50004, N'Còn tin nhắn không gắn tài khoản.', 1;
IF EXISTS (SELECT 1 FROM TaiKhoans WHERE Email IS NULL AND SoDienThoai IS NULL)
    THROW 50005, N'Còn tài khoản chưa có email lẫn số điện thoại (truy vấn A).', 1;
IF EXISTS (SELECT Email FROM TaiKhoans WHERE Email IS NOT NULL GROUP BY Email HAVING COUNT(*) > 1)
    THROW 50006, N'Có email trùng giữa các tài khoản.', 1;
IF EXISTS (SELECT SoDienThoai FROM TaiKhoans WHERE SoDienThoai IS NOT NULL GROUP BY SoDienThoai HAVING COUNT(*) > 1)
    THROW 50007, N'Có số điện thoại trùng giữa các tài khoản.', 1;
IF EXISTS (SELECT 1 FROM LichKhoiHanhTours WHERE SoChoCon < 0 OR SoChoCon > SoChoToiDa)
    THROW 50008, N'Có lịch có số chỗ còn ngoài khoảng 0 đến số chỗ tối đa (truy vấn F).', 1;
IF EXISTS (SELECT 1 FROM LichKhoiHanhTours WHERE TrangThai NOT IN ('MoBan','TamNgung','DaHuy'))
    THROW 50009, N'Có lịch mang trạng thái cũ (chạy migration Mo5 trước).', 1;
IF EXISTS (SELECT 1 FROM KhuyenMais WHERE TrangThai NOT IN ('HoatDong','TamNgung'))
    THROW 50010, N'Có khuyến mãi mang trạng thái cũ (chạy migration Mo4 trước).', 1;
IF EXISTS (SELECT 1 FROM HanhKhachDatTours WHERE LoaiHanhKhach NOT IN (N'Người lớn', N'Trẻ nhỏ'))
    THROW 50011, N'Có hành khách mang loại khác Người lớn / Trẻ nhỏ.', 1;
IF EXISTS (SELECT 1 FROM DatTours WHERE TrangThai NOT IN ('GiuCho','DaXacNhan','DaHuy') OR LoaiDon NOT IN ('CaNhan','Nhom'))
    THROW 50012, N'Có đơn mang trạng thái hoặc loại đơn không hợp lệ.', 1;
GO

ALTER TABLE [DatTours] DROP CONSTRAINT [FK_DatTours_KhuyenMais_KhuyenMaiId];
GO

ALTER TABLE [DatTours] DROP CONSTRAINT [FK_DatTours_LichKhoiHanhTours_LichKhoiHanhTourId];
GO

ALTER TABLE [DatTours] DROP CONSTRAINT [FK_DatTours_TaiKhoans_TaiKhoanId];
GO

DECLARE @sql nvarchar(max) = N'';
SELECT @sql += N'ALTER TABLE [DatTours] DROP CONSTRAINT [' + f.name + N'];'
FROM sys.foreign_keys f
JOIN sys.foreign_key_columns fc ON fc.constraint_object_id = f.object_id
WHERE f.parent_object_id = OBJECT_ID(N'DatTours') AND COL_NAME(fc.parent_object_id, fc.parent_column_id) = N'TourId';
SELECT @sql += N'DROP INDEX [' + i.name + N'] ON [DatTours];'
FROM sys.indexes i
JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
WHERE i.object_id = OBJECT_ID(N'DatTours') AND i.is_primary_key = 0 AND COL_NAME(ic.object_id, ic.column_id) = N'TourId';
IF OBJECT_ID(N'FK_DatTours_TaiKhoans', N'F') IS NOT NULL
    SET @sql += N'ALTER TABLE [DatTours] DROP CONSTRAINT [FK_DatTours_TaiKhoans];';
EXEC sp_executesql @sql;
GO

ALTER TABLE [LichKhoiHanhTours] DROP CONSTRAINT [FK_LichKhoiHanhTours_Tours_TourId];
GO

ALTER TABLE [TinNhanLienHes] DROP CONSTRAINT [FK_TinNhanLienHes_TaiKhoans_TaiKhoanId];
GO

DECLARE @var0 sysname;
SELECT @var0 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[TinNhanLienHes]') AND [c].[name] = N'Email');
IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [TinNhanLienHes] DROP CONSTRAINT [' + @var0 + '];');
ALTER TABLE [TinNhanLienHes] DROP COLUMN [Email];
GO

DECLARE @var1 sysname;
SELECT @var1 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[TinNhanLienHes]') AND [c].[name] = N'HoTen');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [TinNhanLienHes] DROP CONSTRAINT [' + @var1 + '];');
ALTER TABLE [TinNhanLienHes] DROP COLUMN [HoTen];
GO

DECLARE @var2 sysname;
SELECT @var2 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[TaiKhoans]') AND [c].[name] = N'TaiKhoanDangNhap');
IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [TaiKhoans] DROP CONSTRAINT [' + @var2 + '];');
ALTER TABLE [TaiKhoans] DROP COLUMN [TaiKhoanDangNhap];
GO

DECLARE @var3 sysname;
SELECT @var3 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[NhanViens]') AND [c].[name] = N'HoTen');
IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [NhanViens] DROP CONSTRAINT [' + @var3 + '];');
ALTER TABLE [NhanViens] DROP COLUMN [HoTen];
GO

DECLARE @var4 sysname;
SELECT @var4 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[NhanViens]') AND [c].[name] = N'LoaiNhanVien');
IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [NhanViens] DROP CONSTRAINT [' + @var4 + '];');
ALTER TABLE [NhanViens] DROP COLUMN [LoaiNhanVien];
GO

DECLARE @var5 sysname;
SELECT @var5 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[NhanViens]') AND [c].[name] = N'SoDienThoai');
IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [NhanViens] DROP CONSTRAINT [' + @var5 + '];');
ALTER TABLE [NhanViens] DROP COLUMN [SoDienThoai];
GO

DECLARE @var6 sysname;
SELECT @var6 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[KhuyenMais]') AND [c].[name] = N'SoLuotDaDung');
IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [KhuyenMais] DROP CONSTRAINT [' + @var6 + '];');
ALTER TABLE [KhuyenMais] DROP COLUMN [SoLuotDaDung];
GO

DECLARE @var7 sysname;
SELECT @var7 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[DatTours]') AND [c].[name] = N'MaKhuyenMai');
IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [DatTours] DROP CONSTRAINT [' + @var7 + '];');
ALTER TABLE [DatTours] DROP COLUMN [MaKhuyenMai];
GO

DECLARE @var8 sysname;
SELECT @var8 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[DatTours]') AND [c].[name] = N'NgayDi');
IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [DatTours] DROP CONSTRAINT [' + @var8 + '];');
ALTER TABLE [DatTours] DROP COLUMN [NgayDi];
GO

DECLARE @var9 sysname;
SELECT @var9 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[DatTours]') AND [c].[name] = N'PhuongThucThanhToan');
IF @var9 IS NOT NULL EXEC(N'ALTER TABLE [DatTours] DROP CONSTRAINT [' + @var9 + '];');
ALTER TABLE [DatTours] DROP COLUMN [PhuongThucThanhToan];
GO

DECLARE @var10 sysname;
SELECT @var10 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[DatTours]') AND [c].[name] = N'SoNguoi');
IF @var10 IS NOT NULL EXEC(N'ALTER TABLE [DatTours] DROP CONSTRAINT [' + @var10 + '];');
ALTER TABLE [DatTours] DROP COLUMN [SoNguoi];
GO

DECLARE @var11 sysname;
SELECT @var11 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[DatTours]') AND [c].[name] = N'TongTien');
IF @var11 IS NOT NULL EXEC(N'ALTER TABLE [DatTours] DROP CONSTRAINT [' + @var11 + '];');
ALTER TABLE [DatTours] DROP COLUMN [TongTien];
GO

DECLARE @var12 sysname;
SELECT @var12 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[DatTours]') AND [c].[name] = N'TongTienTruocGiam');
IF @var12 IS NOT NULL EXEC(N'ALTER TABLE [DatTours] DROP CONSTRAINT [' + @var12 + '];');
ALTER TABLE [DatTours] DROP COLUMN [TongTienTruocGiam];
GO

DECLARE @var13 sysname;
SELECT @var13 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[DatTours]') AND [c].[name] = N'TourId');
IF @var13 IS NOT NULL EXEC(N'ALTER TABLE [DatTours] DROP CONSTRAINT [' + @var13 + '];');
ALTER TABLE [DatTours] DROP COLUMN [TourId];
GO

DECLARE @var14 sysname;
SELECT @var14 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[DatTours]') AND [c].[name] = N'TrangThaiThanhToan');
IF @var14 IS NOT NULL EXEC(N'ALTER TABLE [DatTours] DROP CONSTRAINT [' + @var14 + '];');
ALTER TABLE [DatTours] DROP COLUMN [TrangThaiThanhToan];
GO

DROP INDEX [IX_TinNhanLienHes_TaiKhoanId] ON [TinNhanLienHes];
DECLARE @var15 sysname;
SELECT @var15 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[TinNhanLienHes]') AND [c].[name] = N'TaiKhoanId');
IF @var15 IS NOT NULL EXEC(N'ALTER TABLE [TinNhanLienHes] DROP CONSTRAINT [' + @var15 + '];');
UPDATE [TinNhanLienHes] SET [TaiKhoanId] = 0 WHERE [TaiKhoanId] IS NULL;
ALTER TABLE [TinNhanLienHes] ALTER COLUMN [TaiKhoanId] int NOT NULL;
ALTER TABLE [TinNhanLienHes] ADD DEFAULT 0 FOR [TaiKhoanId];
CREATE INDEX [IX_TinNhanLienHes_TaiKhoanId] ON [TinNhanLienHes] ([TaiKhoanId]);
GO

DECLARE @var16 sysname;
SELECT @var16 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[LichKhoiHanhTours]') AND [c].[name] = N'TrangThai');
IF @var16 IS NOT NULL EXEC(N'ALTER TABLE [LichKhoiHanhTours] DROP CONSTRAINT [' + @var16 + '];');
ALTER TABLE [LichKhoiHanhTours] ALTER COLUMN [TrangThai] nvarchar(20) NOT NULL;
GO

DECLARE @var17 sysname;
SELECT @var17 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[KhuyenMais]') AND [c].[name] = N'TrangThai');
IF @var17 IS NOT NULL EXEC(N'ALTER TABLE [KhuyenMais] DROP CONSTRAINT [' + @var17 + '];');
ALTER TABLE [KhuyenMais] ALTER COLUMN [TrangThai] nvarchar(20) NOT NULL;
GO

DECLARE @var18 sysname;
SELECT @var18 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[HanhKhachDatTours]') AND [c].[name] = N'SoGiayTo');
IF @var18 IS NOT NULL EXEC(N'ALTER TABLE [HanhKhachDatTours] DROP CONSTRAINT [' + @var18 + '];');
ALTER TABLE [HanhKhachDatTours] ALTER COLUMN [SoGiayTo] nvarchar(max) NULL;
GO

DECLARE @var19 sysname;
SELECT @var19 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[HanhKhachDatTours]') AND [c].[name] = N'LoaiHanhKhach');
IF @var19 IS NOT NULL EXEC(N'ALTER TABLE [HanhKhachDatTours] DROP CONSTRAINT [' + @var19 + '];');
ALTER TABLE [HanhKhachDatTours] ALTER COLUMN [LoaiHanhKhach] nvarchar(20) NOT NULL;
GO

DECLARE @var20 sysname;
SELECT @var20 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[HanhKhachDatTours]') AND [c].[name] = N'LoaiGiayTo');
IF @var20 IS NOT NULL EXEC(N'ALTER TABLE [HanhKhachDatTours] DROP CONSTRAINT [' + @var20 + '];');
ALTER TABLE [HanhKhachDatTours] ALTER COLUMN [LoaiGiayTo] nvarchar(max) NULL;
GO

DECLARE @var21 sysname;
SELECT @var21 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[HanhKhachDatTours]') AND [c].[name] = N'HoTen');
IF @var21 IS NOT NULL EXEC(N'ALTER TABLE [HanhKhachDatTours] DROP CONSTRAINT [' + @var21 + '];');
ALTER TABLE [HanhKhachDatTours] ALTER COLUMN [HoTen] nvarchar(max) NULL;
GO

DECLARE @var22 sysname;
SELECT @var22 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[DatTours]') AND [c].[name] = N'TrangThai');
IF @var22 IS NOT NULL EXEC(N'ALTER TABLE [DatTours] DROP CONSTRAINT [' + @var22 + '];');
ALTER TABLE [DatTours] ADD DEFAULT N'GiuCho' FOR [TrangThai];
GO

DECLARE @var23 sysname;
SELECT @var23 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[DatTours]') AND [c].[name] = N'MaChuyenDi');
IF @var23 IS NOT NULL EXEC(N'ALTER TABLE [DatTours] DROP CONSTRAINT [' + @var23 + '];');
ALTER TABLE [DatTours] ALTER COLUMN [MaChuyenDi] nvarchar(30) NOT NULL;
GO

DROP INDEX [IX_DatTours_LichKhoiHanhTourId] ON [DatTours];
DECLARE @var24 sysname;
SELECT @var24 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[DatTours]') AND [c].[name] = N'LichKhoiHanhTourId');
IF @var24 IS NOT NULL EXEC(N'ALTER TABLE [DatTours] DROP CONSTRAINT [' + @var24 + '];');
UPDATE [DatTours] SET [LichKhoiHanhTourId] = 0 WHERE [LichKhoiHanhTourId] IS NULL;
ALTER TABLE [DatTours] ALTER COLUMN [LichKhoiHanhTourId] int NOT NULL;
ALTER TABLE [DatTours] ADD DEFAULT 0 FOR [LichKhoiHanhTourId];
CREATE INDEX [IX_DatTours_LichKhoiHanhTourId] ON [DatTours] ([LichKhoiHanhTourId]);
GO

CREATE UNIQUE INDEX [IX_TaiKhoans_Email] ON [TaiKhoans] ([Email]) WHERE [Email] IS NOT NULL;
GO

CREATE UNIQUE INDEX [IX_TaiKhoans_SoDienThoai] ON [TaiKhoans] ([SoDienThoai]) WHERE [SoDienThoai] IS NOT NULL;
GO

ALTER TABLE [TaiKhoans] ADD CONSTRAINT [CK_TaiKhoan_DinhDanh] CHECK ([Email] IS NOT NULL OR [SoDienThoai] IS NOT NULL);
GO

ALTER TABLE [LichKhoiHanhTours] ADD CONSTRAINT [CK_Lich_SoCho] CHECK ([SoChoCon] >= 0 AND [SoChoCon] <= [SoChoToiDa]);
GO

ALTER TABLE [LichKhoiHanhTours] ADD CONSTRAINT [CK_Lich_TrangThai] CHECK ([TrangThai] IN ('MoBan','TamNgung','DaHuy'));
GO

ALTER TABLE [KhuyenMais] ADD CONSTRAINT [CK_KhuyenMai_TrangThai] CHECK ([TrangThai] IN ('HoatDong','TamNgung'));
GO

CREATE UNIQUE INDEX [IX_HanhKhachDatTours_DatTourId_TaiKhoanId] ON [HanhKhachDatTours] ([DatTourId], [TaiKhoanId]) WHERE [TaiKhoanId] IS NOT NULL;
GO

CREATE UNIQUE INDEX [IX_HanhKhachDatTours_MaThanhToan] ON [HanhKhachDatTours] ([MaThanhToan]) WHERE [MaThanhToan] IS NOT NULL;
GO

ALTER TABLE [HanhKhachDatTours] ADD CONSTRAINT [CK_HanhKhach_GiaLucDat] CHECK ([GiaLucDat] >= 0);
GO

ALTER TABLE [HanhKhachDatTours] ADD CONSTRAINT [CK_HanhKhach_Loai] CHECK ([LoaiHanhKhach] IN (N'Người lớn', N'Trẻ nhỏ'));
GO

CREATE UNIQUE INDEX [IX_DatTours_MaChuyenDi] ON [DatTours] ([MaChuyenDi]);
GO

CREATE UNIQUE INDEX [IX_DatTours_MaMoi] ON [DatTours] ([MaMoi]) WHERE [MaMoi] IS NOT NULL;
GO

ALTER TABLE [DatTours] ADD CONSTRAINT [CK_DatTours_LoaiDon] CHECK ([LoaiDon] IN ('CaNhan','Nhom'));
GO

ALTER TABLE [DatTours] ADD CONSTRAINT [CK_DatTours_SoTienGiam] CHECK ([SoTienGiam] >= 0);
GO

ALTER TABLE [DatTours] ADD CONSTRAINT [CK_DatTours_TrangThai] CHECK ([TrangThai] IN ('GiuCho','DaXacNhan','DaHuy'));
GO

ALTER TABLE [DatTours] ADD CONSTRAINT [FK_DatTours_KhuyenMais_KhuyenMaiId] FOREIGN KEY ([KhuyenMaiId]) REFERENCES [KhuyenMais] ([Id]) ON DELETE NO ACTION;
GO

ALTER TABLE [DatTours] ADD CONSTRAINT [FK_DatTours_LichKhoiHanhTours_LichKhoiHanhTourId] FOREIGN KEY ([LichKhoiHanhTourId]) REFERENCES [LichKhoiHanhTours] ([Id]) ON DELETE NO ACTION;
GO

ALTER TABLE [DatTours] ADD CONSTRAINT [FK_DatTours_TaiKhoans_TaiKhoanId] FOREIGN KEY ([TaiKhoanId]) REFERENCES [TaiKhoans] ([Id]) ON DELETE NO ACTION;
GO

ALTER TABLE [LichKhoiHanhTours] ADD CONSTRAINT [FK_LichKhoiHanhTours_Tours_TourId] FOREIGN KEY ([TourId]) REFERENCES [Tours] ([Id]) ON DELETE NO ACTION;
GO

ALTER TABLE [TinNhanLienHes] ADD CONSTRAINT [FK_TinNhanLienHes_TaiKhoans_TaiKhoanId] FOREIGN KEY ([TaiKhoanId]) REFERENCES [TaiKhoans] ([Id]) ON DELETE CASCADE;
GO

UPDATE HanhKhachDatTours SET HoTen = NULL WHERE HoTen = N'';
UPDATE HanhKhachDatTours SET LoaiGiayTo = NULL WHERE LoaiGiayTo = N'';
UPDATE HanhKhachDatTours SET SoGiayTo = NULL WHERE SoGiayTo = N'';
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261029151459_Mo6_ThuHepMoHinh', N'8.0.0');
GO

COMMIT;
GO

