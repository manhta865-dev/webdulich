// Vé lật 3D: bấm để lật, rê chuột để nghiêng, lưu thành ảnh PNG gửi vào nhóm chat.
// Dùng ở trang ghế đã trả (DonNhom/GheCuaToi, một vé) và trang vé của đơn đặt riêng (DatTour/Ve, mỗi hành khách một vé).
// Mỗi vé [data-ve] nằm trong một khối [data-ve-khoi] chứa nút lật, nút lưu và dòng báo của riêng vé đó.
// Ảnh vẽ bằng canvas ngay trên trình duyệt, không gửi dữ liệu đi đâu. Dữ liệu vé không có số giấy tờ.
(function () {
    var dsVe = document.querySelectorAll("[data-ve]");
    if (!dsVe.length) return;

    var FONT = '"Be Vietnam Pro", system-ui, "Segoe UI", Arial, sans-serif';
    var MAU = { nen: "#F5F7F6", beMat: "#FFFFFF", chu: "#17262E", phu: "#5B6B73", nhan: "#0A6E8A", dam: "#085A72", vien: "#DCE3E6" };
    var nghiengDuoc = window.matchMedia("(hover: hover) and (prefers-reduced-motion: no-preference)").matches;

    function hinhBoGoc(ctx, x, y, r, d, bo) {
        ctx.beginPath();
        ctx.moveTo(x + bo, y);
        ctx.arcTo(x + r, y, x + r, y + d, bo);
        ctx.arcTo(x + r, y + d, x, y + d, bo);
        ctx.arcTo(x, y + d, x, y, bo);
        ctx.arcTo(x, y, x + r, y, bo);
        ctx.closePath();
    }

    // Ngắt dòng theo từ; quá số dòng thì cắt và thêm dấu ba chấm
    function ngatDong(ctx, chu, rong, toiDa) {
        var dong = [], hienTai = "";
        chu.split(/\s+/).forEach(function (tu) {
            var thu = hienTai ? hienTai + " " + tu : tu;
            if (ctx.measureText(thu).width > rong && hienTai) {
                dong.push(hienTai);
                hienTai = tu;
            } else {
                hienTai = thu;
            }
        });
        if (hienTai) dong.push(hienTai);
        if (dong.length > toiDa) {
            dong = dong.slice(0, toiDa);
            var cuoi = dong[toiDa - 1];
            while (cuoi && ctx.measureText(cuoi + "...").width > rong) cuoi = cuoi.slice(0, -1);
            dong[toiDa - 1] = cuoi + "...";
        }
        return dong;
    }

    function truong(ctx, x, y, rong, nhan, giaTri) {
        ctx.fillStyle = MAU.phu;
        ctx.font = "500 22px " + FONT;
        ctx.fillText(nhan, x, y);
        ctx.fillStyle = MAU.chu;
        ctx.font = "700 30px " + FONT;
        ctx.fillText(ngatDong(ctx, giaTri, rong, 1)[0] || "", x, y + 40);
    }

    function veAnh(ve) {
        var d = ve.dataset;
        var canvas = document.createElement("canvas");
        canvas.width = 1200;
        canvas.height = 560;
        var ctx = canvas.getContext("2d");
        ctx.textBaseline = "alphabetic";

        ctx.fillStyle = MAU.nen;
        ctx.fillRect(0, 0, 1200, 560);

        // Thân vé
        var x = 40, y = 40, r = 1120, c = 480, xCat = 820;
        hinhBoGoc(ctx, x, y, r, c, 24);
        ctx.fillStyle = MAU.beMat;
        ctx.fill();
        ctx.save();
        ctx.clip();
        ctx.fillStyle = MAU.dam;
        ctx.fillRect(x, y, r, 96);
        ctx.restore();

        ctx.fillStyle = "#FFFFFF";
        ctx.font = "700 34px " + FONT;
        ctx.fillText("TravelVN", x + 40, y + 60);
        ctx.font = "500 24px " + FONT;
        ctx.textAlign = "right";
        ctx.fillText("Thông tin chuyến đi", x + r - 40, y + 58);
        ctx.textAlign = "left";

        // Tên tour
        var xTrai = x + 40, rongTrai = xCat - xTrai - 40;
        ctx.fillStyle = MAU.chu;
        ctx.font = "700 40px " + FONT;
        ngatDong(ctx, d.tour, rongTrai, 2).forEach(function (dong, i) {
            ctx.fillText(dong, xTrai, y + 160 + i * 50);
        });

        // Họ tên có thể dài nên chiếm cả hàng, không bị cắt; ba thông tin ngắn xếp hàng dưới
        truong(ctx, xTrai, y + 280, rongTrai, "Hành khách", d.ten);
        var rongCot = (rongTrai - 48) / 3;
        truong(ctx, xTrai, y + 370, rongCot, "Ngày đi", d.ngay);
        truong(ctx, xTrai + rongCot + 24, y + 370, rongCot, "Giờ đi", d.gio);
        truong(ctx, xTrai + (rongCot + 24) * 2, y + 370, rongCot, d.nhanCho, d.cho);

        // Đường xé và hai vết khuyết
        ctx.strokeStyle = MAU.vien;
        ctx.lineWidth = 3;
        ctx.setLineDash([12, 12]);
        ctx.beginPath();
        ctx.moveTo(xCat, y + 110);
        ctx.lineTo(xCat, y + c - 20);
        ctx.stroke();
        ctx.setLineDash([]);
        ctx.fillStyle = MAU.nen;
        [y, y + c].forEach(function (yKhuyet) {
            ctx.beginPath();
            ctx.arc(xCat, yKhuyet, 18, 0, Math.PI * 2);
            ctx.fill();
        });

        // Cuống vé: vé ghế đơn nhóm in mã ghế, vé đơn đặt riêng in số thứ tự hành khách (data-nhan-ma)
        var xPhai = xCat + 40, rongPhai = x + r - xPhai - 40;
        truong(ctx, xPhai, y + 150, rongPhai, "Mã chuyến đi", d.maChuyen);
        truong(ctx, xPhai, y + 240, rongPhai, d.nhanMa || "Mã ghế", d.maGhe);
        ctx.fillStyle = MAU.phu;
        ctx.font = "400 21px " + FONT;
        ngatDong(ctx, "Mang theo giấy tờ gốc đúng với thông tin đã điền. Ảnh này không dùng để soát vé.", rongPhai, 4)
            .forEach(function (dong, i) {
                ctx.fillText(dong, xPhai, y + 340 + i * 30);
            });

        return canvas;
    }

    function ganVe(ve) {
        var khoi = ve.closest("[data-ve-khoi]") || document;
        var nutLat = khoi.querySelector("[data-ve-lat]");
        var nutLuu = khoi.querySelector("[data-ve-luu]");
        var thongBao = khoi.querySelector("[data-ve-bao]");
        var matTruoc = ve.querySelector(".ve-truoc");
        var matSau = ve.querySelector(".ve-sau");
        var nghieng = ve.querySelector(".ve-nghieng");

        function lat() {
            var daLat = ve.classList.toggle("da-lat");
            if (nutLat) nutLat.textContent = daLat ? "Xem mặt trước" : "Xem mặt sau";
            // Trình đọc màn hình chỉ đọc mặt đang hiện
            matTruoc.setAttribute("aria-hidden", daLat ? "true" : "false");
            matSau.setAttribute("aria-hidden", daLat ? "false" : "true");
        }

        if (nutLat) nutLat.addEventListener("click", lat);
        ve.addEventListener("click", function () {
            // Đang bôi đen để sao chép mã thì không lật
            if (String(window.getSelection())) return;
            lat();
        });

        // Rê chuột thì vé nghiêng theo; điện thoại và người chọn giảm chuyển động thì bỏ qua
        if (nghiengDuoc) {
            ve.addEventListener("pointermove", function (e) {
                var khung = ve.getBoundingClientRect();
                var x = (e.clientX - khung.left) / khung.width - 0.5;
                var y = (e.clientY - khung.top) / khung.height - 0.5;
                nghieng.style.transform = "rotateX(" + (-y * 8).toFixed(2) + "deg) rotateY(" + (x * 10).toFixed(2) + "deg)";
            });
            ve.addEventListener("pointerleave", function () {
                nghieng.style.transform = "";
            });
        }

        function bao(chu) {
            if (thongBao) thongBao.textContent = chu;
        }

        if (!nutLuu) return;
        nutLuu.addEventListener("click", function () {
            nutLuu.disabled = true;
            bao("");
            // Đợi phông chữ để tiếng Việt trên ảnh giống trên trang; lỗi phông thì vẫn vẽ bằng phông dự phòng
            var phong = document.fonts
                ? Promise.all([document.fonts.load('700 40px "Be Vietnam Pro"'), document.fonts.load('500 22px "Be Vietnam Pro"')])
                : Promise.resolve();
            phong.catch(function () { }).then(function () {
                veAnh(ve).toBlob(function (tep) {
                    nutLuu.disabled = false;
                    if (!tep) {
                        bao("Không tạo được ảnh trên trình duyệt này. Bạn có thể chụp màn hình vé.");
                        return;
                    }
                    var duongDan = URL.createObjectURL(tep);
                    var a = document.createElement("a");
                    a.href = duongDan;
                    // Tên tệp: vé ghế đơn nhóm "ghe-3", vé đơn đặt riêng "khach-1" (data-tep)
                    a.download = "ve-" + ve.dataset.maChuyen + "-" + (ve.dataset.tep || "ghe-" + ve.dataset.thuTu) + ".png";
                    document.body.appendChild(a);
                    a.click();
                    a.remove();
                    setTimeout(function () { URL.revokeObjectURL(duongDan); }, 10000);
                    bao("Đã lưu ảnh vé.");
                }, "image/png");
            }).catch(function () {
                nutLuu.disabled = false;
                bao("Không tạo được ảnh trên trình duyệt này. Bạn có thể chụp màn hình vé.");
            });
        });
    }

    dsVe.forEach(ganVe);
})();
