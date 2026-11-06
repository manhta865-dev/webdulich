BEGIN TRANSACTION;
GO

ALTER TABLE [LichTrinhTours] ADD [KinhDo] decimal(9,6) NULL;
GO

ALTER TABLE [LichTrinhTours] ADD [ViDo] decimal(9,6) NULL;
GO

ALTER TABLE [LichTrinhTours] ADD CONSTRAINT [CK_LichTrinh_ToaDo] CHECK (([ViDo] IS NULL AND [KinhDo] IS NULL) OR ([ViDo] IS NOT NULL AND [KinhDo] IS NOT NULL AND [ViDo] BETWEEN 8 AND 24 AND [KinhDo] BETWEEN 102 AND 110));
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261105172301_Mo10_ToaDoLichTrinh', N'8.0.0');
GO

COMMIT;
GO

