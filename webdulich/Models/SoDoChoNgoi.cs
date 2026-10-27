using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace webdulich.Models
{
    // Mẫu sơ đồ khoang xe hoặc máy bay, dùng chung cho nhiều chuyến
    public class SoDoChoNgoi
    {
        [Key]
        public int Id { get; set; }

        public string TenSoDo { get; set; } = string.Empty;

        public int? PhuongTienId { get; set; }

        public int SoHang { get; set; }

        public int SoCot { get; set; }

        [ForeignKey("PhuongTienId")]
        public PhuongTien? PhuongTien { get; set; }

        public ICollection<ViTriChoNgoi> ViTriChoNgois { get; set; } = new List<ViTriChoNgoi>();
    }
}
