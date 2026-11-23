// Màn hình chờ "lá dừa" (Views/Shared/_ManHinhCho.cshtml). Nạp ngay sau lớp phủ nên chạy trước khi phần còn lại của trang hiện:
// vẽ một tàu lá dừa, đặt vài tàu thành hai hàng ở hai mép, rồi khi trang dựng xong thì thêm lớp .mo để lá tỏa ra.
(function () {
    "use strict";
    var rung = document.getElementById("tvRung");
    var mau = document.getElementById("tvLaDua");
    if (!rung || !mau) return;
    var NS = "http://www.w3.org/2000/svg";

    function so(v) { return Math.round(v * 10) / 10; }

    // Một tàu lá dừa dài khoảng 640 đơn vị, gốc ở (0, 0), cong vồng lên rồi rủ xuống về phía x dương.
    // Gồm cuống thon dần và hai dãy lá chét hẹp rủ theo trọng lực; tất cả gộp trong một path.
    function veTauLa() {
        var x1 = 320, y1 = -120, x2 = 640, y2 = 50;
        function diem(t) { return [2 * (1 - t) * t * x1 + t * t * x2, 2 * (1 - t) * t * y1 + t * t * y2]; }
        function tiepTuyen(t) {
            var dx = 2 * (1 - t) * x1 + 2 * t * (x2 - x1), dy = 2 * (1 - t) * y1 + 2 * t * (y2 - y1);
            var l = Math.sqrt(dx * dx + dy * dy);
            return [dx / l, dy / l];
        }

        var d = "";
        // Cuống lá: dải thon dần từ gốc tới ngọn
        var trai = [], phai = [];
        for (var i = 0; i <= 24; i++) {
            var t = i / 24, p = diem(t), tg = tiepTuyen(t), w = 7 * (1 - t) + 1;
            trai.push([p[0] - tg[1] * w, p[1] + tg[0] * w]);
            phai.unshift([p[0] + tg[1] * w, p[1] - tg[0] * w]);
        }
        var vien = trai.concat(phai);
        d += "M" + vien.map(function (q) { return so(q[0]) + " " + so(q[1]); }).join("L") + "Z";

        // Lá chét hai bên: dài nhất ở khoảng giữa tàu, ngắn dần về gốc và ngọn, nghiêng về phía ngọn và rủ xuống
        for (var t2 = 0.07; t2 < 0.985; t2 += 0.03) {
            var p2 = diem(t2), tg2 = tiepTuyen(t2), n = [-tg2[1], tg2[0]];
            var dai = 28 + 165 * Math.pow(Math.sin(Math.PI * Math.min(1, t2 * 1.12)), 0.75) * (1 - t2 * 0.3);
            var rong = 6.5 * (1 - t2 * 0.55);
            for (var ben = -1; ben <= 1; ben += 2) {
                var goc = 0.9;
                var hx = tg2[0] * Math.cos(goc) + ben * n[0] * Math.sin(goc);
                var hy = tg2[1] * Math.cos(goc) + ben * n[1] * Math.sin(goc) + 0.42;
                var lh = Math.sqrt(hx * hx + hy * hy);
                hx /= lh; hy /= lh;
                var ngon = [p2[0] + hx * dai, p2[1] + hy * dai];
                var giua = [p2[0] + hx * dai * 0.5, p2[1] + hy * dai * 0.5 + dai * 0.06];
                d += "M" + so(p2[0] - tg2[0] * rong) + " " + so(p2[1] - tg2[1] * rong)
                    + "Q" + so(giua[0] - tg2[0] * rong * 0.5) + " " + so(giua[1] - tg2[1] * rong * 0.5) + " " + so(ngon[0]) + " " + so(ngon[1])
                    + "Q" + so(giua[0] + tg2[0] * rong * 1.1) + " " + so(giua[1] + tg2[1] * rong * 1.1) + " " + so(p2[0] + tg2[0] * rong) + " " + so(p2[1] + tg2[1] * rong) + "Z";
            }
        }
        return d;
    }

    mau.setAttribute("d", veTauLa());

    // Vị trí các tàu trong khung 600 x 1000 của mỗi bên: [x gốc, y gốc, góc xoay, tỉ lệ].
    // Bên phải là bản lật của bên trái (CSS) nhưng xếp khác để hai bên không giống soi gương.
    var HANG = {
        "trai-sau": [[-30, 60, 22, 0.86], [-40, 380, 4, 0.92], [-30, 700, -12, 0.9], [-40, 990, -26, 0.84]],
        "trai-truoc": [[-50, 220, 12, 0.78], [-60, 540, -4, 0.82], [-50, 850, -18, 0.78]],
        "phai-sau": [[-40, -10, 26, 0.84], [-30, 300, 8, 0.9], [-40, 620, -8, 0.92], [-30, 930, -22, 0.86]],
        "phai-truoc": [[-60, 150, 16, 0.78], [-50, 460, 0, 0.82], [-60, 780, -14, 0.8]]
    };
    // Màn hình hẹp: khung mỗi bên bị co theo chiều cao nên tàu lá sẽ dài quá nửa màn hình; thu nhỏ lại
    var heSo = window.innerWidth < 640 ? 0.62 : 1;

    var dsSvg = rung.querySelectorAll("svg[data-hang]");
    for (var k = 0; k < dsSvg.length; k++) {
        var ds = HANG[dsSvg[k].getAttribute("data-hang")] || [];
        for (var j = 0; j < ds.length; j++) {
            var tau = document.createElementNS(NS, "use");
            tau.setAttribute("href", "#tvLaDua");
            tau.setAttribute("transform", "translate(" + ds[j][0] + " " + ds[j][1] + ") rotate(" + ds[j][2] + ") scale(" + so(ds[j][3] * heSo) + ")");
            dsSvg[k].appendChild(tau);
        }
    }

    // Trang dựng xong và lá đã che ít nhất một chút thì mở. Đợi trình duyệt vẽ xong một khung hình rồi mới thêm .mo
    // để chuyển động bắt đầu trơn tru; mở xong gỡ hẳn lớp phủ.
    var batDau = Date.now();
    var daMo = false;
    function mo() {
        if (daMo) return;
        daMo = true;
        setTimeout(function () {
            requestAnimationFrame(function () {
                requestAnimationFrame(function () {
                    rung.classList.add("mo");
                    setTimeout(function () { if (rung.parentNode) rung.parentNode.removeChild(rung); }, 1100);
                });
            });
        }, Math.max(0, 400 - (Date.now() - batDau)));
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", mo);
    } else {
        mo();
    }

    // Quay lại bằng nút Back (trang lấy từ bộ nhớ đệm): không hiện lại lớp phủ
    window.addEventListener("pageshow", function (e) {
        if (e.persisted && rung.parentNode) rung.parentNode.removeChild(rung);
    });
})();
