// Hộp xác nhận cùng kiểu với web, thay cho confirm() mặc định của trình duyệt.
// Cách dùng: thêm vào <form> các thuộc tính
//   data-xac-nhan="Câu giải thích"            (bắt buộc)
//   data-xac-nhan-tieu-de="Bớt ghế 5?"        (không bắt buộc)
//   data-xac-nhan-nut="Bớt ghế"               (chữ trên nút đồng ý, mặc định "Đồng ý")
//   data-xac-nhan-nguy-hiem                   (nút đồng ý màu đỏ, cho thao tác không hoàn tác được; ="false" để tắt)
(function () {
    var hop, tieuDe, noiDung, nutDongY, formDangCho;

    function taoHop() {
        hop = document.createElement("dialog");
        hop.className = "xn-hop";
        hop.setAttribute("aria-labelledby", "xnTieuDe");
        hop.innerHTML =
            '<h2 class="xn-tieu-de" id="xnTieuDe"></h2>' +
            '<p class="xn-noi-dung"></p>' +
            '<div class="xn-nut">' +
            '<button type="button" class="tv-btn tv-btn-vien" data-xn="huy">Không, quay lại</button>' +
            '<button type="button" class="tv-btn tv-btn-chinh" data-xn="dong-y"></button>' +
            "</div>";
        document.body.appendChild(hop);
        tieuDe = hop.querySelector(".xn-tieu-de");
        noiDung = hop.querySelector(".xn-noi-dung");
        nutDongY = hop.querySelector('[data-xn="dong-y"]');

        hop.querySelector('[data-xn="huy"]').addEventListener("click", function () { hop.close(); });
        nutDongY.addEventListener("click", function () {
            var f = formDangCho;
            hop.close();
            // submit() không phát lại sự kiện submit nên không mở hộp lần nữa
            if (f) f.submit();
        });
        // Bấm ra ngoài hộp thì đóng như bấm "Không"
        hop.addEventListener("click", function (e) { if (e.target === hop) hop.close(); });
        hop.addEventListener("close", function () { formDangCho = null; });
    }

    document.addEventListener("submit", function (e) {
        var f = e.target;
        if (!(f instanceof HTMLFormElement) || !f.hasAttribute("data-xac-nhan")) return;
        e.preventDefault();

        // Trình duyệt quá cũ không có <dialog>: dùng hộp mặc định cho chắc
        if (typeof HTMLDialogElement !== "function") {
            if (window.confirm(f.dataset.xacNhan)) f.submit();
            return;
        }

        if (!hop) taoHop();
        formDangCho = f;
        tieuDe.textContent = f.dataset.xacNhanTieuDe || "Bạn chắc chứ?";
        noiDung.textContent = f.dataset.xacNhan;
        nutDongY.textContent = f.dataset.xacNhanNut || "Đồng ý";
        var nguyHiem = f.hasAttribute("data-xac-nhan-nguy-hiem") && f.dataset.xacNhanNguyHiem !== "false";
        nutDongY.classList.toggle("xn-nguy-hiem", nguyHiem);
        hop.showModal();
        // Thao tác không hoàn tác: con trỏ đặt sẵn ở nút "Không" để lỡ bấm Enter cũng không mất gì
        (nguyHiem ? hop.querySelector('[data-xn="huy"]') : nutDongY).focus();
    });
})();
