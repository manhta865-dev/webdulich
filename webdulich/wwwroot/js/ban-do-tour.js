// Bản đồ tour (DiaDiem/BanDo): chấm các điểm đến đang bán tour, mở ở Việt Nam, thu nhỏ ra thấy cả tour nước ngoài.
// Nền bản đồ OpenFreeMap (có nút chuyển sang ảnh vệ tinh), bỏ chữ và đường ranh giới của bản đồ gốc (cùng cách với hanh-trinh-3d.js),
// tên điểm và nhãn Hoàng Sa, Trường Sa do TravelVN tự ghi. Lỗi thì giữ danh sách bên cạnh, không mất thông tin.
const KIEU_BAN_DO = "https://tiles.openfreemap.org/styles/liberty";
const khung = document.getElementById("banDoTour");

if (khung) {
    batDau().catch(() => {
        khung.innerHTML = '<p class="kp-bd-cho">Chưa tải được bản đồ. Bạn xem danh sách tour theo vùng bên cạnh nhé.</p>';
    });
}

function taiThuVien() {
    if (window.maplibregl) return Promise.resolve();
    return new Promise((xong, loi) => {
        const css = document.createElement("link");
        css.rel = "stylesheet";
        css.href = "/lib/maplibre/maplibre-gl.css?v=4.7.1";
        document.head.appendChild(css);
        const js = document.createElement("script");
        js.src = "/lib/maplibre/maplibre-gl.js?v=4.7.1";
        js.onload = xong;
        js.onerror = loi;
        document.head.appendChild(js);
    });
}

async function batDau() {
    if (!document.createElement("canvas").getContext("webgl2")) throw new Error("Không có WebGL2");
    const dsTour = JSON.parse(khung.dataset.dulieu || "[]");
    await taiThuVien();
    const phanHoi = await fetch(KIEU_BAN_DO, { signal: AbortSignal.timeout(8000) });
    if (!phanHoi.ok) throw new Error("Dịch vụ bản đồ báo lỗi");
    const kieu = await phanHoi.json();
    kieu.layers = kieu.layers.filter((l) => l.type !== "symbol" && !/boundary/.test(l.id));

    khung.innerHTML = "";
    const map = new maplibregl.Map({
        container: khung,
        style: kieu,
        // Mở ở Việt Nam; thu nhỏ ra được cả thế giới để thấy tour nước ngoài
        bounds: [[101.8, 8.0], [116.2, 23.6]],
        fitBoundsOptions: { padding: 24 },
        minZoom: 1,
        renderWorldCopies: false,
        dragRotate: false,
        pitchWithRotate: false,
        touchPitch: false,
        cooperativeGestures: !window.matchMedia("(pointer: fine)").matches,
        attributionControl: { compact: true },
        locale: {
            "NavigationControl.ZoomIn": "Phóng to",
            "NavigationControl.ZoomOut": "Thu nhỏ",
            "AttributionControl.ToggleAttribution": "Nguồn dữ liệu bản đồ",
            "CooperativeGesturesHandler.MobileHelpText": "Dùng hai ngón để di chuyển bản đồ"
        }
    });
    map.touchZoomRotate.disableRotation();
    map.addControl(new maplibregl.NavigationControl({ showCompass: false }), "top-right");

    // Nút "Bản đồ | Vệ tinh" (js/nen-ve-tinh.js); các chấm tour là phần tử HTML nên luôn nằm trên ảnh. Lỗi thì bỏ qua.
    // Đặt dưới nút phóng to: góc trên trái là Sapa, Hà Giang, sẽ bị che.
    map.once("load", async () => {
        try {
            (await import("./nen-ve-tinh.js?v=3")).themNenVeTinh(map, { viTri: "top-right" });
        } catch (loi) {
            // Không có nút vệ tinh, giữ nền bản đồ
        }
    });

    // Hoàng Sa, Trường Sa: nhãn tự ghi, không bấm được
    [["Hoàng Sa", 112.0, 16.5], ["Trường Sa", 114.2, 10.0]].forEach(([chu, kinh, vi]) => {
        const el = document.createElement("div");
        el.className = "kp-dao";
        el.textContent = chu;
        new maplibregl.Marker({ element: el }).setLngLat([kinh, vi]).addTo(map);
    });

    // Gom các tour cùng điểm đến vào một chấm
    const theoDiem = new Map();
    dsTour.forEach((t) => {
        const khoa = t.DiemDen.toLowerCase();
        if (!theoDiem.has(khoa)) theoDiem.set(khoa, { ten: t.DiemDen, vi: t.Vi, kinh: t.Kinh, ds: [] });
        theoDiem.get(khoa).ds.push(t);
    });

    const popup = new maplibregl.Popup({ closeButton: true, offset: 18, maxWidth: "300px", className: "kp-popup" });
    const diemTheoTour = new Map();

    function moDiem(d) {
        const the = d.ds.map((t) => `
            <a class="kp-pop-tour" href="${t.Link}">
                ${t.Anh ? `<img src="${t.Anh}" alt="" loading="lazy" />` : ""}
                <span class="kp-pop-chu">
                    <strong>${t.TenTour}</strong>
                    <span>${t.Gia ? "Từ " + t.Gia : "Chưa có lịch mở bán"}</span>
                    ${t.Ngay ? `<span>Gần nhất: ${t.Ngay}${t.SoLich > 1 ? `, ${t.SoLich} lịch` : ""}</span>` : ""}
                </span>
            </a>`).join("");
        popup.setLngLat([d.kinh, d.vi]).setHTML(`<p class="kp-pop-dau">${d.ten}</p>${the}`).addTo(map);
    }

    theoDiem.forEach((d) => {
        const el = document.createElement("button");
        el.type = "button";
        el.className = "kp-cham" + (d.ds.some((t) => t.Gia) ? "" : " kp-cham-trong");
        el.setAttribute("aria-label", `${d.ten}: ${d.ds.length} tour`);
        el.innerHTML = `<span class="kp-cham-nut"></span><span class="kp-cham-ten">${d.ten}</span>`;
        el.addEventListener("click", (e) => { e.stopPropagation(); moDiem(d); });
        new maplibregl.Marker({ element: el, anchor: "bottom" }).setLngLat([d.kinh, d.vi]).addTo(map);
        // MapLibre tự đặt nhãn "Map marker", ghi lại tên điểm cho trình đọc màn hình
        el.setAttribute("aria-label", `${d.ten}: ${d.ds.length} tour`);
        d.ds.forEach((t) => diemTheoTour.set(String(t.Id), d));
    });

    // Bấm ở danh sách bên cạnh: bay tới điểm và mở thẻ tour
    document.querySelectorAll(".kp-bd-muc").forEach((nut) => {
        nut.addEventListener("click", () => {
            const d = diemTheoTour.get(nut.dataset.tour);
            if (!d) return;
            document.querySelectorAll(".kp-bd-muc.active").forEach((n) => n.classList.remove("active"));
            nut.classList.add("active");
            // Điểm xa (nước ngoài) bay nhanh hơn để không phải chờ lâu
            map.flyTo({ center: [d.kinh, d.vi], zoom: Math.max(map.getZoom(), 7), speed: 1.2, maxDuration: 3000 });
            map.once("moveend", () => moDiem(d));
        });
    });
}
