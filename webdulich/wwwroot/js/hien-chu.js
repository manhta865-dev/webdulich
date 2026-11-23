// Trang chủ: cuộn tới phần nào thì chữ phần đó mới chạy ra (Views/TrangChu/Index.cshtml).
// - Tiêu đề h2 của từng phần: tách thành từng chữ, các chữ trượt lên lần lượt.
// - Câu mô tả ngay dưới tiêu đề và các khối nội dung của phần: hiện dần, trượt nhẹ lên.
// Mỗi thứ chỉ chạy một lần. Thứ đang nằm sẵn trong màn hình lúc mở trang thì để nguyên (tránh chớp).
// Không có IntersectionObserver hoặc máy bật giảm chuyển động: không làm gì, chữ hiện bình thường.
(function () {
    "use strict";
    if (!("IntersectionObserver" in window)) return;
    if (window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)").matches) return;

    var khung = document.querySelector(".home-wrap");
    if (!khung) return;

    var caoMan = window.innerHeight || document.documentElement.clientHeight;
    function dangHien(el) {
        var r = el.getBoundingClientRect();
        return r.top < caoMan && r.bottom > 0;
    }

    // Tách chữ của tiêu đề (chỉ tiêu đề toàn chữ, không có thẻ con) thành từng từ bọc span
    function tachChu(h) {
        if (h.children.length > 0) return false;
        var dsTu = h.textContent.trim().split(/\s+/);
        if (dsTu.length === 0) return false;
        h.setAttribute("aria-label", h.textContent.trim());
        h.textContent = "";
        dsTu.forEach(function (tu, i) {
            var vo = document.createElement("span");
            vo.className = "hc-tu";
            vo.setAttribute("aria-hidden", "true");
            var ruot = document.createElement("span");
            ruot.textContent = tu;
            ruot.style.setProperty("--i", i);
            vo.appendChild(ruot);
            h.appendChild(vo);
            if (i < dsTu.length - 1) h.appendChild(document.createTextNode(" "));
        });
        return true;
    }

    var theoDoi = new IntersectionObserver(function (ds) {
        ds.forEach(function (muc) {
            if (!muc.isIntersecting) return;
            muc.target.classList.add("hc-hien");
            theoDoi.unobserve(muc.target);
        });
    }, { rootMargin: "0px 0px -12% 0px", threshold: 0.15 });

    // Tiêu đề từng phần
    khung.querySelectorAll(".tv-section h2").forEach(function (h) {
        if (dangHien(h)) return;
        if (tachChu(h)) {
            h.classList.add("hc-tieu-de");
            theoDoi.observe(h);
        }
    });

    // Câu mô tả dưới tiêu đề và các khối nội dung của từng phần (không lấy khối chứa tiêu đề)
    khung.querySelectorAll(".tv-section").forEach(function (phan) {
        var thuTu = 0;
        Array.prototype.forEach.call(phan.children, function (con) {
            if (dangHien(con)) return;
            var laDau = con.matches(".tc-tieu-de, .tv-section-head") || con.tagName === "H2";
            if (laDau) {
                // Chỉ cho câu mô tả trong khối đầu trượt lên, tiêu đề đã có hiệu ứng riêng
                con.querySelectorAll(":scope > p, :scope > div > p").forEach(function (p) {
                    p.classList.add("hc-khoi");
                    p.style.setProperty("--tre", "260ms");
                    theoDoi.observe(p);
                });
                return;
            }
            con.classList.add("hc-khoi");
            con.style.setProperty("--tre", Math.min(thuTu, 3) * 90 + "ms");
            thuTu++;
            theoDoi.observe(con);
        });
    });
})();
