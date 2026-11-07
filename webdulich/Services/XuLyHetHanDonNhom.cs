using webdulich.Models;

namespace webdulich.Services
{
    // Chạy nền theo chu kỳ (mặc định 5 phút, DonNhom:ChuKyQuetPhut): tìm đơn nhóm đang giữ chỗ đã quá hạn
    // và xử lý từng đơn bằng XuLyDon.XuLyDonQuaHan (gia hạn, nhả ghế, chốt hoặc hủy). N5.
    // Host dùng chung có thể cho ứng dụng "ngủ" khi không ai truy cập, nên các trang đơn nhóm cũng tự kiểm khi mở.
    public class XuLyHetHanDonNhom : BackgroundService
    {
        private readonly IServiceScopeFactory _taoPhamVi;
        private readonly IConfiguration _cauHinh;
        private readonly ILogger<XuLyHetHanDonNhom> _nhatKy;

        public XuLyHetHanDonNhom(IServiceScopeFactory taoPhamVi, IConfiguration cauHinh, ILogger<XuLyHetHanDonNhom> nhatKy)
        {
            _taoPhamVi = taoPhamVi;
            _cauHinh = cauHinh;
            _nhatKy = nhatKy;
        }

        protected override async Task ExecuteAsync(CancellationToken dungLai)
        {
            int chuKy = Math.Max(1, _cauHinh.GetValue("DonNhom:ChuKyQuetPhut", 5));
            using var henGio = new PeriodicTimer(TimeSpan.FromMinutes(chuKy));
            do
            {
                QuetMotLan();
            }
            while (await henGio.WaitForNextTickAsync(dungLai));
        }

        // Một lần quét. Bắt mọi lỗi: lỗi ở một đơn không được làm dừng cả dịch vụ hay ứng dụng.
        private void QuetMotLan()
        {
            try
            {
                using var phamVi = _taoPhamVi.CreateScope();
                var db = phamVi.ServiceProvider.GetRequiredService<DuLichDbContext>();
                int soGioGiaHan = _cauHinh.GetValue("DonNhom:SoGioGiaHan", 6);
                var bayGio = GioVN.BayGio;

                var dsDonId = db.DatTours
                    .Where(x => x.LoaiDon == "Nhom" && x.TrangThai == "GiuCho" && x.HanGiuCho <= bayGio)
                    .Select(x => x.Id)
                    .ToList();

                foreach (var donId in dsDonId)
                {
                    try
                    {
                        var ketQua = XuLyDon.XuLyDonQuaHan(db, donId, soGioGiaHan);
                        if (ketQua != XuLyDon.KetQuaHetHan.KhongDoi)
                        {
                            _nhatKy.LogInformation("Đơn nhóm {DonId} hết hạn giữ chỗ: {KetQua}", donId, ketQua);
                        }
                    }
                    catch (Exception loi)
                    {
                        _nhatKy.LogError(loi, "Lỗi khi xử lý hết hạn đơn nhóm {DonId}", donId);
                    }
                }

                // Đơn cá nhân quá hạn giữ chỗ chưa trả tiền mặt: tự hủy, trả chỗ (RB-38)
                var dsDonCaNhan = db.DatTours
                    .Where(x => x.LoaiDon == "CaNhan" && x.TrangThai == "GiuCho" && x.HanGiuCho <= bayGio)
                    .Select(x => x.Id)
                    .ToList();
                foreach (var donId in dsDonCaNhan)
                {
                    try
                    {
                        if (XuLyDon.HuyDonCaNhanQuaHan(db, donId))
                        {
                            _nhatKy.LogInformation("Đơn cá nhân {DonId} quá hạn giữ chỗ chưa trả: đã hủy", donId);
                        }
                    }
                    catch (Exception loi)
                    {
                        _nhatKy.LogError(loi, "Lỗi khi hủy đơn cá nhân quá hạn {DonId}", donId);
                    }
                }

                // Lịch sắp đi còn ghế đang giữ mà chưa có chỗ trên xe (dữ liệu cũ, hoặc lần xếp trước bị trùng): xếp bù (RB-36)
                var homNay = bayGio.Date;
                var dsLichThieu = db.HanhKhachDatTours
                    .Where(h => h.NgayNha == null
                        && h.DatTour!.TrangThai != "DaHuy"
                        && h.DatTour.LichKhoiHanhTour!.SoDoChoNgoiId != null
                        && h.DatTour.LichKhoiHanhTour.TrangThai != TrangThaiLich.DaHuy
                        && h.DatTour.LichKhoiHanhTour.NgayKhoiHanh >= homNay
                        && !db.ChoNgoiChuyenDis.Any(c => c.HanhKhachDatTourId == h.Id))
                    .Select(h => h.DatTour!.LichKhoiHanhTourId)
                    .Distinct()
                    .ToList();

                foreach (var lichId in dsLichThieu)
                {
                    try
                    {
                        int soXep = XepChoNgoi.XepChoConThieu(db, lichId);
                        if (soXep > 0)
                        {
                            db.SaveChanges();
                            _nhatKy.LogInformation("Lịch {LichId}: xếp bù chỗ ngồi cho {SoGhe} ghế", lichId, soXep);
                        }
                    }
                    catch (Exception loi)
                    {
                        db.ChangeTracker.Clear();
                        _nhatKy.LogError(loi, "Lỗi khi xếp bù chỗ ngồi cho lịch {LichId}", lichId);
                    }
                }
            }
            catch (Exception loi)
            {
                _nhatKy.LogError(loi, "Lỗi khi quét đơn nhóm hết hạn");
            }
        }
    }
}
