BEGIN TRANSACTION;
GO

DECLARE @xe int = (SELECT TOP 1 Id FROM PhuongTiens WHERE TenPhuongTien LIKE N'Xe%' ORDER BY Id);
DECLARE @maybay int = (SELECT TOP 1 Id FROM PhuongTiens WHERE TenPhuongTien LIKE N'Máy bay%' ORDER BY Id);
-- Mỗi dòng: tên, phương tiện, số hàng, số cột, cột lối đi (0 = không có), số hàng thường; hàng cuối (nếu khác) đủ mọi cột
DECLARE @mau TABLE (Ten nvarchar(100), PhuongTienId int NULL, SoHang int, SoCot int, CotLoiDi int, HangCuoiDayDu bit);
INSERT INTO @mau VALUES
    (N'Xe 16 chỗ', @xe, 5, 4, 3, 1),
    (N'Xe 29 chỗ', @xe, 7, 5, 3, 1),
    (N'Xe 45 chỗ', @xe, 11, 5, 3, 1),
    (N'Máy bay 180 chỗ (3-3)', @maybay, 30, 7, 4, 0);
DECLARE @ten nvarchar(100), @pt int, @soHang int, @soCot int, @loiDi int, @cuoiDu bit, @soDoId int;
DECLARE c CURSOR LOCAL FAST_FORWARD FOR SELECT Ten, PhuongTienId, SoHang, SoCot, CotLoiDi, HangCuoiDayDu FROM @mau;
OPEN c;
FETCH NEXT FROM c INTO @ten, @pt, @soHang, @soCot, @loiDi, @cuoiDu;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF NOT EXISTS (SELECT 1 FROM SoDoChoNgois WHERE TenSoDo = @ten)
    BEGIN
        INSERT INTO SoDoChoNgois (TenSoDo, PhuongTienId, SoHang, SoCot) VALUES (@ten, @pt, @soHang, @soCot);
        SET @soDoId = SCOPE_IDENTITY();
        DECLARE @hang int = 1;
        WHILE @hang <= @soHang
        BEGIN
            DECLARE @cot int = 1, @thuTu int = 0;
            WHILE @cot <= @soCot
            BEGIN
                -- Hàng cuối của xe có ghế ở cả cột lối đi; các hàng khác bỏ cột lối đi
                IF @cot <> @loiDi OR (@cuoiDu = 1 AND @hang = @soHang)
                BEGIN
                    SET @thuTu += 1;
                    INSERT INTO ViTriChoNgois (SoDoChoNgoiId, MaViTri, Hang, Cot)
                    VALUES (@soDoId, CONCAT(@hang, CHAR(64 + @thuTu)), @hang, @cot);
                END
                SET @cot += 1;
            END
            SET @hang += 1;
        END
    END
    FETCH NEXT FROM c INTO @ten, @pt, @soHang, @soCot, @loiDi, @cuoiDu;
END
CLOSE c; DEALLOCATE c;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261029162237_Mo7_SoDoChoNgoiMau', N'8.0.0');
GO

COMMIT;
GO

