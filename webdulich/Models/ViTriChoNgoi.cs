using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace webdulich.Models
{
    // Một vị trí vật lý trên sơ đồ (ví dụ 12A)
    public class ViTriChoNgoi
    {
        [Key]
        public int Id { get; set; }

        public int SoDoChoNgoiId { get; set; }

        public string MaViTri { get; set; } = string.Empty;

        public int Hang { get; set; }

        public int Cot { get; set; }

        [ForeignKey("SoDoChoNgoiId")]
        public SoDoChoNgoi? SoDoChoNgoi { get; set; }
    }
}
