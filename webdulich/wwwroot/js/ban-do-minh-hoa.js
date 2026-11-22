// Bản đồ hành trình minh họa ở trang chi tiết tour (Views/DiaDiem/ChiTiet.cshtml).
// Không phải bản đồ thật: các trạm (xuất phát, điểm tham quan, điểm cuối mỗi ngày) xếp đều theo thứ tự trên một đường uốn lượn,
// nối bằng nét đứt, mỗi ngày một màu; nền là đồi, hồ, mây, cây vẽ đơn giản. Bấm trạm thì thẻ bên cạnh hiện thông tin trạm.
// Dữ liệu lấy từ lịch trình và điểm tham quan (Mo23, ảnh Mo25), nên tour nào có lịch trình là tự có bản đồ, kể cả khi chưa có tọa độ.
// Vẽ bằng SVG, không cần WebGL hay dịch vụ bản đồ ngoài. Không có JavaScript thì trang vẫn có danh sách trạm.
(function () {
    const khung = document.getElementById("bdKhung");
    const nguon = document.getElementById("bdDuLieu");
    if (!khung || !nguon) return;

    const duLieu = JSON.parse(nguon.textContent);
    const giamChuyenDong = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    const SVG = "http://www.w3.org/2000/svg";
    // Mỗi ngày một màu, đủ tương phản trên nền sáng và tối
    const MAU_NGAY = ["#0A6E8A", "#E4572E", "#1E8A5A", "#B7791F", "#7B4FA0", "#C2410C", "#2563EB"];
    const mauNgay = (i) => MAU_NGAY[(i < 0 ? 0 : i) % MAU_NGAY.length];

    // ---------- Danh sách trạm theo thứ tự đi ----------
    const dsTram = [];
    if (duLieu.CoXuatPhat) {
        dsTram.push({ loai: "xuat-phat", ngay: -1, nhan: "Xuất phát", ten: "Xuất phát", moTa: "Nơi đoàn bắt đầu chuyến đi." });
    }
    duLieu.DsNgay.forEach((n, i) => {
        n.DsDiem.forEach((d, k) => dsTram.push({
            loai: "diem", ngay: i, so: k + 1, nhan: d.Ten, ten: d.Ten,
            moTa: "Ngày " + n.Ngay + ": " + n.TieuDe, anh: d.Anh, google: d.LinkGoogle, link360: d.Link360
        }));
        dsTram.push({
            loai: "ngay", ngay: i, nhan: "Hết ngày " + n.Ngay, ten: n.TieuDe,
            moTa: n.BuaAn ? "Bữa ăn: " + n.BuaAn : "Điểm dừng cuối ngày " + n.Ngay + ".", google: n.LinkGoogle
        });
    });
    if (dsTram.length < 2) return;

    const the = document.getElementById("bdThe");
    const dsTab = document.querySelectorAll("#bdTab [data-ngay]");
    let ngayLoc = -1, chon = 0, cheDo = null;

    // ---------- Vị trí trạm ----------
    // Màn rộng: các hàng đi zic zắc trái sang phải rồi phải sang trái. Màn hẹp: một cột uốn lượn từ trên xuống.
    function tinhViTri(rong) {
        const n = dsTram.length;
        if (rong >= 640) {
            // Tối đa 6 trạm một hàng, chia đều cho các hàng để hàng cuối không bị thưa
            const soHang = Math.ceil(n / 6), moiHang = Math.ceil(n / soHang);
            const W = 1000, H = Math.max(440, 120 + soHang * 190);
            const trai = 90, phai = W - 90, tren = 120;
            const buocHang = soHang > 1 ? (H - 210) / (soHang - 1) : 0;
            const ds = dsTram.map((_, j) => {
                const hang = Math.floor(j / moiHang), cot = j % moiHang;
                const soCot = Math.min(moiHang, n - hang * moiHang);
                let t = soCot === 1 ? 0.5 : cot / (soCot - 1);
                if (hang % 2 === 1) t = 1 - t;
                const y0 = soHang === 1 ? H / 2 + 10 : tren + hang * buocHang;
                return { x: trai + t * (phai - trai), y: y0 + Math.sin(j * 1.7 + hang) * 34 };
            });
            return { W, H, ds, doc: false };
        }
        const W = 600, buoc = 130, H = 150 + (n - 1) * buoc + 60;
        const ds = dsTram.map((_, j) => ({ x: W / 2 + Math.sin(j * 1.15) * 150, y: 90 + j * buoc }));
        return { W, H, ds, doc: true };
    }

    // Đường cong Catmull-Rom qua các trạm, đổi thành Bézier bậc ba để vẽ từng đoạn riêng màu
    function doan(p0, p1, p2, p3) {
        const c1 = { x: p1.x + (p2.x - p0.x) / 6, y: p1.y + (p2.y - p0.y) / 6 };
        const c2 = { x: p2.x - (p3.x - p1.x) / 6, y: p2.y - (p3.y - p1.y) / 6 };
        return `C ${c1.x.toFixed(1)} ${c1.y.toFixed(1)}, ${c2.x.toFixed(1)} ${c2.y.toFixed(1)}, ${p2.x.toFixed(1)} ${p2.y.toFixed(1)}`;
    }

    // Số ngẫu nhiên cố định theo tour để nền không đổi mỗi lần mở trang
    function taoNgauNhien(hat) {
        let s = hat % 2147483647 || 1;
        return () => (s = (s * 16807) % 2147483647) / 2147483647;
    }

    function phanTu(ten, thuocTinh, cha) {
        const el = document.createElementNS(SVG, ten);
        for (const k in thuocTinh) el.setAttribute(k, thuocTinh[k]);
        if (cha) cha.appendChild(el);
        return el;
    }

    function ve() {
        const rong = khung.clientWidth;
        const moi = rong >= 640 ? "ngang" : "doc";
        cheDo = moi;
        const { W, H, ds, doc } = tinhViTri(rong);
        // Màn hẹp: tranh bị thu nhỏ nhiều nên phóng to trạm và chữ, nhãn đặt sang bên cạnh trạm (phía gần giữa tranh)
        const tiLe = doc ? 1.6 : 1;
        khung.replaceChildren();

        const svg = phanTu("svg", { viewBox: `0 0 ${W} ${H}`, class: "bd-svg", role: "group", "aria-label": "Sơ đồ các trạm của hành trình" });
        const ngauNhien = taoNgauNhien(dsTram.length * 7919 + duLieu.DsNgay.length * 104729);

        // Nền: trời, đồi, hồ, mây, cây. Tránh vẽ đè lên vùng quanh các trạm.
        phanTu("rect", { x: 0, y: 0, width: W, height: H, class: "bd-nen" }, svg);
        // Dải trời phía trên, mép dưới lượn sóng nhẹ
        phanTu("path", { d: `M 0 0 H ${W} V ${H * 0.3} Q ${W * 0.75} ${H * 0.36} ${W / 2} ${H * 0.3} T 0 ${H * 0.3} Z`, class: "bd-troi" }, svg);
        // Các điểm cần tránh: trạm và vài điểm dọc đoạn nối giữa hai trạm (đường đi)
        const vungTranh = ds.slice();
        for (let j = 0; j < ds.length - 1; j++) {
            for (let t = 0.25; t < 1; t += 0.25) vungTranh.push({ x: ds[j].x + (ds[j + 1].x - ds[j].x) * t, y: ds[j].y + (ds[j + 1].y - ds[j].y) * t });
        }
        const gan = (x, y, r) => vungTranh.some((p) => Math.hypot(p.x - x, p.y - y) < r);
        for (let i = 0; i < 4; i++) {
            const x = ngauNhien() * W, rongDoi = 220 + ngauNhien() * 260, cao = 70 + ngauNhien() * 70, day = H - ngauNhien() * 30;
            phanTu("path", { d: `M ${x - rongDoi / 2} ${day} Q ${x} ${day - cao * 2} ${x + rongDoi / 2} ${day} Z`, class: "bd-doi" + (i % 2 ? " xa" : "") }, svg);
        }
        for (let i = 0, lan = 0; i < 2 && lan < 40; lan++) {
            const x = 80 + ngauNhien() * (W - 160), y = 80 + ngauNhien() * (H - 160);
            if (gan(x, y, 110)) continue;
            phanTu("ellipse", { cx: x, cy: y, rx: 60 + ngauNhien() * 40, ry: 22 + ngauNhien() * 10, class: "bd-nuoc" }, svg);
            i++;
        }
        for (let i = 0, lan = 0; i < 4 && lan < 60; lan++) {
            const x = 60 + ngauNhien() * (W - 120), y = 30 + ngauNhien() * (H * 0.35);
            if (gan(x, y, 70)) continue;
            const may = phanTu("g", { class: "bd-may" }, svg);
            phanTu("ellipse", { cx: x, cy: y, rx: 34, ry: 12 }, may);
            phanTu("ellipse", { cx: x + 18, cy: y - 8, rx: 20, ry: 12 }, may);
            i++;
        }
        for (let i = 0, lan = 0; i < 18 && lan < 200; lan++) {
            const x = 30 + ngauNhien() * (W - 60), y = 60 + ngauNhien() * (H - 90);
            if (gan(x, y, 58)) continue;
            const c = 14 + ngauNhien() * 10;
            phanTu("path", { d: `M ${x} ${y - c} L ${x + c * 0.55} ${y} L ${x - c * 0.55} ${y} Z`, class: "bd-cay" }, svg);
            i++;
        }

        // Đường đi: viền trắng rộng, trên là nét đứt theo màu ngày của trạm đến
        const lopDuong = phanTu("g", { class: "bd-duong" }, svg);
        let dToanBo = `M ${ds[0].x.toFixed(1)} ${ds[0].y.toFixed(1)}`;
        const dsDoan = [];
        for (let j = 0; j < ds.length - 1; j++) {
            const p0 = ds[Math.max(0, j - 1)], p1 = ds[j], p2 = ds[j + 1], p3 = ds[Math.min(ds.length - 1, j + 2)];
            const c = doan(p0, p1, p2, p3);
            dToanBo += " " + c;
            const d = `M ${p1.x.toFixed(1)} ${p1.y.toFixed(1)} ${c}`;
            phanTu("path", { d, class: "bd-duong-vien" }, lopDuong);
            dsDoan.push({ el: phanTu("path", { d, class: "bd-duong-net", stroke: mauNgay(dsTram[j + 1].ngay) }, lopDuong), ngay: dsTram[j + 1].ngay });
        }

        // Chấm nhỏ chạy dọc đường: khoảnh khắc chuyển động duy nhất, tắt khi người xem chọn giảm chuyển động
        if (!giamChuyenDong) {
            const cham = phanTu("circle", { r: 7, class: "bd-cham" }, svg);
            const chay = phanTu("animateMotion", { dur: Math.max(8, ds.length * 1.6) + "s", repeatCount: "indefinite", path: dToanBo }, cham);
            chay.setAttribute("rotate", "auto");
        }

        // Trạm: vòng tròn theo màu ngày, nhãn tên xen kẽ trên dưới
        const lopTram = phanTu("g", { class: "bd-ds-tram" }, svg);
        const dsNhom = dsTram.map((t, j) => {
            const p = ds[j], mau = mauNgay(t.ngay);
            const g = phanTu("g", {
                class: "bd-tram " + t.loai, tabindex: 0, role: "button",
                "aria-label": (t.ngay >= 0 ? "Ngày " + duLieu.DsNgay[t.ngay].Ngay + ", " : "") + t.ten,
                transform: `translate(${p.x.toFixed(1)} ${p.y.toFixed(1)}) scale(${tiLe})`
            }, lopTram);
            phanTu("circle", { r: 26, class: "bd-tram-vung" }, g);
            phanTu("circle", { r: t.loai === "diem" ? 15 : 18, class: "bd-tram-vong", stroke: mau }, g);
            const chu = t.loai === "xuat-phat" ? "XP" : t.loai === "ngay" ? "N" + duLieu.DsNgay[t.ngay].Ngay : String(t.so);
            phanTu("text", { class: "bd-tram-so", fill: mau, "text-anchor": "middle", "dominant-baseline": "central" }, g).textContent = chu;

            // Màn rộng: nhãn xen kẽ trên, dưới trạm. Màn hẹp: nhãn bên trái hoặc phải, phía gần giữa tranh.
            const ben = doc ? (p.x > W / 2 ? -1 : 1) : 0;
            const tren = j % 2 === 0;
            const nhan = phanTu("g", { class: "bd-nhan", transform: doc ? `translate(${ben * 32} 0)` : `translate(0 ${tren ? -44 : 44})` }, g);
            const nen = phanTu("rect", { rx: 12, ry: 12, height: 26, y: -13 }, nhan);
            const tx = phanTu("text", { "text-anchor": ben === 0 ? "middle" : ben > 0 ? "start" : "end", "dominant-baseline": "central" }, nhan);
            const toiDa = doc ? 16 : 22;
            tx.textContent = t.nhan.length > toiDa ? t.nhan.slice(0, toiDa - 1) + "…" : t.nhan;
            g.addEventListener("click", () => chonTram(j, true));
            g.addEventListener("keydown", (e) => {
                if (e.key === "Enter" || e.key === " ") { e.preventDefault(); chonTram(j, true); }
            });
            return { g, nen, tx, ben };
        });

        khung.appendChild(svg);
        // Đo chữ sau khi gắn vào trang để nền nhãn vừa khít
        dsNhom.forEach(({ nen, tx, ben }) => {
            const w = tx.getComputedTextLength() + 22;
            nen.setAttribute("width", w.toFixed(1));
            nen.setAttribute("x", (ben === 0 ? -w / 2 : ben > 0 ? -11 : -w + 11).toFixed(1));
        });

        khung._bd = { dsNhom, dsDoan };
        apLoc();
        chonTram(chon, false);
    }

    // ---------- Lọc theo ngày, chọn trạm ----------
    function apLoc() {
        const bd = khung._bd;
        if (!bd) return;
        bd.dsNhom.forEach(({ g }, j) => {
            const t = dsTram[j];
            g.classList.toggle("mo", ngayLoc >= 0 && t.ngay !== ngayLoc);
        });
        bd.dsDoan.forEach((d) => d.el.classList.toggle("mo", ngayLoc >= 0 && d.ngay !== ngayLoc));
        dsTab.forEach((b) => b.setAttribute("aria-selected", String(Number(b.dataset.ngay) === ngayLoc)));
    }

    function chonTram(j, tuNguoiXem) {
        chon = j;
        const t = dsTram[j];
        const bd = khung._bd;
        if (bd) bd.dsNhom.forEach(({ g }, a) => g.classList.toggle("chon", a === j));
        if (!the) return;

        const anh = the.querySelector('[data-bd="anh"]');
        anh.replaceChildren();
        if (t.anh) {
            const img = document.createElement("img");
            img.src = t.anh;
            img.alt = t.ten;
            anh.appendChild(img);
        } else {
            anh.innerHTML = '<span class="ct-bd-the-trong" aria-hidden="true"><svg class="tv-icon"><use href="#i-' + (t.loai === "diem" ? "pin" : "route") + '"/></svg></span>';
        }
        anh.style.setProperty("--mau-ngay", mauNgay(t.ngay));

        the.querySelector('[data-bd="ngay"]').textContent = t.ngay >= 0
            ? "NGÀY " + String(duLieu.DsNgay[t.ngay].Ngay).padStart(2, "0") + (t.loai === "diem" ? ", ĐIỂM " + t.so : "")
            : "KHỞI HÀNH";
        the.querySelector('[data-bd="ten"]').textContent = t.ten;
        the.querySelector('[data-bd="mo-ta"]').textContent = t.moTa;

        const nut = the.querySelector('[data-bd="nut"]');
        nut.replaceChildren();
        const themNut = (chu, href, chinh, moTabMoi) => {
            const a = document.createElement("a");
            a.href = href;
            a.textContent = chu;
            a.className = "tv-btn " + (chinh ? "tv-btn-chinh" : "tv-btn-vien");
            if (moTabMoi) { a.target = "_blank"; a.rel = "noopener"; }
            nut.appendChild(a);
        };
        if (t.link360) themNut("Xem 360°", t.link360, true, false);
        if (t.google) themNut(t.loai === "ngay" ? "Chỉ đường ngày này" : "Google Maps", t.google, !t.link360, true);
        nut.hidden = !nut.children.length;

        // Hàng ảnh nhỏ: các điểm khác trong cùng ngày, bấm để chuyển nhanh
        const nho = the.querySelector('[data-bd="nho"]');
        nho.replaceChildren();
        dsTram.forEach((u, a) => {
            if (u.loai !== "diem" || u.ngay !== t.ngay || t.ngay < 0) return;
            const b = document.createElement("button");
            b.type = "button";
            b.className = "ct-bd-nho" + (a === j ? " chon" : "");
            b.setAttribute("aria-label", u.ten);
            b.title = u.ten;
            if (u.anh) {
                const img = document.createElement("img");
                img.src = u.anh;
                img.alt = "";
                img.loading = "lazy";
                b.appendChild(img);
            } else {
                b.textContent = u.so;
            }
            b.addEventListener("click", () => chonTram(a, true));
            nho.appendChild(b);
        });
        nho.hidden = !nho.children.length;

        if (tuNguoiXem && ngayLoc >= 0 && t.ngay !== ngayLoc && t.ngay >= 0) {
            ngayLoc = t.ngay;
            apLoc();
        }
    }

    dsTab.forEach((b) => b.addEventListener("click", () => {
        ngayLoc = Number(b.dataset.ngay);
        apLoc();
        const dau = ngayLoc < 0 ? 0 : dsTram.findIndex((t) => t.ngay === ngayLoc);
        chonTram(Math.max(0, dau), false);
    }));

    // Đổi giữa màn rộng và màn hẹp thì vẽ lại
    let hen = null;
    window.addEventListener("resize", () => {
        clearTimeout(hen);
        hen = setTimeout(() => {
            if ((khung.clientWidth >= 640 ? "ngang" : "doc") !== cheDo) ve();
        }, 200);
    });

    ve();
})();
