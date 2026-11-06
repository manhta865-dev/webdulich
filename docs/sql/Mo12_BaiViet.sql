BEGIN TRANSACTION;
GO

CREATE TABLE [BaiViets] (
    [Id] int NOT NULL IDENTITY,
    [TieuDe] nvarchar(200) NOT NULL,
    [DuongDan] nvarchar(220) NOT NULL,
    [TomTat] nvarchar(500) NOT NULL,
    [NoiDung] nvarchar(max) NOT NULL,
    [AnhBia] nvarchar(260) NULL,
    [TrangThai] nvarchar(10) NOT NULL,
    [NgayTao] datetime2 NOT NULL,
    [NgayDang] datetime2 NULL,
    [TacGiaId] int NOT NULL,
    [TourId] int NULL,
    CONSTRAINT [PK_BaiViets] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_BaiViet_NgayDang] CHECK ([TrangThai] <> 'DaDang' OR [NgayDang] IS NOT NULL),
    CONSTRAINT [CK_BaiViet_TrangThai] CHECK ([TrangThai] IN ('Nhap','DaDang')),
    CONSTRAINT [FK_BaiViets_TaiKhoans_TacGiaId] FOREIGN KEY ([TacGiaId]) REFERENCES [TaiKhoans] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_BaiViets_Tours_TourId] FOREIGN KEY ([TourId]) REFERENCES [Tours] ([Id]) ON DELETE SET NULL
);
GO

CREATE UNIQUE INDEX [IX_BaiViets_DuongDan] ON [BaiViets] ([DuongDan]);
GO

CREATE INDEX [IX_BaiViets_TacGiaId] ON [BaiViets] ([TacGiaId]);
GO

CREATE INDEX [IX_BaiViets_TourId] ON [BaiViets] ([TourId]);
GO

CREATE INDEX [IX_BaiViets_TrangThai_NgayDang] ON [BaiViets] ([TrangThai], [NgayDang]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261106125521_Mo12_BaiViet', N'8.0.0');
GO

COMMIT;
GO

