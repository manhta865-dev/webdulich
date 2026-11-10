using System.ComponentModel.DataAnnotations;

namespace webdulich.Models
{
    public class DiaDiem
    {
        [Key]
        public int MaDiaDiem { get; set; }

        public string TenDiaDiem { get; set; }

        public string MoTa { get; set; }

        public string DiaChi { get; set; }

        public string HinhAnh { get; set; }

        public decimal GiaTour { get; set; }

        // Vị trí trên Bản đồ tour và quả địa cầu (Mo18, RB-44). Admin bấm chọn trên bản đồ ở form tour.
        // Trong nước hay nước ngoài đều được. Rỗng: chưa chọn vị trí. Hai cột cùng có hoặc cùng rỗng (CHECK).
        public decimal? ViDo { get; set; }

        public decimal? KinhDo { get; set; }
    }
}
