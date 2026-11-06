// Hành trình ở trang chi tiết tour, có hai cách vẽ dùng chung một bộ nút:
// - Tour có đủ điểm xuất phát và điểm mọi ngày: xe chạy trên bản đồ thật (MapLibre), đã bỏ chữ và ranh giới của bản đồ gốc,
//   khóa vùng xem và góc nhìn. Đường đi lấy từ CSDL; ngày chưa có đường thì nối bằng nét đứt thẳng.
// - Tour chưa đủ tọa độ, hoặc bản đồ lỗi: phong cảnh tự vẽ (three), là hình minh họa, không có hình đất nước.
// Khung này ẩn sẵn; lỗi ở đây không ảnh hưởng danh sách lịch trình bên dưới.
const goc = document.getElementById("hanhTrinh3D");
if (goc) {
    const moi = new IntersectionObserver((muc) => {
        if (muc.some((m) => m.isIntersecting)) {
            moi.disconnect();
            batDau();
        }
    }, { rootMargin: "300px" });
    moi.observe(goc.parentElement);
}

async function batDau() {
    const duLieu = JSON.parse(document.getElementById("hanhTrinhDuLieu").textContent);
    const dsNgay = duLieu.DsNgay;
    goc.hidden = false;

    let canh = null;
    if (duLieu.XuatPhat && dsNgay.every((n) => n.ViDo != null && n.KinhDo != null)) {
        try {
            canh = await taoBanDo(duLieu);
        } catch (loi) {
            console.warn("Không dùng được bản đồ, chuyển sang bản minh họa:", loi && loi.message);
        }
    }

    if (!canh) {
        // Bản đồ lỗi mà tour chỉ có một ngày: một cột mốc trên phong cảnh không có ý nghĩa, chỉ giữ danh sách
        if (dsNgay.length < 2) {
            goc.hidden = true;
            return;
        }
        try {
            const thu = document.createElement("canvas");
            if (!thu.getContext("webgl2") && !thu.getContext("webgl")) throw new Error("Không có WebGL");
            const THREE = await import("/lib/three/three.module.min.js");
            canh = taoCachDieu(THREE, dsNgay);
        } catch (loi) {
            goc.hidden = true;
            return;
        }
    }

    ganGiaoDien(dsNgay, canh);
}

// ---------- Bộ nút dùng chung. canh.bayToi(i, xong): bay tới ngày i, tới nơi thì gọi xong ----------
function ganGiaoDien(dsNgay, canh) {
    const nhomNut = goc.querySelector(".ct-ht-ngay");
    const theTieuDe = goc.querySelector(".ct-ht-tieu-de");
    const theBuaAn = goc.querySelector(".ct-ht-bua-an");
    const nutBay = goc.querySelector("[data-ht-bay]");
    goc.querySelector(".ct-ht-duoi small").textContent = canh.ghiChu;
    // Một ngày thì không cần nút chọn ngày; nút bay vẫn giữ để xem lại xe chạy
    nhomNut.hidden = dsNgay.length < 2;

    let ngayChon = 0, tuBay = false, henGio = null, luot = 0;

    function dungTuBay() {
        tuBay = false;
        nutBay.textContent = "Bay cả hành trình";
        if (henGio) clearTimeout(henGio);
    }

    function sangNgaySau() {
        if (ngayChon >= dsNgay.length - 1) {
            dungTuBay();
            return;
        }
        chon(ngayChon + 1, false);
    }

    function chon(i, tuNut) {
        ngayChon = i;
        if (henGio) clearTimeout(henGio);
        // Bấm ngày khác giữa đường thì lần bay cũ không được tính là đã tới
        const luotNay = ++luot;
        canh.bayToi(i, () => {
            if (luotNay === luot && tuBay) henGio = setTimeout(sangNgaySau, 1800);
        });

        const n = dsNgay[i];
        theTieuDe.textContent = "Ngày " + n.Ngay + ": " + n.TieuDe;
        theBuaAn.textContent = n.BuaAn ? "Bữa ăn: " + n.BuaAn : "";
        theBuaAn.hidden = !n.BuaAn;
        nhomNut.querySelectorAll("button").forEach((nut, j) => {
            nut.setAttribute("aria-pressed", j === i ? "true" : "false");
        });
        if (tuNut) {
            dungTuBay();
            // Mở đúng ngày trong danh sách chi tiết bên dưới
            const chiTiet = document.querySelector('.ct-day[data-ngay="' + n.Ngay + '"]');
            if (chiTiet) chiTiet.open = true;
        }
    }

    dsNgay.forEach((n, i) => {
        const nut = document.createElement("button");
        nut.type = "button";
        nut.textContent = "Ngày " + n.Ngay;
        nut.addEventListener("click", () => chon(i, true));
        nhomNut.appendChild(nut);
    });

    nutBay.addEventListener("click", () => {
        if (tuBay) {
            dungTuBay();
            return;
        }
        tuBay = true;
        nutBay.textContent = "Dừng lại";
        chon(0, false);
    });

    chon(0, false);
}

// ---------- Bản đồ thật ----------
const KIEU_BAN_DO = "https://tiles.openfreemap.org/styles/liberty";
const DIA_HINH = "https://s3.amazonaws.com/elevation-tiles-prod/terrarium/{z}/{x}/{y}.png";

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

// Khoảng cách gần đúng giữa hai điểm [kinh độ, vĩ độ], tính bằng km
function tinhKm(a, b) {
    const R = Math.PI / 180;
    const x = (b[0] - a[0]) * R * Math.cos((a[1] + b[1]) / 2 * R), y = (b[1] - a[1]) * R;
    return Math.hypot(x, y) * 6371;
}

function tinhHuong(a, b) {
    const R = Math.PI / 180;
    return Math.atan2((b[0] - a[0]) * Math.cos((a[1] + b[1]) / 2 * R), b[1] - a[1]) / R;
}

// Mỗi ngày là một đoạn đường: có sẵn trong CSDL, hoặc nối thẳng nếu chưa lấy được
function taoDoan(duLieu) {
    let truoc = duLieu.XuatPhat;
    return duLieu.DsNgay.map((n) => {
        const den = [n.KinhDo, n.ViDo];
        let diem = null;
        try {
            diem = n.DuongDi ? JSON.parse(n.DuongDi) : null;
        } catch (loi) {
            diem = null;
        }
        const thang = !Array.isArray(diem) || diem.length < 2;
        if (thang) diem = [truoc, den];
        const moc = [0];
        for (let i = 1; i < diem.length; i++) moc.push(moc[i - 1] + tinhKm(diem[i - 1], diem[i]));
        truoc = den;
        return { diem, thang, den, moc, km: moc[moc.length - 1] };
    });
}

// Vị trí sau khi đi được km kilômét trên đoạn, kèm chỉ số điểm đã qua để vẽ quãng đã đi
function viTriTrenDoan(doan, km) {
    const { diem, moc } = doan;
    if (km <= 0) return { p: diem[0], i: 0 };
    if (km >= doan.km) return { p: diem[diem.length - 1], i: diem.length - 1 };
    let i = 1;
    while (moc[i] < km) i++;
    const t = (km - moc[i - 1]) / ((moc[i] - moc[i - 1]) || 1);
    return { p: [diem[i - 1][0] + (diem[i][0] - diem[i - 1][0]) * t, diem[i - 1][1] + (diem[i][1] - diem[i - 1][1]) * t], i: i - 1 };
}

async function taoBanDo(duLieu) {
    const dsNgay = duLieu.DsNgay;
    if (!document.createElement("canvas").getContext("webgl2")) throw new Error("Không có WebGL2");
    await taiThuVienBanDo();

    const phanHoi = await fetch(KIEU_BAN_DO, { signal: AbortSignal.timeout(8000) });
    if (!phanHoi.ok) throw new Error("Dịch vụ bản đồ báo lỗi " + phanHoi.status);
    const kieu = await phanHoi.json();
    // Bỏ mọi chữ và đường ranh giới của bản đồ gốc: tên điểm do TravelVN tự ghi
    kieu.layers = kieu.layers.filter((l) => l.type !== "symbol" && !/boundary/.test(l.id));

    const dsDoan = taoDoan(duLieu);
    // Khung xem tính theo cả đường đi, vì đường có thể lượn ra ngoài các điểm dừng
    const tatCaDiem = dsDoan.flatMap((d) => d.diem);
    const dsKinh = tatCaDiem.map((d) => d[0]), dsVi = tatCaDiem.map((d) => d[1]);
    const tay = Math.min(...dsKinh), dong = Math.max(...dsKinh), nam = Math.min(...dsVi), bac = Math.max(...dsVi);
    const dem = Math.max(0.25, (dong - tay) * 0.25, (bac - nam) * 0.25);
    const khung = goc.querySelector(".ct-ht-khung");
    const canvasCu = khung.querySelector("canvas");
    const hop = document.createElement("div");
    hop.className = "ct-ht-ban-do";
    canvasCu.hidden = true;
    khung.appendChild(hop);

    const map = new maplibregl.Map({
        container: hop,
        style: kieu,
        // Nới khung một chút: tour một điểm có khung rộng bằng 0 sẽ làm bản đồ phóng to tối đa
        bounds: [[tay - 0.05, nam - 0.05], [dong + 0.05, bac + 0.05]],
        fitBoundsOptions: { padding: 60 },
        // Khóa vùng xem quanh tour; không cho tự xoay, nghiêng để không nhìn ra được biển xa
        maxBounds: [[tay - dem, nam - dem], [dong + dem, bac + dem]],
        maxPitch: 70,
        dragRotate: false,
        pitchWithRotate: false,
        touchPitch: false,
        keyboard: false,
        scrollZoom: false,
        // Điện thoại: một ngón để cuộn trang, không bị kẹt trong bản đồ; hai ngón để phóng to
        dragPan: window.matchMedia("(pointer: fine)").matches,
        attributionControl: { compact: true },
        locale: {
            "NavigationControl.ZoomIn": "Phóng to",
            "NavigationControl.ZoomOut": "Thu nhỏ",
            "AttributionControl.ToggleAttribution": "Nguồn dữ liệu bản đồ"
        }
    });
    map.touchZoomRotate.disableRotation();
    map.addControl(new maplibregl.NavigationControl({ showCompass: false }), "top-right");

    try {
        await new Promise((xong, loi) => {
            // Tab đang ẩn thì trình duyệt tạm dừng vẽ, không tính là tải chậm; quay lại thì đếm từ đầu
            let hen = null;
            const datHen = () => {
                clearTimeout(hen);
                hen = setTimeout(() => {
                    if (document.hidden) return;
                    document.removeEventListener("visibilitychange", datHen);
                    loi(new Error("Bản đồ tải quá lâu"));
                }, 15000);
            };
            document.addEventListener("visibilitychange", datHen);
            datHen();
            map.once("load", () => {
                clearTimeout(hen);
                document.removeEventListener("visibilitychange", datHen);
                xong();
            });
        });
    } catch (loi) {
        map.remove();
        hop.remove();
        canvasCu.hidden = false;
        throw loi;
    }

    // Không thu nhỏ quá khung tour, nhưng vẫn phải nhỏ hơn mức zoom lúc bay tới từng điểm
    map.setMinZoom(Math.min(map.getZoom() - 0.3, 10.5));
    map.addSource("diaHinh", { type: "raster-dem", tiles: [DIA_HINH], encoding: "terrarium", tileSize: 256, maxzoom: 13, attribution: "Địa hình: Mapzen, AWS Open Data" });
    map.setTerrain({ source: "diaHinh", exaggeration: 1.25 });
    // Đặt bóng đồi dưới lớp nước để không hiện gò nổi của đáy biển
    map.addLayer({
        id: "bongDoi",
        type: "hillshade",
        source: "diaHinh",
        paint: { "hillshade-exaggeration": 0.3, "hillshade-shadow-color": "#5F6F5A", "hillshade-highlight-color": "#FFFFFF" }
    }, map.getLayer("water") ? "water" : undefined);

    const coDuongThat = dsDoan.some((d) => !d.thang), coDuongThang = dsDoan.some((d) => d.thang);
    map.addSource("hanhTrinh", {
        type: "geojson",
        attribution: coDuongThat ? "Đường đi: OSRM, dữ liệu OpenStreetMap" : undefined,
        data: {
            type: "FeatureCollection",
            features: dsDoan.map((d) => ({ type: "Feature", properties: { thang: d.thang }, geometry: { type: "LineString", coordinates: d.diem } }))
        }
    });
    const nen = { "line-cap": "round", "line-join": "round" };
    map.addLayer({ id: "hanhTrinhVien", type: "line", source: "hanhTrinh", layout: nen, paint: { "line-color": "#FFFFFF", "line-width": 7 } });
    map.addLayer({ id: "hanhTrinhDuong", type: "line", source: "hanhTrinh", filter: ["==", ["get", "thang"], false], layout: nen, paint: { "line-color": "#7C8A91", "line-width": 4 } });
    map.addLayer({ id: "hanhTrinhNoiTam", type: "line", source: "hanhTrinh", filter: ["==", ["get", "thang"], true], layout: { "line-cap": "round" }, paint: { "line-color": "#7C8A91", "line-width": 3, "line-dasharray": [2, 1.5] } });
    // Quãng xe đã đi, vẽ lại liên tục khi xe chạy
    const khongCoGi = { type: "Feature", geometry: { type: "MultiLineString", coordinates: [] } };
    map.addSource("daDi", { type: "geojson", data: khongCoGi });
    map.addLayer({ id: "daDiDuong", type: "line", source: "daDi", layout: nen, paint: { "line-color": "#E4572E", "line-width": 5 } });

    function themMoc(chu, toaDo) {
        const el = document.createElement("div");
        el.className = "ct-ht-moc";
        el.textContent = chu;
        new maplibregl.Marker({ element: el, anchor: "bottom", opacityWhenCovered: "1" }).setLngLat(toaDo).addTo(map);
        return el;
    }
    themMoc("Xuất phát", duLieu.XuatPhat);
    const dsMoc = dsNgay.map((n, i) => themMoc("Ngày " + n.Ngay, dsDoan[i].den));

    // Xe nhìn từ trên xuống, mũi xe hướng lên; xoay và nằm theo mặt bản đồ
    const elXe = document.createElement("div");
    elXe.className = "ct-ht-xe";
    elXe.innerHTML = '<svg viewBox="0 0 24 44" width="20" height="36" aria-hidden="true"><rect x="2" y="2" width="20" height="40" rx="6" fill="#E4572E" stroke="#FFFFFF" stroke-width="2"/><rect x="5" y="7" width="14" height="7" rx="2" fill="#BDE3EC"/><rect x="5" y="32" width="14" height="5" rx="2" fill="#BDE3EC"/></svg>';
    const xe = new maplibregl.Marker({ element: elXe, rotationAlignment: "map", pitchAlignment: "map", opacityWhenCovered: "1" })
        .setLngLat(duLieu.XuatPhat)
        .addTo(map);

    // Xe chỉ bắt đầu chạy khi người xem thật sự nhìn thấy khung bản đồ
    let baoDaThay;
    const daThay = new Promise((xong) => { baoDaThay = xong; });
    const moiThay = new IntersectionObserver((muc) => {
        if (muc.some((m) => m.isIntersecting)) {
            moiThay.disconnect();
            baoDaThay();
        }
    }, { threshold: 0.4 });
    moiThay.observe(khung);

    const giamChuyenDong = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    let luotBay = 0;

    function veDaDi(i, phanDangDi) {
        const dsDuong = dsDoan.slice(0, i).map((d) => d.diem);
        if (phanDangDi && phanDangDi.length > 1) dsDuong.push(phanDangDi);
        map.getSource("daDi").setData({ type: "Feature", geometry: { type: "MultiLineString", coordinates: dsDuong } });
    }

    async function chayDoan(i, luot, xong) {
        await daThay;
        if (luot !== luotBay) return;

        const doan = dsDoan[i];
        veDaDi(i, null);
        xe.setLngLat(doan.diem[0]);

        // Quãng càng dài camera càng lên cao, không thì xe lao vun vút qua màn hình
        const zoom = Math.max(map.getMinZoom() + 0.1, doan.km < 60 ? 11.4 : 11.4 - Math.log2(doan.km / 60));

        // Ngày tham quan tại chỗ: xe đứng yên, camera chỉ lượn tới
        if (doan.km < 0.05) {
            map.once("moveend", () => { if (luot === luotBay) xong(); });
            map.flyTo({ center: doan.den, zoom, pitch: 55, duration: 2500 });
            return;
        }

        if (giamChuyenDong) {
            veDaDi(i, doan.diem);
            xe.setLngLat(doan.den);
            map.jumpTo({ center: doan.den, zoom, pitch: 55 });
            xong();
            return;
        }

        const nhin = (km) => viTriTrenDoan(doan, km + Math.max(0.4, doan.km * 0.03)).p;
        let huong = tinhHuong(doan.diem[0], nhin(0));
        xe.setRotation(huong);
        map.flyTo({ center: doan.diem[0], zoom, pitch: 55, bearing: huong, duration: 1500 });
        await new Promise((tiep) => setTimeout(tiep, 1550));
        if (luot !== luotBay) return;

        const thoiGian = Math.min(14000, Math.max(4000, 4000 + doan.km * 70));
        const batDau = performance.now();
        const buoc = (t) => {
            if (luot !== luotBay) return;
            const k = Math.min(1, (t - batDau) / thoiGian);
            const em = k < 0.5 ? 2 * k * k : 1 - Math.pow(-2 * k + 2, 2) / 2;
            const km = em * doan.km;
            const { p, i: daQua } = viTriTrenDoan(doan, km);

            // Xoay camera từ từ theo hướng phía trước, đi vòng theo góc ngắn nhất
            const huongDich = k >= 1 ? huong : tinhHuong(p, nhin(km));
            huong += (((huongDich - huong + 540) % 360) - 180) * 0.06;

            xe.setLngLat(p);
            xe.setRotation(huong);
            map.jumpTo({ center: p, bearing: huong });
            veDaDi(i, doan.diem.slice(0, daQua + 1).concat([p]));

            if (k < 1) {
                requestAnimationFrame(buoc);
            } else {
                veDaDi(i, doan.diem);
                xong();
            }
        };
        requestAnimationFrame(buoc);
    }

    // Dòng nguồn dữ liệu thu gọn thành nút "i" để không che bản đồ, bấm vào vẫn xem được
    const nguon = hop.querySelector(".maplibregl-ctrl-attrib");
    if (nguon) nguon.classList.remove("maplibregl-compact-show");

    return {
        ghiChu: coDuongThang
            ? "Đường tham khảo, lộ trình thực tế có thể khác. Nét đứt là đoạn tạm nối thẳng, chưa có đường xe chạy."
            : "Đường tham khảo, lộ trình thực tế có thể khác.",
        bayToi(i, xong) {
            dsMoc.forEach((m, j) => m.classList.toggle("chon", j === i));
            chayDoan(i, ++luotBay, xong);
        }
    };
}

// ---------- Phong cảnh tự vẽ ----------
function taoCachDieu(THREE, dsNgay) {
    const khung = goc.querySelector(".ct-ht-khung");
    const canvas = khung.querySelector("canvas");
    const giamChuyenDong = window.matchMedia("(prefers-reduced-motion: reduce)").matches;

    // Mỗi tour một dáng đường riêng nhưng mở lại vẫn giống nhau
    const hat = (Number(goc.dataset.tour) || 1) * 1.37;
    let trangThaiNgauNhien = Math.floor(hat * 1000) + 7;
    const ngauNhien = () => {
        trangThaiNgauNhien = (trangThaiNgauNhien * 16807) % 2147483647;
        return trangThaiNgauNhien / 2147483647;
    };

    const SO_NGAY = dsNgay.length;
    const DAI = Math.max(70, (SO_NGAY - 1) * 26 + 40);
    const RONG = 90;
    const Y_DUONG = 1.5;

    // Đường đi từ z = 0 về phía z âm; x là hàm của z để biết điểm nào gần đường
    const xDuong = (z) => Math.sin(z * 0.07 + hat) * 7 + Math.sin(z * 0.023 + hat * 2) * 5;
    const tronDan = (a, b, x) => {
        const t = Math.min(1, Math.max(0, (x - a) / (b - a)));
        return t * t * (3 - 2 * t);
    };

    // Độ cao: đồi bằng tổng các sóng, hạ thấp một bên thành biển, san phẳng dọc đường
    function doCao(x, z) {
        const doi = 2.6 * Math.sin(x * 0.13 + hat) * Math.cos(z * 0.11 + hat)
            + 1.3 * Math.sin(x * 0.31 + z * 0.17 + hat * 2)
            + 0.6 * Math.sin(x * 0.7 - z * 0.5)
            + 2.4;
        const bien = tronDan(-14, -34, x) * 9;
        const nui = tronDan(14, 42, x) * 5.5;
        const cach = Math.abs(x - xDuong(z));
        return Y_DUONG + (doi + nui - bien - Y_DUONG) * tronDan(2.2, 8, cach);
    }

    const renderer = new THREE.WebGLRenderer({ canvas, antialias: true });
    renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, 2));
    const canh = new THREE.Scene();
    const mauTroi = 0xCFE8EE;
    canh.background = new THREE.Color(mauTroi);
    canh.fog = new THREE.Fog(mauTroi, 32, 92);
    const camera = new THREE.PerspectiveCamera(50, 1, 0.1, 300);

    canh.add(new THREE.HemisphereLight(0xFFFFFF, 0x8FA79A, 1.6));
    const den = new THREE.DirectionalLight(0xFFF4DD, 2);
    den.position.set(30, 40, 20);
    canh.add(den);

    // Mặt đất: mỗi mặt tam giác một màu để ra kiểu đồ họa khối
    const giuaZ = -DAI / 2;
    const soO = Math.min(240, Math.round(DAI + 40));
    const hinhDat = new THREE.PlaneGeometry(RONG, DAI + 90, 72, soO).toNonIndexed();
    hinhDat.rotateX(-Math.PI / 2);
    hinhDat.translate(0, 0, giuaZ);
    const viTri = hinhDat.attributes.position;
    for (let i = 0; i < viTri.count; i++) {
        viTri.setY(i, doCao(viTri.getX(i), viTri.getZ(i)));
    }
    const mauDat = new Float32Array(viTri.count * 3);
    const mau = new THREE.Color();
    for (let i = 0; i < viTri.count; i += 3) {
        const cao = (viTri.getY(i) + viTri.getY(i + 1) + viTri.getY(i + 2)) / 3;
        mau.set(cao < 0.45 ? 0xE9DBAC : cao < 3 ? 0x86BC70 : cao < 5.6 ? 0x559A5E : 0x8E9C90);
        for (let j = 0; j < 3; j++) mau.toArray(mauDat, (i + j) * 3);
    }
    hinhDat.setAttribute("color", new THREE.BufferAttribute(mauDat, 3));
    hinhDat.computeVertexNormals();
    canh.add(new THREE.Mesh(hinhDat, new THREE.MeshStandardMaterial({ vertexColors: true, flatShading: true, roughness: 0.95 })));

    const bien = new THREE.Mesh(
        new THREE.PlaneGeometry(RONG + 200, DAI + 300),
        new THREE.MeshStandardMaterial({ color: 0x3BA7C4, roughness: 0.35, metalness: 0.1 })
    );
    bien.rotation.x = -Math.PI / 2;
    bien.position.set(0, 0.1, giuaZ);
    canh.add(bien);

    // Con đường: dải phẳng lấy mẫu dọc theo xDuong; thêm một dải màu cho quãng đã đi
    const DIEM = Math.round(DAI * 3);
    const zTaiU = (u) => -u * DAI;
    const diemTaiU = (u, dich = new THREE.Vector3()) => {
        const z = zTaiU(u);
        return dich.set(xDuong(z), Y_DUONG, z);
    };
    function taoDai(rong, y, vatLieu) {
        const dinh = [], chiSo = [];
        for (let i = 0; i <= DIEM; i++) {
            const z = zTaiU(i / DIEM), x = xDuong(z);
            dinh.push(x - rong / 2, y, z, x + rong / 2, y, z);
            if (i < DIEM) {
                const a = i * 2;
                chiSo.push(a, a + 1, a + 2, a + 1, a + 3, a + 2);
            }
        }
        const hinh = new THREE.BufferGeometry();
        hinh.setAttribute("position", new THREE.Float32BufferAttribute(dinh, 3));
        hinh.setIndex(chiSo);
        hinh.computeVertexNormals();
        const m = new THREE.Mesh(hinh, vatLieu);
        canh.add(m);
        return m;
    }
    taoDai(2.2, Y_DUONG + 0.06, new THREE.MeshStandardMaterial({ color: 0x46545B, roughness: 0.9, side: THREE.DoubleSide }));
    const daDi = taoDai(0.5, Y_DUONG + 0.1, new THREE.MeshBasicMaterial({ color: 0xFFD166, side: THREE.DoubleSide }));

    // Cột mốc mỗi ngày, cách đều trên đường
    const uNgay = dsNgay.map((_, i) => 0.06 + 0.88 * (SO_NGAY === 1 ? 0 : i / (SO_NGAY - 1)));
    const vatLieuCot = new THREE.MeshStandardMaterial({ color: 0x5B6B73 });
    const dsMoc = dsNgay.map((n, i) => {
        const p = diemTaiU(uNgay[i]);
        const nhom = new THREE.Group();
        nhom.position.set(p.x + 1.7, Y_DUONG, p.z);
        const cot = new THREE.Mesh(new THREE.CylinderGeometry(0.07, 0.07, 2.2, 8), vatLieuCot);
        cot.position.y = 1.1;
        const vatLieuDau = new THREE.MeshStandardMaterial({ color: 0x0A6E8A, emissive: 0x0A6E8A, emissiveIntensity: 0.2 });
        const dau = new THREE.Mesh(new THREE.SphereGeometry(0.34, 16, 12), vatLieuDau);
        dau.position.y = 2.35;
        const nhan = taoNhan(THREE, "Ngày " + n.Ngay);
        nhan.position.y = 3.3;
        nhom.add(cot, dau, nhan);
        canh.add(nhom);
        return { vatLieuDau, dau };
    });

    // Cây: rải ngẫu nhiên, tránh đường, biển và núi đá
    const soCay = Math.min(900, Math.round(DAI * 7));
    const cayTan = new THREE.InstancedMesh(new THREE.ConeGeometry(0.75, 2, 6), new THREE.MeshStandardMaterial({ color: 0x3E8552, flatShading: true }), soCay);
    const cayThan = new THREE.InstancedMesh(new THREE.CylinderGeometry(0.12, 0.14, 0.6, 6), new THREE.MeshStandardMaterial({ color: 0x7A5A3A }), soCay);
    const tam = new THREE.Object3D();
    let soDaDat = 0;
    for (let lan = 0; lan < soCay * 6 && soDaDat < soCay; lan++) {
        const x = (ngauNhien() - 0.5) * RONG, z = 40 - ngauNhien() * (DAI + 85);
        const cao = doCao(x, z);
        if (cao < 1 || cao > 5.5 || Math.abs(x - xDuong(z)) < 3.4) continue;
        const to = 0.7 + ngauNhien() * 0.8;
        tam.scale.setScalar(to);
        tam.position.set(x, cao + 0.3 * to, z);
        tam.updateMatrix();
        cayThan.setMatrixAt(soDaDat, tam.matrix);
        tam.position.y = cao + 1.45 * to;
        tam.updateMatrix();
        cayTan.setMatrixAt(soDaDat, tam.matrix);
        soDaDat++;
    }
    cayTan.count = cayThan.count = soDaDat;
    canh.add(cayTan, cayThan);

    // Xe nhỏ chạy trên đường
    const xe = new THREE.Group();
    const thanXe = new THREE.Mesh(new THREE.BoxGeometry(0.85, 0.72, 2), new THREE.MeshStandardMaterial({ color: 0xE4572E, roughness: 0.5 }));
    thanXe.position.y = 0.6;
    const kinhXe = new THREE.Mesh(new THREE.BoxGeometry(0.87, 0.3, 1.5), new THREE.MeshStandardMaterial({ color: 0xBDE3EC, roughness: 0.2 }));
    kinhXe.position.set(0, 0.76, -0.1);
    xe.add(thanXe, kinhXe);
    canh.add(xe);

    // ---------- Camera bay theo u (0..1 trên đường) ----------
    let u = giamChuyenDong ? uNgay[0] : 0, uDau = u, uDich = uNgay[0], batDauBay = 0, thoiGianBay = 1;
    let dangHien = true, ngayChon = 0, khiToi = null;
    const p = new THREE.Vector3(), truoc = new THREE.Vector3(), nhin = new THREE.Vector3();

    function datCanh(t) {
        diemTaiU(u, p);
        diemTaiU(Math.min(1.05, u + 0.012), truoc);
        xe.position.set(p.x, Y_DUONG + 0.05, p.z);
        xe.lookAt(truoc.x, Y_DUONG + 0.05, truoc.z);
        xe.rotateY(Math.PI);

        // Đứng phía sau xe, lệch sang bên và ở trên cao; lắc nhẹ cho có sức sống
        const lac = giamChuyenDong ? 0 : Math.sin(t * 0.00035) * 1.2;
        camera.position.set(p.x + 5 + lac, 9.5, p.z + 13);
        diemTaiU(Math.min(1.1, u + 7 / DAI), nhin);
        camera.lookAt(nhin.x, 1.5, nhin.z);

        // Quãng đã đi: mỗi đoạn lấy mẫu có 6 chỉ số tam giác
        daDi.geometry.setDrawRange(0, Math.round(Math.max(0, Math.min(1, u)) * DIEM) * 6);
        dsMoc.forEach((m, i) => {
            const chon = i === ngayChon;
            m.vatLieuDau.color.set(chon ? 0xE4572E : u >= uNgay[i] - 0.001 ? 0xFFB703 : 0x0A6E8A);
            m.vatLieuDau.emissive.copy(m.vatLieuDau.color);
            m.vatLieuDau.emissiveIntensity = chon ? 0.45 : 0.2;
            m.dau.scale.setScalar(chon ? 1.35 : 1);
        });
    }

    function ve(t) {
        if (batDauBay) {
            const k = Math.min(1, (t - batDauBay) / thoiGianBay);
            const em = k < 0.5 ? 2 * k * k : 1 - Math.pow(-2 * k + 2, 2) / 2;
            u = uDau + (uDich - uDau) * em;
            if (k >= 1) {
                batDauBay = 0;
                const xong = khiToi;
                khiToi = null;
                if (xong) xong();
            }
        }
        datCanh(t);
        renderer.render(canh, camera);
    }

    function bayToi(i, xong) {
        ngayChon = i;
        uDau = u;
        uDich = uNgay[i];
        if (giamChuyenDong) {
            u = uDich;
            batDauBay = 0;
            ve(performance.now());
            xong();
        } else {
            khiToi = xong;
            batDauBay = performance.now();
            thoiGianBay = 900 + Math.abs(uDich - uDau) * DAI * 55;
        }
    }

    function doKichThuoc() {
        const r = khung.clientWidth, c = khung.clientHeight;
        if (!r || !c) return;
        renderer.setSize(r, c, false);
        camera.aspect = r / c;
        camera.updateProjectionMatrix();
        ve(performance.now());
    }
    new ResizeObserver(doKichThuoc).observe(khung);
    doKichThuoc();

    if (!giamChuyenDong) {
        new IntersectionObserver((muc) => { dangHien = muc.some((m) => m.isIntersecting); }).observe(khung);
        const lap = (t) => {
            if (dangHien) ve(t);
            requestAnimationFrame(lap);
        };
        requestAnimationFrame(lap);
    }

    canvas.addEventListener("webglcontextlost", (e) => {
        e.preventDefault();
        goc.hidden = true;
    });

    return { bayToi, ghiChu: "Minh họa hành trình, không phải bản đồ." };
}

function taoNhan(THREE, chu) {
    const c = document.createElement("canvas");
    c.width = 192;
    c.height = 72;
    const ctx = c.getContext("2d");
    ctx.fillStyle = "#17262E";
    ctx.beginPath();
    ctx.roundRect(4, 4, 184, 64, 20);
    ctx.fill();
    ctx.fillStyle = "#FFFFFF";
    ctx.font = '700 34px "Be Vietnam Pro", system-ui, sans-serif';
    ctx.textAlign = "center";
    ctx.textBaseline = "middle";
    ctx.fillText(chu, 96, 38);
    const texture = new THREE.CanvasTexture(c);
    texture.colorSpace = THREE.SRGBColorSpace;
    const sprite = new THREE.Sprite(new THREE.SpriteMaterial({ map: texture, depthTest: false, transparent: true, fog: false }));
    sprite.scale.set(2.2, 0.82, 1);
    sprite.renderOrder = 10;
    return sprite;
}
