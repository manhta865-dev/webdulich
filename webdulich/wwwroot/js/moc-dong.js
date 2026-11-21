// Đóng mộc lên các tấm vé khi khối chứa chúng cuộn tới: mỗi khối [data-moc-dong] một lần, lần lượt từ trên xuống.
// Lớp .cho-dong chỉ thêm bằng JS (CSS ở ve.css ẩn mộc khi có lớp này), nên không có JS thì mộc vẫn hiện sẵn.
// Người dùng chọn giảm chuyển động thì không làm gì.
(function () {
    if (!("IntersectionObserver" in window)
        || window.matchMedia("(prefers-reduced-motion: reduce)").matches) return;

    document.querySelectorAll("[data-moc-dong]").forEach(function (khoi) {
        var dsMoc = khoi.querySelectorAll(".tv-moc");
        if (!dsMoc.length) return;

        khoi.classList.add("cho-dong");
        var quanSat = new IntersectionObserver(function (muc) {
            if (!muc[0].isIntersecting) return;
            quanSat.disconnect();
            dsMoc.forEach(function (moc, k) {
                moc.style.animationDelay = (k * 160) + "ms";
                moc.classList.add("tv-moc-dong");
            });
        }, { rootMargin: "0px 0px -20% 0px" }); // khối dài hơn màn hình vẫn kích hoạt được

        quanSat.observe(khoi);
    });
})();
