using System.ComponentModel.DataAnnotations;

namespace webdulich.Models
{
    public class DanhMuc
    {
        [Key]
        public int MaDanhMuc { get; set; }
        public string TenDanhMuc { get; set; }
    }
}