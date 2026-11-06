BEGIN TRANSACTION;
GO

ALTER TABLE [Tours] ADD [KinhDoXuatPhat] decimal(9,6) NULL;
GO

ALTER TABLE [Tours] ADD [ViDoXuatPhat] decimal(9,6) NULL;
GO

ALTER TABLE [LichTrinhTours] ADD [DuongDi] nvarchar(max) NULL;
GO

ALTER TABLE [Tours] ADD CONSTRAINT [CK_Tours_XuatPhat] CHECK (([ViDoXuatPhat] IS NULL AND [KinhDoXuatPhat] IS NULL) OR ([ViDoXuatPhat] IS NOT NULL AND [KinhDoXuatPhat] IS NOT NULL AND [ViDoXuatPhat] BETWEEN 8 AND 24 AND [KinhDoXuatPhat] BETWEEN 102 AND 110));
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261106124145_Mo11_XuatPhatVaDuongDi', N'8.0.0');
GO

COMMIT;
GO

