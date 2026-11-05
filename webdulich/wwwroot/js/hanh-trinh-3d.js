// Hành trình 3D ở trang chi tiết tour: phong cảnh TỰ VẼ, con đường và cột mốc theo số ngày trong lịch trình.
// Đây là hình minh họa, không phải bản đồ: không có tọa độ, không có hình đất nước, vị trí cột mốc không ứng với địa điểm thật.
// Khung này ẩn sẵn; chỉ hiện khi 3D chạy được. Lỗi ở đây không ảnh hưởng danh sách lịch trình bên dưới.
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
    const thu = document.createElement("canvas");
    if (!thu.getContext("webgl2") && !thu.getContext("webgl")) return;
    try {
        const THREE = await import("/lib/three/three.module.min.js");
        khoiTao(THREE);
    } catch (loi) {
        goc.hidden = true;
    }
}

function khoiTao(THREE) {
    const dsNgay = JSON.parse(document.getElementById("hanhTrinhDuLieu").textContent);
    const khung = goc.querySelector(".ct-ht-khung");
    const canvas = khung.querySelector("canvas");
    const nhomNut = goc.querySelector(".ct-ht-ngay");
    const theTieuDe = goc.querySelector(".ct-ht-tieu-de");
    const theBuaAn = goc.querySelector(".ct-ht-bua-an");
    const nutBay = goc.querySelector("[data-ht-bay]");
    const giamChuyenDong = window.matchMedia("(prefers-reduced-motion: reduce)").matches;

    goc.hidden = false;

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
    let dangHien = true, ngayChon = 0, tuBay = false, henGio = null;
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
                if (tuBay) henGio = setTimeout(sangNgaySau, 1800);
            }
        }
        datCanh(t);
        renderer.render(canh, camera);
    }

    function chonNgay(i, tuNut) {
        ngayChon = i;
        if (henGio) clearTimeout(henGio);
        uDau = u;
        uDich = uNgay[i];
        if (giamChuyenDong) {
            u = uDich;
            batDauBay = 0;
            ve(performance.now());
        } else {
            batDauBay = performance.now();
            thoiGianBay = 900 + Math.abs(uDich - uDau) * DAI * 55;
        }
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

    function sangNgaySau() {
        if (ngayChon >= SO_NGAY - 1) {
            dungTuBay();
            return;
        }
        chonNgay(ngayChon + 1, false);
    }

    function dungTuBay() {
        tuBay = false;
        nutBay.textContent = "Bay cả hành trình";
        if (henGio) clearTimeout(henGio);
    }

    dsNgay.forEach((n, i) => {
        const nut = document.createElement("button");
        nut.type = "button";
        nut.textContent = "Ngày " + n.Ngay;
        nut.addEventListener("click", () => chonNgay(i, true));
        nhomNut.appendChild(nut);
    });

    nutBay.addEventListener("click", () => {
        if (tuBay) {
            dungTuBay();
            return;
        }
        tuBay = true;
        nutBay.textContent = "Dừng lại";
        chonNgay(0, false);
        if (giamChuyenDong) {
            dungTuBay();
        } else if (!batDauBay) {
            sangNgaySau();
        }
    });

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
    chonNgay(0, false);

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
