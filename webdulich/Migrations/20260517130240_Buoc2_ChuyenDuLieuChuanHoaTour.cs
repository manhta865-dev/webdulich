using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace webdulich.Migrations
{
    public partial class Buoc2_ChuyenDuLieuChuanHoaTour : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM [PhuongTiens] WHERE [TenPhuongTien] = N'Xe du lịch')
    INSERT INTO [PhuongTiens] ([TenPhuongTien]) VALUES (N'Xe du lịch');

IF NOT EXISTS (SELECT 1 FROM [PhuongTiens] WHERE [TenPhuongTien] = N'Máy bay')
    INSERT INTO [PhuongTiens] ([TenPhuongTien]) VALUES (N'Máy bay');

IF NOT EXISTS (SELECT 1 FROM [PhuongTiens] WHERE [TenPhuongTien] = N'Tàu hỏa')
    INSERT INTO [PhuongTiens] ([TenPhuongTien]) VALUES (N'Tàu hỏa');

IF NOT EXISTS (SELECT 1 FROM [PhuongTiens] WHERE [TenPhuongTien] = N'Tàu cao tốc')
    INSERT INTO [PhuongTiens] ([TenPhuongTien]) VALUES (N'Tàu cao tốc');

IF NOT EXISTS (SELECT 1 FROM [PhuongTiens] WHERE [TenPhuongTien] = N'Du thuyền')
    INSERT INTO [PhuongTiens] ([TenPhuongTien]) VALUES (N'Du thuyền');

IF NOT EXISTS (SELECT 1 FROM [PhuongTiens] WHERE [TenPhuongTien] = N'Xe máy')
    INSERT INTO [PhuongTiens] ([TenPhuongTien]) VALUES (N'Xe máy');

IF NOT EXISTS (SELECT 1 FROM [PhuongTiens] WHERE [TenPhuongTien] = N'Tự túc')
    INSERT INTO [PhuongTiens] ([TenPhuongTien]) VALUES (N'Tự túc');
");

            migrationBuilder.Sql(@"
INSERT INTO [LoaiDiaHinhs] ([TenLoaiDiaHinh])
SELECT DISTINCT DuLieu.Ten
FROM (
    SELECT LTRIM(RTRIM([value])) AS Ten
    FROM [Tours]
    CROSS APPLY STRING_SPLIT(ISNULL([LoaiDiaHinh], N''), N',')
    WHERE LTRIM(RTRIM([value])) <> N''
) AS DuLieu
WHERE NOT EXISTS (
    SELECT 1
    FROM [LoaiDiaHinhs] AS l
    WHERE l.[TenLoaiDiaHinh] = DuLieu.Ten
);
");

            migrationBuilder.Sql(@"
INSERT INTO [PhongCachs] ([TenPhongCach])
SELECT DISTINCT DuLieu.Ten
FROM (
    SELECT LTRIM(RTRIM([value])) AS Ten
    FROM [Tours]
    CROSS APPLY STRING_SPLIT(ISNULL([PhongCach], N''), N',')
    WHERE LTRIM(RTRIM([value])) <> N''
) AS DuLieu
WHERE NOT EXISTS (
    SELECT 1
    FROM [PhongCachs] AS p
    WHERE p.[TenPhongCach] = DuLieu.Ten
);
");

            migrationBuilder.Sql(@"
INSERT INTO [PhuongTiens] ([TenPhuongTien])
SELECT DISTINCT LTRIM(RTRIM([PhuongTien]))
FROM [Tours]
WHERE [PhuongTien] IS NOT NULL
  AND LTRIM(RTRIM([PhuongTien])) <> N''
  AND NOT EXISTS (
      SELECT 1
      FROM [PhuongTiens] AS p
      WHERE p.[TenPhuongTien] = LTRIM(RTRIM([Tours].[PhuongTien]))
  );
");

            migrationBuilder.Sql(@"
UPDATE t
SET t.[PhuongTienId] = p.[Id]
FROM [Tours] AS t
INNER JOIN [PhuongTiens] AS p
    ON p.[TenPhuongTien] = LTRIM(RTRIM(t.[PhuongTien]))
WHERE t.[PhuongTienId] IS NULL
  AND t.[PhuongTien] IS NOT NULL
  AND LTRIM(RTRIM(t.[PhuongTien])) <> N'';
");

            migrationBuilder.Sql(@"
INSERT INTO [TourLoaiDiaHinhs] ([TourId], [LoaiDiaHinhId])
SELECT DISTINCT t.[Id], l.[Id]
FROM [Tours] AS t
CROSS APPLY STRING_SPLIT(ISNULL(t.[LoaiDiaHinh], N''), N',') AS s
INNER JOIN [LoaiDiaHinhs] AS l
    ON l.[TenLoaiDiaHinh] = LTRIM(RTRIM(s.[value]))
WHERE LTRIM(RTRIM(s.[value])) <> N''
  AND NOT EXISTS (
      SELECT 1
      FROM [TourLoaiDiaHinhs] AS tl
      WHERE tl.[TourId] = t.[Id]
        AND tl.[LoaiDiaHinhId] = l.[Id]
  );
");

            migrationBuilder.Sql(@"
INSERT INTO [TourPhongCachs] ([TourId], [PhongCachId])
SELECT DISTINCT t.[Id], p.[Id]
FROM [Tours] AS t
CROSS APPLY STRING_SPLIT(ISNULL(t.[PhongCach], N''), N',') AS s
INNER JOIN [PhongCachs] AS p
    ON p.[TenPhongCach] = LTRIM(RTRIM(s.[value]))
WHERE LTRIM(RTRIM(s.[value])) <> N''
  AND NOT EXISTS (
      SELECT 1
      FROM [TourPhongCachs] AS tp
      WHERE tp.[TourId] = t.[Id]
        AND tp.[PhongCachId] = p.[Id]
  );
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Không tự động xóa dữ liệu chuẩn hóa để tránh mất dữ liệu nếu đã nhập thêm sau migration.
        }
    }
}