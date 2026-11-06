BEGIN TRANSACTION;
GO

CREATE TABLE [Videos] (
    [Id] int NOT NULL IDENTITY,
    [TieuDe] nvarchar(200) NOT NULL,
    [MaYouTube] varchar(11) NOT NULL,
    [MoTa] nvarchar(500) NULL,
    [ThuTu] int NOT NULL,
    [DangHien] bit NOT NULL,
    [TourId] int NULL,
    [NgayTao] datetime2 NOT NULL,
    CONSTRAINT [PK_Videos] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_Video_MaYouTube] CHECK (LEN([MaYouTube]) = 11 AND [MaYouTube] NOT LIKE '%[^A-Za-z0-9_-]%'),
    CONSTRAINT [FK_Videos_Tours_TourId] FOREIGN KEY ([TourId]) REFERENCES [Tours] ([Id]) ON DELETE SET NULL
);
GO

CREATE UNIQUE INDEX [IX_Videos_MaYouTube] ON [Videos] ([MaYouTube]);
GO

CREATE INDEX [IX_Videos_TourId] ON [Videos] ([TourId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261106145952_Mo13_Video', N'8.0.0');
GO

COMMIT;
GO

