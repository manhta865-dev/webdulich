// Video lớn ở trang chủ: tự chạy không tiếng khi cuộn tới, giống video quảng cáo chạy nền.
// Chỉ tạo khung phát của YouTube lúc khung lọt vào màn hình. Không tự chạy khi người xem chọn giảm chuyển động
// hoặc bật chế độ tiết kiệm dữ liệu; khi đó vẫn có ảnh và nút xem có tiếng.
(function () {
    var khung = document.querySelector("[data-video-nen]");
    if (!khung) return;
    var ma = khung.getAttribute("data-video-nen");
    var nutTieng = khung.querySelector("[data-video-bat-tieng]");

    function taoKhungPhat(coTieng) {
        var cu = khung.querySelector("iframe");
        if (cu) cu.remove();
        var tham = coTieng
            ? "autoplay=1&rel=0&playsinline=1"
            : "autoplay=1&mute=1&loop=1&playlist=" + encodeURIComponent(ma) + "&controls=0&rel=0&playsinline=1&disablekb=1";
        var f = document.createElement("iframe");
        f.src = "https://www.youtube-nocookie.com/embed/" + encodeURIComponent(ma) + "?" + tham;
        f.title = "Video giới thiệu" + (coTieng ? "" : ", đang chạy không tiếng");
        f.allow = "autoplay; encrypted-media; picture-in-picture";
        f.allowFullscreen = true;
        f.referrerPolicy = "strict-origin-when-cross-origin";
        if (!coTieng) {
            // Video chạy nền không nhận chuột, để bấm vào khung không mở trang YouTube
            f.tabIndex = -1;
            f.setAttribute("aria-hidden", "true");
        }
        khung.classList.toggle("co-tieng", coTieng);
        khung.appendChild(f);
    }

    if (nutTieng) {
        nutTieng.addEventListener("click", function (e) {
            if (e.ctrlKey || e.metaKey || e.shiftKey) return;
            e.preventDefault();
            taoKhungPhat(true);
        });
    }

    var giamChuyenDong = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    var tietKiem = navigator.connection && navigator.connection.saveData;
    if (giamChuyenDong || tietKiem || !("IntersectionObserver" in window)) return;

    var quanSat = new IntersectionObserver(function (muc) {
        if (muc[0].isIntersecting) {
            quanSat.disconnect();
            if (!khung.querySelector("iframe")) taoKhungPhat(false);
        }
    }, { threshold: 0.35 });
    quanSat.observe(khung);
})();
