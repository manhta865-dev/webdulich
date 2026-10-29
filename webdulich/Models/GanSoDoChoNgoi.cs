namespace webdulich.Models
{
    // Chọn sơ đồ chỗ ngồi cho một lịch khởi hành: sơ đồ nhỏ nhất của đúng phương tiện của tour
    // mà đủ chỗ cho số chỗ tối đa của lịch (RB-14). Ví dụ xe du lịch 15 chỗ -> Xe 16 chỗ, 20 chỗ -> Xe 29 chỗ.
    public static class GanSoDoChoNgoi
    {
        public class KetQua
        {
            public int? SoDoChoNgoiId { get; set; }
            public string? Loi { get; set; }
        }

        public static KetQua Chon(DuLichDbContext db, int tourId, int soChoToiDa)
        {
            var phuongTienId = db.Tours.Where(t => t.Id == tourId).Select(t => t.PhuongTienId).FirstOrDefault();

            // Tour chưa có phương tiện: lịch không có sơ đồ, khách không chọn chỗ
            if (!phuongTienId.HasValue)
            {
                return new KetQua();
            }

            var dsSoDo = db.SoDoChoNgois
                .Where(s => s.PhuongTienId == phuongTienId.Value)
                .Select(s => new { s.Id, s.TenSoDo, SoViTri = s.ViTriChoNgois.Count })
                .OrderBy(s => s.SoViTri)
                .ToList();

            // Phương tiện chưa có sơ đồ mẫu nào: cũng không chọn chỗ
            if (!dsSoDo.Any())
            {
                return new KetQua();
            }

            var phuHop = dsSoDo.FirstOrDefault(s => s.SoViTri >= soChoToiDa);
            if (phuHop == null)
            {
                var lonNhat = dsSoDo.Last();
                return new KetQua
                {
                    Loi = $"Số chỗ tối đa {soChoToiDa} vượt sức chứa phương tiện lớn nhất ({lonNhat.TenSoDo}, {lonNhat.SoViTri} chỗ)."
                };
            }

            return new KetQua { SoDoChoNgoiId = phuHop.Id };
        }
    }
}
