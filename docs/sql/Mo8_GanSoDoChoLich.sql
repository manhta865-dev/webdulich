BEGIN TRANSACTION;
GO

UPDATE l
SET SoDoChoNgoiId = (
    SELECT TOP 1 s.Id
    FROM SoDoChoNgois s
    WHERE s.PhuongTienId = t.PhuongTienId
      AND (SELECT COUNT(*) FROM ViTriChoNgois v WHERE v.SoDoChoNgoiId = s.Id) >= l.SoChoToiDa
    ORDER BY (SELECT COUNT(*) FROM ViTriChoNgois v WHERE v.SoDoChoNgoiId = s.Id), s.Id)
FROM LichKhoiHanhTours l
JOIN Tours t ON t.Id = l.TourId
WHERE l.SoDoChoNgoiId IS NULL AND t.PhuongTienId IS NOT NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261029162743_Mo8_GanSoDoChoLich', N'8.0.0');
GO

COMMIT;
GO

