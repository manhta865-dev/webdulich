// Bấm vào ảnh thì mới tạo khung phát của YouTube, dùng tên miền nocookie để giảm cookie theo dõi.
// Chưa chạy được JS thì liên kết vẫn mở video trên YouTube.
(function () {
    document.addEventListener("click", function (e) {
        var lienKet = e.target.closest ? e.target.closest("a[data-video]") : null;
        if (!lienKet) return;
        // Ctrl hoặc Cmd + bấm: để trình duyệt mở tab mới như liên kết thường
        if (e.ctrlKey || e.metaKey || e.shiftKey) return;
        e.preventDefault();

        var khung = document.createElement("iframe");
        khung.src = "https://www.youtube-nocookie.com/embed/" + encodeURIComponent(lienKet.dataset.video) + "?autoplay=1&rel=0";
        khung.title = lienKet.dataset.tieuDe || "Video";
        khung.allow = "autoplay; encrypted-media; picture-in-picture";
        khung.allowFullscreen = true;
        khung.referrerPolicy = "strict-origin-when-cross-origin";
        khung.className = "vd-khung vd-dang-phat";
        lienKet.replaceWith(khung);
    });
})();
