// Màn hình chờ "lá dừa" (Views/Shared/_ManHinhCho.cshtml). Hình lá đã có sẵn trong HTML; file này chỉ lo việc mở:
// khi trang dựng xong (không đợi ảnh) và lá đã che ít nhất một chút thì thêm lớp .mo để lá tỏa ra, xong thì gỡ lớp phủ.
// Nạp kiểu defer nên không chặn trang đang dựng.
(function () {
    "use strict";
    var rung = document.getElementById("tvRung");
    if (!rung) return;

    // Tính từ lúc trình duyệt bắt đầu tải trang, không phải lúc file này chạy
    var CHE_TOI_THIEU = 400;
    var daMo = false;

    function mo() {
        if (daMo) return;
        daMo = true;
        var daQua = window.performance && performance.now ? performance.now() : 0;
        setTimeout(function () {
            // Đợi trình duyệt vẽ xong một khung hình rồi mới bắt đầu để chuyển động trơn tru từ đầu
            requestAnimationFrame(function () {
                requestAnimationFrame(function () {
                    rung.classList.add("mo");
                    setTimeout(function () { if (rung.parentNode) rung.parentNode.removeChild(rung); }, 1100);
                });
            });
        }, Math.max(0, CHE_TOI_THIEU - daQua));
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
