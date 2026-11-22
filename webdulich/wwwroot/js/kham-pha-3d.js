// Trang Khám phá 3D (Views/DiaDiem/KhamPha.cshtml): ba tab Bản đồ 3D, Google Maps, Đi dạo 360°.
// Bản đồ 3D dùng MapLibre như trang chi tiết tour (thư viện, kiểu bản đồ, địa hình, nút vệ tinh dùng chung), nhưng được xoay,
// nghiêng, phóng to tự do vì chiếm cả màn hình. Chọn một ngày: camera bay tới, xe chạy hết đoạn đường của ngày đó.
// "Dẫn đường" hỏi máy chủ TravelVN (DiaDiem/ChiDuong, máy chủ hỏi OSRM và dịch sang tiếng Việt). Bản đồ lỗi thì các tab khác vẫn dùng được.
const duLieu = JSON.parse(document.getElementById("k3DuLieu").textContent);
const dsNgay = duLieu.DsNgay;
const giamChuyenDong = window.matchMedia("(prefers-reduced-motion: reduce)").matches;

// MapLibre 4.7: mỗi lần bay camera thư viện bật cờ giữ độ cao và chỉ tắt lại khi có freezeElevation: true.
// Không có thì độ cao camera kẹt ở 0 m, bay sát xuống vùng núi (Đà Lạt cao khoảng 1.500 m) là camera chui vào trong địa hình.
const CO_DINH_DO_CAO = true;
const KIEU_BAN_DO = "https://tiles.openfreemap.org/styles/liberty";
const DIA_HINH = "https://s3.amazonaws.com/elevation-tiles-prod/terrarium/{z}/{x}/{y}.png";

let chiSo = 0;          // ngày đang chọn
let diemChon = null;    // điểm tham quan đang chọn trong ngày (chỉ số), null là đang xem cả ngày
let banDo = null;       // đối tượng điều khiển bản đồ, null nếu bản đồ lỗi
let moChiDuong = false; // panel chỉ đường đang mở
const nhoChiDuong = new Map();

// ---------- Tab ----------
const dsTab = document.querySelectorAll(".k3-tab [role=tab]");
dsTab.forEach((tab) => tab.addEventListener("click", () => moTab(tab.dataset.tab)));

function moTab(ten) {
    dsTab.forEach((tab) => {
        const dung = tab.dataset.tab === ten;
        tab.setAttribute("aria-selected", String(dung));
        document.getElementById(tab.getAttribute("aria-controls")).hidden = !dung;
    });
    document.body.dataset.tab = ten;
    if (ten === "google" || ten === "di-360") capNhatGoogle(ten);
    // Bản đồ bị ẩn rồi hiện lại thì phải đo lại kích thước khung
    if (ten === "ban-do" && banDo) banDo.map.resize();
}

// ---------- Chọn ngày, chọn điểm tham quan ----------
const dsChipNgay = document.querySelectorAll("[data-diem]");
const dsChipDiem = document.querySelectorAll("[data-diem-tq]");
dsChipNgay.forEach((chip) => chip.addEventListener("click", () => chon(Number(chip.dataset.diem), true)));
dsChipDiem.forEach((chip) => chip.addEventListener("click", () => chonDiem(Number(chip.dataset.ngay), Number(chip.dataset.diemTq))));

// Nơi đang xem: điểm tham quan nếu có chọn, không thì điểm cuối của ngày
function muc() {
    const n = dsNgay[chiSo];
    const d = diemChon !== null ? n.DsDiem[diemChon] : null;
    return d
        ? { ten: d.Ten, Vi: d.Vi, Kinh: d.Kinh, LinkStreetView: d.LinkStreetView, LinkNhung: d.LinkNhung, Link360: d.Link360 }
        : { ten: "Ngày " + n.Ngay + ": " + n.TieuDe, Vi: n.Vi, Kinh: n.Kinh, LinkStreetView: n.LinkStreetView, LinkNhung: n.LinkNhung, Link360: n.Link360 };
}

function capNhatChip() {
    dsChipNgay.forEach((chip, j) => chip.setAttribute("aria-pressed", String(j === chiSo && diemChon === null)));
    dsChipDiem.forEach((chip) => {
        const dung = Number(chip.dataset.ngay) === chiSo && Number(chip.dataset.diemTq) === diemChon;
        chip.setAttribute("aria-pressed", String(dung));
        if (dung) chip.scrollIntoView({ block: "nearest", inline: "nearest" });
    });
}

function capNhatThe() {
    const n = dsNgay[chiSo];
    const d = diemChon !== null ? n.DsDiem[diemChon] : null;
    const the = document.getElementById("k3The");
    the.querySelector('[data-the="ngay"]').textContent = "NGÀY " + n.Ngay + (d ? ", ĐIỂM " + (diemChon + 1) : "");
    the.querySelector('[data-the="tieu-de"]').textContent = d ? d.Ten : n.TieuDe;
    const phu = the.querySelector('[data-the="phu"]');
    phu.textContent = d ? n.TieuDe : "";
    phu.hidden = !d;
    const bua = the.querySelector('[data-the="bua-an"]');
    bua.textContent = !d && n.BuaAn ? "Bữa ăn: " + n.BuaAn : "";
    bua.hidden = !!d || !n.BuaAn;
    the.querySelector('[data-the="google"]').href = n.LinkChiDuong;
}

function chon(i, tuNguoiXem) {
    const doiNgay = i !== chiSo;
    chiSo = i;
    diemChon = null;
    capNhatChip();
    capNhatThe();
    capNhatGoogle(null);
    if (moChiDuong && (doiNgay || tuNguoiXem === null)) taiChiDuong();
    if (banDo && tuNguoiXem !== null) banDo.bayToi(i);
}

function chonDiem(i, k) {
    const doiNgay = i !== chiSo;
    chiSo = i;
    diemChon = k;
    capNhatChip();
    capNhatThe();
    capNhatGoogle(null);
    if (moChiDuong && doiNgay) taiChiDuong();
    if (banDo) banDo.bayToiDiem(i, k);
}

// ---------- Google Maps, Street View ----------
// tabMo: tab vừa mở ("google", "di-360") hoặc null. Khung chỉ được tải khi tab của nó mở (hoặc đã mở trước đó).
function capNhatGoogle(tabMo) {
    const m = muc();
    document.querySelectorAll('[data-gg="ten"]').forEach((el) => { el.textContent = m.ten; });
    document.querySelectorAll('[data-gg="toa-do"]').forEach((el) => { el.textContent = m.Vi.toFixed(4) + ", " + m.Kinh.toFixed(4); });
    document.querySelectorAll('[data-gg="chi-duong"]').forEach((el) => { el.href = dsNgay[chiSo].LinkChiDuong; });
    document.querySelectorAll('[data-gg="street-view"]').forEach((el) => { el.href = m.LinkStreetView; });
    [["khung", "google", m.LinkNhung], ["khung-360", "di-360", m.Link360]].forEach(([ten, tab, link]) => {
        const khung = document.querySelector('[data-gg="' + ten + '"]');
        if (!khung) return;
        if ((tabMo === tab || khung.getAttribute("src")) && khung.getAttribute("src") !== link) khung.src = link;
    });
}

// ---------- Chỉ đường từng bước ----------
const nutDanDuong = document.querySelector('[data-tac="dan-duong"]');
const panelChiDuong = document.getElementById("k3ChiDuong");
nutDanDuong.addEventListener("click", () => {
    moChiDuong = !moChiDuong;
    nutDanDuong.setAttribute("aria-pressed", String(moChiDuong));
    panelChiDuong.hidden = !moChiDuong;
    document.getElementById("k3The").classList.toggle("mo-chi-duong", moChiDuong);
    if (moChiDuong) taiChiDuong();
});

function dinhKm(km) {
    return km < 1 ? Math.round(km * 1000) + " m" : km.toLocaleString("vi-VN", { maximumFractionDigits: 1 }) + " km";
}

function dinhPhut(phut) {
    const gio = Math.floor(phut / 60), con = Math.round(phut % 60);
    return gio > 0 ? gio + " giờ" + (con > 0 ? " " + con + " phút" : "") : Math.max(1, con) + " phút";
}

async function taiChiDuong() {
    const ngay = dsNgay[chiSo].Ngay;
    const tong = panelChiDuong.querySelector('[data-cd="tong"]');
    const ds = panelChiDuong.querySelector('[data-cd="ds"]');
    ds.replaceChildren();

    let kq = nhoChiDuong.get(ngay);
    if (!kq) {
        tong.textContent = "Đang lấy chỉ đường…";
        try {
            const phanHoi = await fetch(duLieu.UrlChiDuong + "?id=" + duLieu.TourId + "&ngay=" + ngay, { signal: AbortSignal.timeout(12000) });
            kq = await phanHoi.json();
            if (!phanHoi.ok) throw new Error(kq && kq.loi);
            nhoChiDuong.set(ngay, kq);
        } catch (loi) {
            // Người xem đã chọn ngày khác trong lúc chờ thì không ghi đè
            if (dsNgay[chiSo].Ngay !== ngay) return;
            tong.textContent = (loi && loi.message) || "Chưa lấy được chỉ đường. Bạn thử lại sau, hoặc mở Google Maps.";
            return;
        }
    }
    if (dsNgay[chiSo].Ngay !== ngay) return;

    tong.textContent = "Quãng đường " + dinhKm(kq.km) + ", xe chạy khoảng " + dinhPhut(kq.phut) + ".";
    kq.buoc.forEach((b) => {
        const li = document.createElement("li");
        const chu = document.createElement("span");
        chu.textContent = b.chu;
        li.appendChild(chu);
        if (b.km > 0) {
            const km = document.createElement("small");
            km.className = "tv-ma";
            km.textContent = dinhKm(b.km);
            li.appendChild(km);
        }
        ds.appendChild(li);
    });
}

// ---------- Bay vòng, toàn cảnh ----------
const nutBayVong = document.querySelector('[data-tac="bay-vong"]');
nutBayVong.addEventListener("click", () => {
    if (!banDo) return;
    if (banDo.dangBayVong()) banDo.dungBayVong();
    else banDo.bayVong();
});
document.querySelector('[data-tac="toan-canh"]').addEventListener("click", () => banDo && banDo.toanCanh());

function baoBayVong(dang) {
    nutBayVong.setAttribute("aria-pressed", String(dang));
}

// ---------- Bản đồ ----------
function taiThuVienBanDo() {
    if (window.maplibregl) return Promise.resolve();
    return new Promise((xong, loi) => {
        const css = document.createElement("link");
        css.rel = "stylesheet";
        css.href = "/lib/maplibre/maplibre-gl.css?v=4.7.1";
        document.head.appendChild(css);
        const js = document.createElement("script");
        js.src = "/lib/maplibre/maplibre-gl.js?v=4.7.1";
        js.onload = xong;
        js.onerror = () => loi(new Error("Không tải được thư viện bản đồ"));
        document.head.appendChild(js);
    });
}

function tinhKm(a, b) {
    const R = Math.PI / 180;
    const x = (b[0] - a[0]) * R * Math.cos((a[1] + b[1]) / 2 * R), y = (b[1] - a[1]) * R;
    return Math.hypot(x, y) * 6371;
}

function tinhHuong(a, b) {
    const R = Math.PI / 180;
    return Math.atan2((b[0] - a[0]) * Math.cos((a[1] + b[1]) / 2 * R), b[1] - a[1]) / R;
}

// Mỗi ngày một đoạn: đường đã lưu trong CSDL, không có thì nối thẳng từ điểm trước
function taoDoan() {
    let truoc = duLieu.XuatPhat;
    return dsNgay.map((n) => {
        const den = [n.Kinh, n.Vi];
        let diem = null;
        try {
            diem = n.DuongDi ? JSON.parse(n.DuongDi) : null;
        } catch (loi) {
            diem = null;
        }
        const thang = !Array.isArray(diem) || diem.length < 2;
        if (thang) diem = [truoc, den];
        const moc = [0];
        for (let k = 1; k < diem.length; k++) moc.push(moc[k - 1] + tinhKm(diem[k - 1], diem[k]));
        truoc = den;
        return { diem, thang, den, moc, km: moc[moc.length - 1] };
    });
}

function viTriTrenDoan(doan, km) {
    const { diem, moc } = doan;
    if (km <= 0) return diem[0];
    if (km >= doan.km) return diem[diem.length - 1];
    let k = 1;
    while (moc[k] < km) k++;
    const t = (km - moc[k - 1]) / ((moc[k] - moc[k - 1]) || 1);
    return [diem[k - 1][0] + (diem[k][0] - diem[k - 1][0]) * t, diem[k - 1][1] + (diem[k][1] - diem[k - 1][1]) * t];
}

function khungCua(dsDiem) {
    const dsKinh = dsDiem.map((d) => d[0]), dsVi = dsDiem.map((d) => d[1]);
    return [[Math.min(...dsKinh) - 0.02, Math.min(...dsVi) - 0.02], [Math.max(...dsKinh) + 0.02, Math.max(...dsVi) + 0.02]];
}

// Chừa chỗ cho thẻ thông tin (trái trên máy tính, dưới trên điện thoại) và thanh nút ở đáy, đo theo kích thước thật của thẻ
function lePhu() {
    const hop = document.getElementById("k3BanDoVe").getBoundingClientRect();
    const the = document.getElementById("k3The").getBoundingClientRect();
    if (window.matchMedia("(max-width: 760px)").matches) {
        // Không để phần chừa ăn hết chiều cao: luôn còn ít nhất 120px để thấy đường
        const duoi = Math.min(Math.max(140, hop.bottom - the.top + 16), hop.height - 170);
        return { top: 40, bottom: Math.max(40, duoi), left: 30, right: 30 };
    }
    return { top: 70, bottom: 110, left: Math.max(60, the.right - hop.left + 30), right: 70 };
}

async function taoBanDo() {
    if (!document.createElement("canvas").getContext("webgl2")) throw new Error("Trình duyệt không hỗ trợ WebGL2");
    await taiThuVienBanDo();

    const phanHoi = await fetch(KIEU_BAN_DO, { signal: AbortSignal.timeout(8000) });
    if (!phanHoi.ok) throw new Error("Dịch vụ bản đồ báo lỗi " + phanHoi.status);
    const kieu = await phanHoi.json();
    // Bỏ chữ và đường ranh giới của bản đồ gốc như trang tour: tên điểm do TravelVN tự ghi
    kieu.layers = kieu.layers.filter((l) => l.type !== "symbol" && !/boundary/.test(l.id));

    const dsDoan = taoDoan();
    const tatCa = dsDoan.flatMap((d) => d.diem);
    const khungTatCa = khungCua(tatCa);

    const map = new maplibregl.Map({
        container: "k3BanDoVe",
        style: kieu,
        bounds: khungTatCa,
        fitBoundsOptions: { padding: lePhu() },
        maxPitch: 75,
        attributionControl: { compact: true },
        locale: {
            "NavigationControl.ZoomIn": "Phóng to",
            "NavigationControl.ZoomOut": "Thu nhỏ",
            "NavigationControl.ResetBearing": "Xoay về hướng bắc",
            "AttributionControl.ToggleAttribution": "Nguồn dữ liệu bản đồ"
        }
    });
    map.addControl(new maplibregl.NavigationControl({ visualizePitch: true }), "top-right");

    await new Promise((xong, loi) => {
        const hen = setTimeout(() => loi(new Error("Bản đồ tải quá lâu")), 20000);
        map.once("load", () => { clearTimeout(hen); xong(); });
    });

    map.addSource("diaHinh", { type: "raster-dem", tiles: [DIA_HINH], encoding: "terrarium", tileSize: 256, maxzoom: 13, attribution: "Địa hình: Mapzen, AWS Open Data" });
    map.setTerrain({ source: "diaHinh", exaggeration: 1.4 });
    map.addLayer({
        id: "bongDoi",
        type: "hillshade",
        source: "diaHinh",
        paint: { "hillshade-exaggeration": 0.35, "hillshade-shadow-color": "#5F6F5A", "hillshade-highlight-color": "#FFFFFF" }
    }, map.getLayer("water") ? "water" : undefined);
    map.setSky && map.setSky({ "sky-color": "#BFE3F0", "horizon-color": "#E8F4F7", "sky-horizon-blend": 0.6 });

    try {
        (await import("./nen-ve-tinh.js?v=3")).themNenVeTinh(map, { viTri: "top-left" });
    } catch (loi) {
        // Không có nút vệ tinh, giữ nền bản đồ
    }

    const coDuongThat = dsDoan.some((d) => !d.thang);
    map.addSource("hanhTrinh", {
        type: "geojson",
        attribution: coDuongThat ? "Đường đi: OSRM, dữ liệu OpenStreetMap" : undefined,
        data: {
            type: "FeatureCollection",
            features: dsDoan.map((d, k) => ({ type: "Feature", properties: { thang: d.thang, ngay: k }, geometry: { type: "LineString", coordinates: d.diem } }))
        }
    });
    const nen = { "line-cap": "round", "line-join": "round" };
    map.addLayer({ id: "htVien", type: "line", source: "hanhTrinh", layout: nen, paint: { "line-color": "#FFFFFF", "line-width": 7 } });
    map.addLayer({ id: "htDuong", type: "line", source: "hanhTrinh", filter: ["==", ["get", "thang"], false], layout: nen, paint: { "line-color": "#7C8A91", "line-width": 4 } });
    map.addLayer({ id: "htThang", type: "line", source: "hanhTrinh", filter: ["==", ["get", "thang"], true], layout: { "line-cap": "round" }, paint: { "line-color": "#7C8A91", "line-width": 3, "line-dasharray": [2, 1.5] } });
    // Đoạn của ngày đang chọn tô nổi, quãng xe đã chạy tô cam
    map.addLayer({ id: "htChon", type: "line", source: "hanhTrinh", filter: ["==", ["get", "ngay"], 0], layout: nen, paint: { "line-color": "#0A6E8A", "line-width": 5 } });
    map.addSource("daDi", { type: "geojson", data: { type: "Feature", geometry: { type: "LineString", coordinates: [] } } });
    map.addLayer({ id: "daDi", type: "line", source: "daDi", layout: nen, paint: { "line-color": "#E4572E", "line-width": 5 } });

    function themMoc(chu, toaDo) {
        const el = document.createElement("div");
        el.className = "k3-moc";
        el.textContent = chu;
        new maplibregl.Marker({ element: el, anchor: "bottom", opacityWhenCovered: "1" }).setLngLat(toaDo).addTo(map);
        return el;
    }
    themMoc("Xuất phát", duLieu.XuatPhat).classList.add("xuat-phat");
    const dsMoc = dsNgay.map((n, k) => themMoc("Ngày " + n.Ngay, dsDoan[k].den));
    // Ghim điểm tham quan: nhỏ hơn nhãn ngày, bấm vào là chọn điểm đó
    const dsMocDiem = dsNgay.map((n, k) => n.DsDiem.map((d, j) => {
        const el = themMoc(d.Ten, [d.Kinh, d.Vi]);
        el.classList.add("diem");
        el.setAttribute("role", "button");
        el.addEventListener("click", () => chonDiem(k, j));
        return el;
    }));
    function danhDauDiem(k, j) {
        dsMocDiem.forEach((ds, a) => ds.forEach((el, b) => el.classList.toggle("chon", a === k && b === j)));
    }

    const elXe = document.createElement("div");
    elXe.className = "k3-xe";
    elXe.innerHTML = '<svg viewBox="0 0 24 44" width="20" height="36" aria-hidden="true"><rect x="2" y="2" width="20" height="40" rx="6" fill="#E4572E" stroke="#FFFFFF" stroke-width="2"/><rect x="5" y="7" width="14" height="7" rx="2" fill="#BDE3EC"/><rect x="5" y="32" width="14" height="5" rx="2" fill="#BDE3EC"/></svg>';
    const xe = new maplibregl.Marker({ element: elXe, rotationAlignment: "map", pitchAlignment: "map", opacityWhenCovered: "1" })
        .setLngLat(duLieu.XuatPhat).addTo(map);

    // Dòng nguồn thu gọn thành nút "i"
    map.getContainer().querySelector(".maplibregl-ctrl-attrib")?.classList.remove("maplibregl-compact-show");

    let luot = 0, vong = null;

    // fitBounds của MapLibre 4 không nhận góc nghiêng: tính khung nhìn thẳng rồi bay tới với góc nghiêng,
    // lùi zoom một chút vì nhìn nghiêng thì phần xa bị thu nhỏ
    function nhinKhung(khung, nghieng, huong, thoiGian) {
        const cam = map.cameraForBounds(khung, { padding: lePhu(), bearing: huong });
        if (!cam) return;
        map.flyTo({
            center: cam.center,
            zoom: Math.min(13.5, cam.zoom - nghieng / 110),
            bearing: huong,
            pitch: nghieng,
            duration: giamChuyenDong ? 0 : thoiGian,
            freezeElevation: CO_DINH_DO_CAO
        });
    }

    function chayXe(k, luotNay) {
        const doan = dsDoan[k];
        map.getSource("daDi").setData({ type: "Feature", geometry: { type: "LineString", coordinates: [] } });
        if (giamChuyenDong || doan.km < 0.05) {
            xe.setLngLat(doan.den);
            return;
        }
        const thoiGian = Math.min(12000, Math.max(3500, 3500 + doan.km * 25));
        const batDau = performance.now();
        const buoc = (t) => {
            if (luotNay !== luot) return;
            const ti = Math.min(1, (t - batDau) / thoiGian);
            const em = ti < 0.5 ? 2 * ti * ti : 1 - Math.pow(-2 * ti + 2, 2) / 2;
            const km = em * doan.km;
            const p = viTriTrenDoan(doan, km);
            xe.setLngLat(p);
            // Xe nằm theo mặt bản đồ nên góc xoay tính theo hướng bắc, không phụ thuộc góc camera
            xe.setRotation(tinhHuong(p, viTriTrenDoan(doan, km + Math.max(0.3, doan.km * 0.02))));
            // Quãng đã chạy: các điểm đã qua cộng vị trí hiện tại
            const daQua = doan.diem.filter((_, j) => doan.moc[j] <= km);
            map.getSource("daDi").setData({ type: "Feature", geometry: { type: "LineString", coordinates: daQua.concat([p]) } });
            if (ti < 1) requestAnimationFrame(buoc);
        };
        requestAnimationFrame(buoc);
    }

    function dungBayVong() {
        if (!vong) return;
        cancelAnimationFrame(vong);
        vong = null;
        baoBayVong(false);
    }
    // Người xem tự kéo, xoay, cuộn thì thôi bay vòng
    ["mousedown", "touchstart", "wheel"].forEach((su) => map.getCanvas().addEventListener(su, dungBayVong, { passive: true }));

    return {
        map,
        bayToi(k) {
            dungBayVong();
            const luotNay = ++luot;
            map.setFilter("htChon", ["==", ["get", "ngay"], k]);
            dsMoc.forEach((m, j) => m.classList.toggle("chon", j === k));
            danhDauDiem(-1, -1);
            const doan = dsDoan[k];
            xe.setLngLat(doan.diem[0]);
            // Đăng ký trước khi bay: bay tức thì (giảm chuyển động) thì moveend đến ngay trong lúc gọi
            map.once("moveend", () => { if (luotNay === luot) chayXe(k, luotNay); });
            nhinKhung(khungCua(doan.diem), 55, doan.km > 0.05 ? tinhHuong(doan.diem[0], doan.den) * 0.35 : map.getBearing(), 2200);
        },
        // Bay sát xuống một điểm tham quan, giữ hướng nhìn hiện tại; xe đứng yên.
        // Hai chặng: chặng 1 tới trên điểm ở độ cao an toàn, cuối chặng thư viện nhận đúng độ cao mặt đất tại điểm;
        // chặng 2 mới hạ xuống. Bay thẳng một lần từ vùng thấp xuống sát núi thì camera tính theo độ cao cũ và rơi xuống đất.
        bayToiDiem(k, j) {
            dungBayVong();
            const luotNay = ++luot;
            map.setFilter("htChon", ["==", ["get", "ngay"], k]);
            dsMoc.forEach((m, a) => m.classList.toggle("chon", a === k));
            danhDauDiem(k, j);
            const d = dsNgay[k].DsDiem[j];
            const tam = [d.Kinh, d.Vi];
            const ha = () => {
                if (luotNay !== luot) return;
                map.easeTo({ center: tam, zoom: 15.3, pitch: 60, duration: giamChuyenDong ? 0 : 1200, freezeElevation: CO_DINH_DO_CAO });
            };
            map.once("moveend", ha);
            map.flyTo({ center: tam, zoom: Math.min(12.8, map.getZoom()), pitch: 50, bearing: map.getBearing(), duration: giamChuyenDong ? 0 : 1600, freezeElevation: CO_DINH_DO_CAO });
        },
        toanCanh() {
            dungBayVong();
            nhinKhung(khungTatCa, 45, 0, 2000);
        },
        bayVong() {
            const m = muc();
            const tam = [m.Kinh, m.Vi];
            baoBayVong(true);
            map.easeTo({ center: tam, zoom: Math.max(map.getZoom(), 12.5), pitch: 62, duration: giamChuyenDong ? 0 : 1500, freezeElevation: CO_DINH_DO_CAO });
            if (giamChuyenDong) return;
            let truoc = null;
            const quay = (t) => {
                if (truoc !== null && !map.isEasing()) map.setBearing((map.getBearing() + (t - truoc) * 0.012) % 360);
                truoc = t;
                vong = requestAnimationFrame(quay);
            };
            vong = requestAnimationFrame(quay);
        },
        dangBayVong: () => vong !== null,
        dungBayVong
    };
}

// ---------- Khởi động ----------
// Trang tour có thể mở thẳng một điểm và một tab: ?ngay=chỉ số ngày&diem=chỉ số điểm&tab=di-360
const thamSo = new URLSearchParams(location.search);
const ngayMo = Number(thamSo.get("ngay")), diemMo = Number(thamSo.get("diem"));
const coDiemMo = thamSo.has("ngay") && thamSo.has("diem") && dsNgay[ngayMo] && dsNgay[ngayMo].DsDiem[diemMo];
chon(0, null);
if (coDiemMo) chonDiem(ngayMo, diemMo);
if (["google", "di-360"].includes(thamSo.get("tab"))) moTab(thamSo.get("tab"));
(async () => {
    const cho = document.getElementById("k3Cho");
    try {
        banDo = await taoBanDo();
        cho.hidden = true;
        if (coDiemMo) banDo.bayToiDiem(ngayMo, diemMo);
        else banDo.bayToi(0);
    } catch (loi) {
        console.warn("Không dùng được bản đồ 3D:", loi && loi.message);
        cho.textContent = "Chưa tải được bản đồ 3D trên trình duyệt này. Bạn vẫn xem được chỉ đường và tab Google Maps.";
        document.querySelectorAll('[data-tac="bay-vong"], [data-tac="toan-canh"]').forEach((nut) => { nut.disabled = true; });
    }
})();
