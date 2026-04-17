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
    }
}