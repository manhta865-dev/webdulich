using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace webdulich.Migrations
{
    /// <summary>
    /// Bước 5a: dữ liệu mẫu 4 sơ đồ chỗ ngồi (không đổi cấu trúc).
    /// Lưới gồm SoHang hàng, SoCot cột; cột lối đi không có vị trí nào. Mã vị trí = số hàng + chữ cái
    /// theo thứ tự ghế trong hàng (ví dụ 3B). Chạy lại không nhân đôi (bỏ qua sơ đồ đã có cùng tên).
    ///   Xe 16 chỗ: hàng 1-4 "A B · C", hàng 5 "A B C D"
    ///   Xe 29 chỗ: hàng 1-6 "A B · C D", hàng 7 năm ghế liền
    ///   Xe 45 chỗ: hàng 1-10 "A B · C D", hàng 11 năm ghế liền
    ///   Máy bay 180 chỗ: 30 hàng "A B C · D E F"
    /// </summary>
    public partial class Mo7_SoDoChoNgoiMau : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
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
CLOSE c; DEALLOCATE c;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Chỉ xóa sơ đồ mẫu chưa được lịch nào dùng (vị trí xóa theo, Cascade)
            migrationBuilder.Sql(@"
DELETE s FROM SoDoChoNgois s
WHERE s.TenSoDo IN (N'Xe 16 chỗ', N'Xe 29 chỗ', N'Xe 45 chỗ', N'Máy bay 180 chỗ (3-3)')
  AND NOT EXISTS (SELECT 1 FROM LichKhoiHanhTours l WHERE l.SoDoChoNgoiId = s.Id);");
        }
    }
}
