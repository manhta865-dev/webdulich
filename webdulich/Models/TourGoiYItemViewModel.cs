namespace webdulich.Models
{
    public class TourGoiYItemViewModel
    {
        public Tour Tour { get; set; } = new Tour();

        public int DiemGoiY { get; set; }

        public int SoLuotDat { get; set; }

        public string LyDoGoiY { get; set; } = string.Empty;

        public string TenDiaDiemHienThi { get; set; } = string.Empty;
    }
}