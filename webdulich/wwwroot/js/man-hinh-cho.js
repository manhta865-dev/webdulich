// Màn hình chờ "rừng cây" (Views/Shared/_ManHinhCho.cshtml). File này được nạp ngay sau lớp phủ nên chạy trước khi
// phần còn lại của trang hiện: vẽ lá vào bốn khung SVG, rồi khi trang dựng xong (không đợi ảnh) thì thêm lớp .mo để cây tách ra.
(function () {
    "use strict";
    var rung = document.getElementById("tvRung");
    if (!rung) return;
    var NS = "http://www.w3.org/2000/svg";

    // Một lá nhọn dài 64 đơn vị, gốc ở (0, 0), chĩa theo chiều x dương
    var LA = "M0 0C18 -15 44 -15 64 0C44 15 18 15 0 0Z";
    var GOC_LA = [-64, -34, -8, 18, 44, 70];

    function phanTu(ten, thuocTinh) {
        var el = document.createElementNS(NS, ten);
        for (var k in thuocTinh) el.setAttribute(k, thuocTinh[k]);
        return el;
    }

    // Chùm lá xòe quanh hướng huong (độ); giữa có khối lá tròn che chỗ nối. lech cố định để lần nào cũng ra cùng một hình.
    function chumLa(g, x, y, r, huong, lech) {
        var s = r / 40;
        for (var k = 0; k < GOC_LA.length; k++) {
            var goc = huong + GOC_LA[k] + ((lech + k * 7) % 11) - 5;
            var tiLe = s * (0.78 + ((lech + k * 3) % 5) * 0.09);
            var la = phanTu("path", {
                d: LA,
                transform: "translate(" + x + " " + y + ") rotate(" + goc + ") scale(" + tiLe.toFixed(2) + ")"
            });
            // Vài lá bắt nắng sáng hơn cho có khối
            if ((lech + k) % 4 === 1) la.setAttribute("class", "sang");
            g.appendChild(la);
        }
        g.appendChild(phanTu("circle", { cx: x, cy: y, r: (r * 0.62).toFixed(1) }));
    }

    // Lớp cây dọc (bên trái; bên phải là bản lật ngang bằng CSS): khối đặc tới mep, dọc mép trong là chùm lá chĩa vào giữa
    function lopDoc(svg, mep, buoc, dsR, dsLech, lop) {
        var g = phanTu("g", { "class": lop });
        g.appendChild(phanTu("rect", { x: -40, y: -60, width: mep + 40, height: 1120 }));
        for (var i = 0, y = -30; y <= 1040; y += buoc, i++) {
            chumLa(g, mep + dsLech[i % dsLech.length], y, dsR[i % dsR.length], 0, i * 5);
        }
        svg.appendChild(g);
    }

    // Lớp tán ngang (phía trên lá rủ xuống; phía dưới là bản lật dọc thành bụi cây mọc lên)
    function lopNgang(svg, mep, buoc, dsR, dsLech, lop) {
        var g = phanTu("g", { "class": lop });
        g.appendChild(phanTu("rect", { x: -60, y: -40, width: 1120, height: mep + 40 }));
        for (var i = 0, x = -30; x <= 1040; x += buoc, i++) {
            chumLa(g, x, mep + dsLech[i % dsLech.length], dsR[i % dsR.length], 90, i * 3);
        }
        svg.appendChild(g);
    }

    var dsSvg = rung.querySelectorAll("svg[data-rung]");
    for (var n = 0; n < dsSvg.length; n++) {
        var svg = dsSvg[n];
        if (svg.getAttribute("data-rung") === "doc") {
            lopDoc(svg, 450, 70, [62, 48, 70, 54, 66, 44], [10, -14, 22, -6, 16, -20], "rung-xa");
            lopDoc(svg, 360, 62, [54, 66, 46, 60, 72, 50], [-8, 18, -16, 12, -4, 20], "rung-giua");
            lopDoc(svg, 260, 56, [56, 44, 64, 50, 60, 68], [14, -10, 6, -18, 20, -2], "rung-gan");
        } else {
            lopNgang(svg, 110, 64, [56, 68, 48, 62, 72, 52], [6, -12, 18, -4, 10, -16], "rung-xa");
            lopNgang(svg, 56, 56, [48, 60, 42, 56, 64, 46], [-6, 14, -10, 8, -14, 12], "rung-gan");
        }
    }

    // Trang dựng xong và rừng đã hiện ít nhất một chút thì mở; mở xong gỡ hẳn lớp phủ
    var batDau = Date.now();
    var daMo = false;
    function mo() {
        if (daMo) return;
        daMo = true;
        setTimeout(function () {
            rung.classList.add("mo");
            setTimeout(function () { if (rung.parentNode) rung.parentNode.removeChild(rung); }, 1300);
        }, Math.max(0, 450 - (Date.now() - batDau)));
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", mo);
    } else {
        mo();
    }

    // Quay lại bằng nút Back (trang lấy từ bộ nhớ đệm): không hiện lại rừng
    window.addEventListener("pageshow", function (e) {
        if (e.persisted && rung.parentNode) rung.parentNode.removeChild(rung);
    });
})();
