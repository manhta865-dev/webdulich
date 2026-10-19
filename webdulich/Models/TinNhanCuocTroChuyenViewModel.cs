namespace webdulich.Models
{
    public class TinNhanCuocTroChuyenViewModel
    {
        public int TaiKhoanId { get; set; }

        public string HoTen { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string TinNhanCuoi { get; set; } = string.Empty;

        public string NguoiGuiCuoi { get; set; } = string.Empty;

        public DateTime NgayGuiCuoi { get; set; }

        public int ChuaDoc { get; set; }
    }
}