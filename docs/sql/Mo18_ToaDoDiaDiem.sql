BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261109154239_Mo18_ToaDoDiaDiem'
)
BEGIN
    ALTER TABLE [DiaDiems] ADD [KinhDo] decimal(9,6) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261109154239_Mo18_ToaDoDiaDiem'
)
BEGIN
    ALTER TABLE [DiaDiems] ADD [ViDo] decimal(9,6) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261109154239_Mo18_ToaDoDiaDiem'
)
BEGIN
    EXEC(N'ALTER TABLE [DiaDiems] ADD CONSTRAINT [CK_DiaDiem_ToaDo] CHECK (([ViDo] IS NULL AND [KinhDo] IS NULL) OR ([ViDo] IS NOT NULL AND [KinhDo] IS NOT NULL AND [ViDo] BETWEEN -90 AND 90 AND [KinhDo] BETWEEN -180 AND 180))');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261109154239_Mo18_ToaDoDiaDiem'
)
BEGIN
    UPDATE DiaDiems SET ViDo = 21.028, KinhDo = 105.854 WHERE ViDo IS NULL AND LTRIM(RTRIM(TenDiaDiem)) COLLATE Latin1_General_CI_AI IN (N'Hà Nội', N'Ha Noi')
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261109154239_Mo18_ToaDoDiaDiem'
)
BEGIN
    UPDATE DiaDiems SET ViDo = 20.951, KinhDo = 107.080 WHERE ViDo IS NULL AND LTRIM(RTRIM(TenDiaDiem)) COLLATE Latin1_General_CI_AI IN (N'Hạ Long', N'Ha Long')
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261109154239_Mo18_ToaDoDiaDiem'
)
BEGIN
    UPDATE DiaDiems SET ViDo = 22.336, KinhDo = 103.844 WHERE ViDo IS NULL AND LTRIM(RTRIM(TenDiaDiem)) COLLATE Latin1_General_CI_AI IN (N'Sapa', N'Sa Pa')
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261109154239_Mo18_ToaDoDiaDiem'
)
BEGIN
    UPDATE DiaDiems SET ViDo = 22.823, KinhDo = 104.984 WHERE ViDo IS NULL AND LTRIM(RTRIM(TenDiaDiem)) COLLATE Latin1_General_CI_AI IN (N'Hà Giang', N'Ha Giang')
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261109154239_Mo18_ToaDoDiaDiem'
)
BEGIN
    UPDATE DiaDiems SET ViDo = 20.251, KinhDo = 105.975 WHERE ViDo IS NULL AND LTRIM(RTRIM(TenDiaDiem)) COLLATE Latin1_General_CI_AI IN (N'Ninh Bình', N'Ninh Binh')
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261109154239_Mo18_ToaDoDiaDiem'
)
BEGIN
    UPDATE DiaDiems SET ViDo = 17.468, KinhDo = 106.623 WHERE ViDo IS NULL AND LTRIM(RTRIM(TenDiaDiem)) COLLATE Latin1_General_CI_AI IN (N'Quảng Bình', N'Quang Binh')
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261109154239_Mo18_ToaDoDiaDiem'
)
BEGIN
    UPDATE DiaDiems SET ViDo = 16.463, KinhDo = 107.590 WHERE ViDo IS NULL AND LTRIM(RTRIM(TenDiaDiem)) COLLATE Latin1_General_CI_AI IN (N'Huế', N'Hue')
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261109154239_Mo18_ToaDoDiaDiem'
)
BEGIN
    UPDATE DiaDiems SET ViDo = 16.054, KinhDo = 108.202 WHERE ViDo IS NULL AND LTRIM(RTRIM(TenDiaDiem)) COLLATE Latin1_General_CI_AI IN (N'Đà Nẵng', N'Da Nang')
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261109154239_Mo18_ToaDoDiaDiem'
)
BEGIN
    UPDATE DiaDiems SET ViDo = 15.880, KinhDo = 108.335 WHERE ViDo IS NULL AND LTRIM(RTRIM(TenDiaDiem)) COLLATE Latin1_General_CI_AI IN (N'Hội An', N'Hoi An')
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261109154239_Mo18_ToaDoDiaDiem'
)
BEGIN
    UPDATE DiaDiems SET ViDo = 12.238, KinhDo = 109.196 WHERE ViDo IS NULL AND LTRIM(RTRIM(TenDiaDiem)) COLLATE Latin1_General_CI_AI IN (N'Nha Trang', N'Nha Trang')
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261109154239_Mo18_ToaDoDiaDiem'
)
BEGIN
    UPDATE DiaDiems SET ViDo = 11.940, KinhDo = 108.458 WHERE ViDo IS NULL AND LTRIM(RTRIM(TenDiaDiem)) COLLATE Latin1_General_CI_AI IN (N'Đà Lạt', N'Da Lat')
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261109154239_Mo18_ToaDoDiaDiem'
)
BEGIN
    UPDATE DiaDiems SET ViDo = 10.346, KinhDo = 107.084 WHERE ViDo IS NULL AND LTRIM(RTRIM(TenDiaDiem)) COLLATE Latin1_General_CI_AI IN (N'Vũng Tàu', N'Vung Tau')
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261109154239_Mo18_ToaDoDiaDiem'
)
BEGIN
    UPDATE DiaDiems SET ViDo = 11.310, KinhDo = 106.098 WHERE ViDo IS NULL AND LTRIM(RTRIM(TenDiaDiem)) COLLATE Latin1_General_CI_AI IN (N'Tây Ninh', N'Tay Ninh')
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261109154239_Mo18_ToaDoDiaDiem'
)
BEGIN
    UPDATE DiaDiems SET ViDo = 10.241, KinhDo = 106.376 WHERE ViDo IS NULL AND LTRIM(RTRIM(TenDiaDiem)) COLLATE Latin1_General_CI_AI IN (N'Bến Tre', N'Ben Tre')
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261109154239_Mo18_ToaDoDiaDiem'
)
BEGIN
    UPDATE DiaDiems SET ViDo = 10.521, KinhDo = 105.125 WHERE ViDo IS NULL AND LTRIM(RTRIM(TenDiaDiem)) COLLATE Latin1_General_CI_AI IN (N'An Giang', N'An Giang')
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261109154239_Mo18_ToaDoDiaDiem'
)
BEGIN
    UPDATE DiaDiems SET ViDo = 10.289, KinhDo = 103.984 WHERE ViDo IS NULL AND LTRIM(RTRIM(TenDiaDiem)) COLLATE Latin1_General_CI_AI IN (N'Phú Quốc', N'Phu Quoc')
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261109154239_Mo18_ToaDoDiaDiem'
)
BEGIN
    UPDATE DiaDiems SET ViDo = 10.045, KinhDo = 105.747 WHERE ViDo IS NULL AND LTRIM(RTRIM(TenDiaDiem)) COLLATE Latin1_General_CI_AI IN (N'Cần Thơ', N'Can Tho')
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261109154239_Mo18_ToaDoDiaDiem'
)
BEGIN
    UPDATE DiaDiems SET ViDo = 8.683, KinhDo = 106.609 WHERE ViDo IS NULL AND LTRIM(RTRIM(TenDiaDiem)) COLLATE Latin1_General_CI_AI IN (N'Côn Đảo', N'Con Dao')
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261109154239_Mo18_ToaDoDiaDiem'
)
BEGIN
    UPDATE DiaDiems SET ViDo = 34.052, KinhDo = -118.244 WHERE ViDo IS NULL AND LTRIM(RTRIM(TenDiaDiem)) COLLATE Latin1_General_CI_AI IN (N'USA', N'Mỹ')
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261109154239_Mo18_ToaDoDiaDiem'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261109154239_Mo18_ToaDoDiaDiem', N'8.0.0');
END;
GO

COMMIT;
GO

