BEGIN TRANSACTION;
GO

ALTER TABLE [GiaoDichThanhToans] ADD [NguoiTraId] int NULL;
GO

ALTER TABLE [DatTours] ADD [DaGiaHan] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

CREATE INDEX [IX_GiaoDichThanhToans_NguoiTraId] ON [GiaoDichThanhToans] ([NguoiTraId]);
GO

ALTER TABLE [GiaoDichThanhToans] ADD CONSTRAINT [FK_GiaoDichThanhToans_TaiKhoans_NguoiTraId] FOREIGN KEY ([NguoiTraId]) REFERENCES [TaiKhoans] ([Id]) ON DELETE NO ACTION;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261030123014_Mo9_DonNhom', N'8.0.0');
GO

COMMIT;
GO

