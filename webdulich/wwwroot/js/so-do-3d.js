// Xe của nhóm 3D: dựng sơ đồ chỗ thật của chuyến, tô theo trạng thái ghế, ghế đã trả sáng lên.
// Bản 2D do máy chủ dựng sẵn luôn có trong trang. Chỉ khi 3D chạy được mới ẩn bản 2D, nên lỗi ở đây không làm mất thông tin.
// Thư viện three khá nặng nên chỉ tải khi người xem cuộn gần tới khung này.
const goc = document.getElementById("soDoNhom");
if (goc) {
    const moi = new IntersectionObserver((muc) => {
        if (muc.some((m) => m.isIntersecting)) {
            moi.disconnect();
            batDau();
        }
    }, { rootMargin: "200px" });
    moi.observe(goc);
}

async function batDau() {
    // Bản 2D của sơ đồ dài (máy bay): cuộn tới ghế của nhóm, phòng khi 3D không chạy
    const ban2D = goc.querySelector(".dn-2d");
    const gheDau = ban2D.querySelector(".cua-nhom");
    if (gheDau) ban2D.scrollTop = Math.max(0, gheDau.offsetTop - ban2D.offsetTop - 60);

    const thu = document.createElement("canvas");
    if (!thu.getContext("webgl2") && !thu.getContext("webgl")) return;
    try {
        const THREE = await import("/lib/three/three.module.min.js");
        khoiTao(THREE);
    } catch (loi) {
        hienBan2D();
    }
}

function hienBan2D() {
    goc.querySelector(".dn-3d").hidden = true;
    goc.querySelector(".dn-2d").hidden = false;
    goc.querySelector(".dn-3d-nhac").hidden = true;
}

function khoiTao(THREE) {
    const duLieu = JSON.parse(document.getElementById("soDoNhomDuLieu").textContent);
    const khung = goc.querySelector(".dn-3d");
    const canvas = khung.querySelector("canvas");
    const tip = khung.querySelector(".dn-3d-tip");
    const giamChuyenDong = window.matchMedia("(prefers-reduced-motion: reduce)").matches;

    const MAU = {
        "con-trong": { mau: 0xDCE3E6 },
        // Chỗ không mở bán: ghế mờ như không có, để không ai tưởng là còn trống
        "khong-ban": { mau: 0xC9D1D4, doTrong: 0.28 },
        "khach-khac": { mau: 0x7C8A91 },
        "trong": { mau: 0xB9DCE5 },
        "da-nhan": { mau: 0x8FB8C0 },
        "da-dien": { mau: 0x0A6E8A },
        "cho": { mau: 0xD9962B, sang: 0x7A4E08, doSang: 0.35 },
        "da-tra": { mau: 0x27B573, sang: 0x1E8A5A, doSang: 0.6 }
    };

    // Hiện khung trước để đo được kích thước
    khung.hidden = false;
    goc.querySelector(".dn-2d").hidden = true;
    goc.querySelector(".dn-3d-nhac").hidden = false;

    const renderer = new THREE.WebGLRenderer({ canvas, antialias: true, alpha: true });
    renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, 2));
    const canh = new THREE.Scene();
    const camera = new THREE.PerspectiveCamera(32, 1, 0.1, 400);

    canh.add(new THREE.HemisphereLight(0xFFFFFF, 0x9FB2BA, 1.5));
    const den = new THREE.DirectionalLight(0xFFFFFF, 1.6);
    den.position.set(6, 14, -8);
    canh.add(den);

    // Hàng 1 ở phía đầu xe (z âm), cột 1 bên trái
    const soHang = duLieu.SoHang, soCot = duLieu.SoCot;
    const viTriX = (cot) => cot - (soCot + 1) / 2;
    const viTriZ = (hang) => hang - (soHang + 1) / 2;
    const dauXe = viTriZ(1) - 2.2, duoiXe = viTriZ(soHang) + 0.9;
    const rong = soCot + 0.6, dai = duoiXe - dauXe, giuaZ = (dauXe + duoiXe) / 2;

    // Loại phương tiện: máy bay, xe nhỏ kiểu van (16 chỗ, ít hàng), còn lại là xe khách (29, 45 chỗ)
    const kieu = duLieu.LaMayBay ? "may-bay" : soHang <= 6 ? "van" : "xe-khach";
    const thongTinThan = dungThan(THREE, canh, kieu, { rong, dai, dauXe, duoiXe, giuaZ, viTriX });

    // Ghế: đệm, lưng hơi ngả, tựa đầu, hai tay vịn. Gom theo trạng thái rồi dùng InstancedMesh để máy bay 180 chỗ vẫn nhẹ.
    const PHAN_GHE = phanGhe(THREE);
    const trungGian = new THREE.Object3D();
    const dsMesh = [];
    const vatLieuSang = [];
    Object.keys(MAU).forEach((loai) => {
        const dsCho = duLieu.DsCho.filter((c) => c.Loai === loai);
        if (!dsCho.length) return;
        const m = MAU[loai];
        const vatLieu = new THREE.MeshStandardMaterial({
            color: m.mau, roughness: 0.55,
            emissive: m.sang || 0x000000, emissiveIntensity: m.doSang || 0,
            transparent: !!m.doTrong, opacity: m.doTrong || 1, depthWrite: !m.doTrong
        });
        if (loai === "da-tra") vatLieuSang.push(vatLieu);
        PHAN_GHE.forEach((p) => {
            const mesh = new THREE.InstancedMesh(p.hinh, vatLieu, dsCho.length);
            dsCho.forEach((c, i) => {
                trungGian.position.set(viTriX(c.Cot) + p.x, p.y, viTriZ(c.Hang) + p.z);
                trungGian.rotation.set(p.nga || 0, 0, 0);
                trungGian.updateMatrix();
                mesh.setMatrixAt(i, trungGian.matrix);
            });
            mesh.userData.dsCho = dsCho;
            canh.add(mesh);
            dsMesh.push(mesh);
        });
    });

    // Ghế tài xế (xe) màu xám đậm, cùng kiểu ghế khách
    if (thongTinThan.gheLai) {
        const vatLieuLai = new THREE.MeshStandardMaterial({ color: 0x4A5A62, roughness: 0.7 });
        PHAN_GHE.forEach((p) => {
            const m = new THREE.Mesh(p.hinh, vatLieuLai);
            m.position.set(thongTinThan.gheLai.x + p.x, p.y, thongTinThan.gheLai.z + p.z);
            m.rotation.x = p.nga || 0;
            canh.add(m);
        });
    }

    // Nhãn mã chỗ cho ghế của nhóm
    const cuaNhom = duLieu.DsCho.filter((c) => c.Ten);
    cuaNhom.forEach((c) => {
        const nhan = taoNhan(THREE, c.Ma);
        nhan.position.set(viTriX(c.Cot), 1.75, viTriZ(c.Hang) + 0.2);
        canh.add(nhan);
    });

    // Xe nhỏ thì nhìn cả xe; sơ đồ dài như máy bay thì nhìn vào khu ghế của nhóm để ghế không bị li ti
    const muTieu = new THREE.Vector3(0, 0.6, giuaZ);
    // Thân xe mới có mũi, đuôi, gương nên rộng và dài hơn khu ghế một chút
    let banKinh = Math.hypot(rong + 1.4, dai + 1.6) / 2 * 0.9;
    if (soHang > 15) {
        const dsZ = cuaNhom.map((c) => viTriZ(c.Hang));
        const nho = Math.min(...dsZ), lon = Math.max(...dsZ);
        muTieu.z = (nho + lon) / 2;
        banKinh = Math.max(6.5, (lon - nho) / 2 + 4);
    }

    // Xe: nhìn chếch từ bên hông phía trước để thấy dáng mũi, kính lái, bánh xe. Máy bay: nhìn cao hơn để thấy ghế qua nửa trên trong suốt
    let gocNgang = duLieu.LaMayBay ? 2.5 : 2.2, gocDoc = duLieu.LaMayBay ? 0.9 : 1.02, tuDong = !giamChuyenDong, dangHien = true;

    function datCamera(thoiGian) {
        const ngang = gocNgang + (tuDong ? Math.sin(thoiGian * 0.0003) * 0.28 : 0);
        const fovDoc = camera.fov * Math.PI / 180;
        const fovNgang = 2 * Math.atan(Math.tan(fovDoc / 2) * camera.aspect);
        const xa = banKinh / Math.sin(Math.min(fovDoc, fovNgang) / 2);
        camera.position.set(
            muTieu.x + xa * Math.sin(gocDoc) * Math.sin(ngang),
            muTieu.y + xa * Math.cos(gocDoc),
            muTieu.z + xa * Math.sin(gocDoc) * Math.cos(ngang)
        );
        camera.lookAt(muTieu);
    }

    function veMotKhung(thoiGian) {
        vatLieuSang.forEach((v) => { v.emissiveIntensity = giamChuyenDong ? 0.6 : 0.55 + Math.sin(thoiGian * 0.003) * 0.25; });
        datCamera(thoiGian);
        renderer.render(canh, camera);
    }

    function doKichThuoc() {
        const r = khung.clientWidth, c = khung.clientHeight;
        if (!r || !c) return;
        renderer.setSize(r, c, false);
        camera.aspect = r / c;
        camera.updateProjectionMatrix();
        veMotKhung(performance.now());
    }
    new ResizeObserver(doKichThuoc).observe(khung);
    doKichThuoc();

    // Chỉ chạy vòng vẽ khi khung đang trong màn hình; giảm chuyển động thì chỉ vẽ khi có thao tác
    if (!giamChuyenDong) {
        new IntersectionObserver((muc) => { dangHien = muc.some((m) => m.isIntersecting); }).observe(khung);
        const lap = (t) => {
            if (dangHien) veMotKhung(t);
            requestAnimationFrame(lap);
        };
        requestAnimationFrame(lap);
    }

    // ---------- Thao tác: kéo để xoay, bấm hoặc rê lên ghế để xem tên ----------
    const raycaster = new THREE.Raycaster();
    const chuot = new THREE.Vector2();
    let dangKeo = false, xCu = 0, yCu = 0, quangKeo = 0;

    function choTaiDiem(e) {
        const r = canvas.getBoundingClientRect();
        chuot.set((e.clientX - r.left) / r.width * 2 - 1, -((e.clientY - r.top) / r.height) * 2 + 1);
        raycaster.setFromCamera(chuot, camera);
        const trung = raycaster.intersectObjects(dsMesh, false)[0];
        return trung ? trung.object.userData.dsCho[trung.instanceId] : null;
    }

    function hienTip(cho, e) {
        if (!cho) {
            tip.hidden = true;
            return;
        }
        tip.textContent = cho.Ten
            ? cho.Ma + ": " + cho.Ten + ", " + cho.TrangThai.toLowerCase()
            : cho.Ma + (cho.Loai === "khach-khac" ? ": khách ngoài nhóm" : cho.Loai === "khong-ban" ? ": không mở bán" : ": còn trống");
        tip.hidden = false;
        const r = khung.getBoundingClientRect();
        const x = Math.min(e.clientX - r.left + 12, r.width - tip.offsetWidth - 8);
        const y = Math.max(e.clientY - r.top - tip.offsetHeight - 10, 8);
        tip.style.transform = "translate(" + Math.max(x, 8) + "px," + y + "px)";
    }

    canvas.addEventListener("pointerdown", (e) => {
        dangKeo = true;
        quangKeo = 0;
        xCu = e.clientX;
        yCu = e.clientY;
        canvas.setPointerCapture(e.pointerId);
    });
    canvas.addEventListener("pointermove", (e) => {
        if (!dangKeo) {
            if (e.pointerType === "mouse") {
                const cho = choTaiDiem(e);
                canvas.style.cursor = cho ? "pointer" : "grab";
                hienTip(cho, e);
            }
            return;
        }
        const dx = e.clientX - xCu, dy = e.clientY - yCu;
        quangKeo += Math.abs(dx) + Math.abs(dy);
        xCu = e.clientX;
        yCu = e.clientY;
        if (quangKeo > 6) {
            tuDong = false;
            tip.hidden = true;
            gocNgang -= dx * 0.008;
            gocDoc = Math.min(1.35, Math.max(0.35, gocDoc - dy * 0.006));
            if (giamChuyenDong) veMotKhung(performance.now());
        }
    });
    const thaTay = (e) => {
        if (dangKeo && quangKeo <= 6) hienTip(choTaiDiem(e), e);
        dangKeo = false;
    };
    canvas.addEventListener("pointerup", thaTay);
    canvas.addEventListener("pointercancel", () => { dangKeo = false; });
    canvas.addEventListener("pointerleave", () => { if (!dangKeo) tip.hidden = true; });

    canvas.addEventListener("webglcontextlost", (e) => {
        e.preventDefault();
        hienBan2D();
    });
}

// Các phần của một ghế, tọa độ so với tâm chỗ ngồi (khách quay mặt về đầu xe, tức hướng z âm)
function phanGhe(THREE) {
    return [
        { hinh: new THREE.BoxGeometry(0.66, 0.16, 0.6), x: 0, y: 0.42, z: 0.02 },                // đệm ngồi
        { hinh: new THREE.BoxGeometry(0.66, 0.72, 0.13), x: 0, y: 0.84, z: 0.31, nga: -0.16 },   // lưng tựa hơi ngả ra sau
        { hinh: new THREE.BoxGeometry(0.4, 0.2, 0.12), x: 0, y: 1.3, z: 0.39, nga: -0.16 },      // tựa đầu
        { hinh: new THREE.BoxGeometry(0.07, 0.08, 0.5), x: -0.36, y: 0.6, z: 0.06 },             // tay vịn trái
        { hinh: new THREE.BoxGeometry(0.07, 0.08, 0.5), x: 0.36, y: 0.6, z: 0.06 },              // tay vịn phải
        { hinh: new THREE.BoxGeometry(0.5, 0.3, 0.42), x: 0, y: 0.2, z: 0.06 }                   // chân ghế
    ];
}

// Dựng thân phương tiện quanh khu ghế. Xe: phần dưới cửa sổ đặc, dải kính trong suốt có khung, không có mui
// để nhìn thấy ghế. Máy bay: thân ống, nửa trên trong suốt. Trả về vị trí ghế tài xế (nếu có).
function dungThan(THREE, canh, kieu, k) {
    return kieu === "may-bay" ? dungMayBay(THREE, canh, k) : dungXe(THREE, canh, kieu, k);
}

// Bóng mờ dưới gầm cho xe có cảm giác đứng trên mặt đất
function bongDo(THREE, canh, rong, dai, y, z) {
    const c = document.createElement("canvas");
    c.width = c.height = 128;
    const ctx = c.getContext("2d");
    const g = ctx.createRadialGradient(64, 64, 8, 64, 64, 64);
    g.addColorStop(0, "rgba(16,36,44,0.45)");
    g.addColorStop(1, "rgba(16,36,44,0)");
    ctx.fillStyle = g;
    ctx.fillRect(0, 0, 128, 128);
    const t = new THREE.CanvasTexture(c);
    const m = new THREE.Mesh(new THREE.PlaneGeometry(rong, dai), new THREE.MeshBasicMaterial({ map: t, transparent: true, depthWrite: false }));
    m.rotation.x = -Math.PI / 2;
    m.position.set(0, y, z);
    canh.add(m);
}

function dungXe(THREE, canh, kieu, k) {
    const { rong, dauXe, duoiXe, viTriX } = k;
    const laVan = kieu === "van";

    // Kích thước theo loại xe (đơn vị = một ô ghế)
    const day = laVan ? -0.3 : -0.42;            // đáy thân
    const that = laVan ? 0.78 : 0.86;            // mép dưới cửa sổ
    const mui = laVan ? 2.0 : 2.28;              // nóc
    const rBanh = laVan ? 0.4 : 0.52;
    const beNgang = rong + 0.3;
    const zDau = dauXe - (laVan ? 0.45 : 0.25);  // mặt trước cản
    const zDuoi = duoiXe + 0.3;
    const xD = -zDau, xS = -zDuoi;               // tọa độ trong hình 2D (đầu xe là x lớn)
    const daiNap = laVan ? 0.75 : 0.06;          // nắp capo: van có mũi ngắn, xe khách đầu phẳng
    const nghiengKinh = laVan ? 1.05 : 0.28;     // kính lái lùi vào bao nhiêu ở mép trên

    const mauThan = new THREE.MeshStandardMaterial({ color: 0xF3F6F7, roughness: 0.35, metalness: 0.1, side: THREE.DoubleSide });
    const mauKinh = new THREE.MeshStandardMaterial({ color: 0x7FB9CB, roughness: 0.1, transparent: true, opacity: 0.2, depthWrite: false, side: THREE.DoubleSide });
    // Kính lái, kính đuôi tối màu như kính xe thật
    const mauKinhToi = new THREE.MeshStandardMaterial({ color: 0x1C3440, roughness: 0.1, metalness: 0.2, transparent: true, opacity: 0.5, depthWrite: false, side: THREE.DoubleSide });
    const mauKhung = new THREE.MeshStandardMaterial({ color: 0x1F2B31, roughness: 0.6 });
    const mauSan = new THREE.MeshStandardMaterial({ color: 0x3B4A52, roughness: 0.9 });
    const mauNhan = new THREE.MeshStandardMaterial({ color: 0x0A6E8A, roughness: 0.45 });
    const mauVay = new THREE.MeshStandardMaterial({ color: 0x2E3A40, roughness: 0.7 });
    const hop = (r, c, d, vatLieu, x, y, z) => {
        const m = new THREE.Mesh(new THREE.BoxGeometry(r, c, d), vatLieu);
        m.position.set(x, y, z);
        canh.add(m);
        return m;
    };

    // Dáng nghiêng phần thân dưới cửa sổ (trong hình 2D: trục ngang là chiều dài, x lớn là đầu xe;
    // khi dựng vách hông, đầu xe nằm ở z âm như sơ đồ ghế): cản trước, nắp capo ngắn, đuôi bo tròn
    const duoi = new THREE.Shape();
    duoi.moveTo(xS + 0.25, day);
    duoi.lineTo(xD - 0.2, day);
    duoi.quadraticCurveTo(xD, day, xD, day + 0.22);
    duoi.lineTo(xD, laVan ? 0.48 : that - 0.05);
    duoi.quadraticCurveTo(xD - 0.05, laVan ? 0.68 : that, xD - daiNap, that);
    duoi.lineTo(xS, that);
    duoi.lineTo(xS, day + 0.25);
    duoi.quadraticCurveTo(xS, day, xS + 0.25, day);
    // Dải kính quanh xe: kính lái nghiêng ở đầu, đuôi bo tròn, không có mui
    const kinh = new THREE.Shape();
    kinh.moveTo(xS, that);
    kinh.lineTo(xD - daiNap, that);
    kinh.lineTo(xD - daiNap - nghiengKinh, mui - 0.12);
    kinh.quadraticCurveTo(xD - daiNap - nghiengKinh - 0.12, mui, xD - daiNap - nghiengKinh - 0.3, mui);
    kinh.lineTo(xS + 0.3, mui);
    kinh.quadraticCurveTo(xS, mui, xS, mui - 0.3);
    kinh.lineTo(xS, that);

    // Thân là các tấm vách, bên trong rỗng và không có mui để nhìn thấy ghế:
    // hai hông theo đúng dáng nghiêng, mặt trước, nắp capo, kính lái, vách đuôi, kính đuôi
    const vachHong = (hinh2D, vatLieu, x) => {
        const g = new THREE.ShapeGeometry(hinh2D, 14);
        g.rotateY(Math.PI / 2);
        const m = new THREE.Mesh(g, vatLieu);
        m.position.x = x;
        canh.add(m);
        const vienM = new THREE.LineSegments(new THREE.EdgesGeometry(g, 20),
            new THREE.LineBasicMaterial({ color: 0x1F2B31, transparent: true, opacity: 0.35 }));
        vienM.position.x = x;
        canh.add(vienM);
    };
    // Tấm phẳng nối hai điểm (y, z) theo bề ngang xe
    const tamNgang = (y1, z1, y2, z2, vatLieu, day = 0.05) => {
        const dai = Math.hypot(y2 - y1, z2 - z1);
        const m = new THREE.Mesh(new THREE.BoxGeometry(beNgang, dai, day), vatLieu);
        m.position.set(0, (y1 + y2) / 2, (z1 + z2) / 2);
        m.rotation.x = Math.atan2(z2 - z1, y2 - y1);
        canh.add(m);
        return m;
    };
    [-1, 1].forEach((ben) => {
        vachHong(duoi, mauThan, ben * beNgang / 2);
        vachHong(kinh, mauKinh, ben * beNgang / 2);
    });
    const dinhCan = laVan ? 0.48 : that - 0.05;
    tamNgang(day + 0.2, zDau, dinhCan, zDau, mauThan, 0.08);                       // mặt trước, cản
    tamNgang(dinhCan, zDau, that, zDau + daiNap, mauThan);                          // nắp capo
    tamNgang(that, zDau + daiNap, mui - 0.1, zDau + daiNap + nghiengKinh, mauKinhToi); // kính lái
    tamNgang(day + 0.2, zDuoi, that, zDuoi, mauThan, 0.08);                         // vách đuôi
    tamNgang(that, zDuoi, mui - 0.25, zDuoi, mauKinhToi);                          // kính đuôi
    // Đoạn mui đặc phía trên khoang tài xế (phần còn lại để trống cho thấy ghế)
    const zMuiDau = zDau + daiNap + nghiengKinh;
    const daiMuiDau = laVan ? 0.7 : 1.0;
    hop(beNgang, 0.08, daiMuiDau, mauThan, 0, mui - 0.02, zMuiDau + daiMuiDau / 2 - 0.05);

    // Sàn trong xe
    hop(rong, 0.1, zDuoi - zDau - 0.4, mauSan, 0, 0.16, (zDau + zDuoi) / 2 + 0.1);

    const mat = beNgang / 2 + 0.012;           // mặt ngoài hai bên hông
    const zKinhDau = zDau + daiNap + nghiengKinh + 0.25;  // cửa sổ hông bắt đầu sau kính lái

    [-1, 1].forEach((ben) => {
        const x = ben * mat;
        // Vạch màu nhấn dọc thân, viền váy tối dưới cùng
        hop(0.02, 0.09, zDuoi - zDau - 0.6, mauNhan, x, that - 0.22, (zDau + zDuoi) / 2 + 0.15);
        hop(0.02, 0.16, zDuoi - zDau - 0.3, mauVay, x, day + 0.1, (zDau + zDuoi) / 2);
        // Khung cửa sổ: thanh trên, thanh dưới, các trụ đứng
        hop(0.05, 0.07, zDuoi - zKinhDau, mauKhung, x, mui - 0.04, (zKinhDau + zDuoi) / 2);
        hop(0.05, 0.06, zDuoi - zDau - daiNap, mauKhung, x, that + 0.02, (zDau + daiNap + zDuoi) / 2);
        const buoc = laVan ? 1.25 : 1.55;
        for (let z = zKinhDau; z <= zDuoi - 0.2; z += buoc) {
            hop(0.05, mui - that, 0.09, mauKhung, x, (mui + that) / 2, z);
        }
        // Trụ A chạy theo mép kính lái
        const dauDuoi = new THREE.Vector3(x, that, zDau + daiNap);
        const dauTren = new THREE.Vector3(x, mui - 0.05, zDau + daiNap + nghiengKinh);
        const truA = hop(0.06, dauDuoi.distanceTo(dauTren), 0.1, mauKhung, x, (that + mui) / 2, (dauDuoi.z + dauTren.z) / 2);
        truA.rotation.x = Math.atan2(dauTren.z - dauDuoi.z, dauTren.y - dauDuoi.y);
        // Gương chiếu hậu
        hop(0.32, 0.05, 0.05, mauKhung, ben * (mat + 0.16), that + 0.45, zDau + daiNap + 0.1);
        hop(0.06, laVan ? 0.3 : 0.42, 0.16, mauKhung, ben * (mat + 0.32), that + 0.35, zDau + daiNap + 0.08);
        // Đèn pha và đèn hậu
        const den = new THREE.MeshStandardMaterial({ color: 0xFFF6DA, emissive: 0xFFE7A8, emissiveIntensity: 0.55 });
        hop(laVan ? 0.42 : 0.36, laVan ? 0.14 : 0.18, 0.04, den, ben * (beNgang / 2 - 0.3), laVan ? 0.52 : 0.08, zDau - 0.05);
        const denHau = new THREE.MeshStandardMaterial({ color: 0xC0392B, emissive: 0xA01E12, emissiveIntensity: 0.5 });
        hop(0.18, laVan ? 0.7 : 0.5, 0.04, denHau, ben * (beNgang / 2 - 0.12), laVan ? 0.75 : 0.35, zDuoi + 0.05);
    });

    // Lưới tản nhiệt, biển số
    hop(beNgang * (laVan ? 0.5 : 0.62), laVan ? 0.26 : 0.16, 0.04, mauKhung, 0, laVan ? 0.24 : -0.12, zDau - 0.05);
    hop(0.62, 0.16, 0.03, new THREE.MeshStandardMaterial({ color: 0xFFFFFF }), 0, laVan ? -0.06 : -0.3, zDau - 0.07);

    // Cửa lên xe: viền tối bên phải phía trước (xe khách) hoặc cửa lùa bên phải (van)
    const zCua = laVan ? zKinhDau + 0.9 : zDau + 0.55;
    hop(0.02, that - day - 0.15, 0.04, mauKhung, mat, (that + day) / 2 + 0.05, zCua - 0.45);
    hop(0.02, that - day - 0.15, 0.04, mauKhung, mat, (that + day) / 2 + 0.05, zCua + 0.45);

    // Bánh xe: lốp, mâm bạc, vòm bánh tối
    const lop = new THREE.CylinderGeometry(rBanh, rBanh, 0.32, 28);
    const mam = new THREE.CylinderGeometry(rBanh * 0.55, rBanh * 0.55, 0.34, 20);
    const vom = new THREE.CircleGeometry(rBanh * 1.12, 28);
    const mauLop = new THREE.MeshStandardMaterial({ color: 0x1B2226, roughness: 0.95 });
    const mauMam = new THREE.MeshStandardMaterial({ color: 0xC9D2D6, roughness: 0.3, metalness: 0.6 });
    const zBanh = [zDau + (laVan ? 0.95 : 1.45), zDuoi - (laVan ? 1.05 : 2.3)];
    const yBanh = day - rBanh * 0.25;
    zBanh.forEach((z) => {
        [-1, 1].forEach((ben) => {
            const lopM = new THREE.Mesh(lop, mauLop);
            lopM.rotation.z = Math.PI / 2;
            lopM.position.set(ben * (beNgang / 2 - 0.05), yBanh, z);
            canh.add(lopM);
            const mamM = new THREE.Mesh(mam, mauMam);
            mamM.rotation.z = Math.PI / 2;
            mamM.position.copy(lopM.position);
            canh.add(mamM);
            const vomM = new THREE.Mesh(vom, mauVay);
            vomM.rotation.y = ben * Math.PI / 2;
            vomM.position.set(ben * (mat + 0.004), yBanh + 0.02, z);
            canh.add(vomM);
        });
    });

    bongDo(THREE, canh, beNgang * 1.7, (zDuoi - zDau) * 1.15, yBanh - rBanh + 0.01, (zDau + zDuoi) / 2);

    // Vô lăng trước ghế tài xế (bên trái)
    const xLai = viTriX(1);
    const zLai = zDau + daiNap + (laVan ? 1.05 : 1.1);
    const volang = new THREE.Mesh(new THREE.TorusGeometry(0.2, 0.035, 8, 24), mauKhung);
    volang.rotation.x = -1.0;
    volang.position.set(xLai, 0.95, zLai - 0.55);
    canh.add(volang);
    hop(0.06, 0.06, 0.4, mauKhung, xLai, 0.82, zLai - 0.38).rotation.x = -0.6;

    return { gheLai: { x: xLai, z: zLai } };
}

function dungMayBay(THREE, canh, k) {
    const { rong, dauXe, duoiXe } = k;
    const R = rong / 2 + 0.35;                    // bán kính thân
    const truc = 0.2 + R * 0.32;                  // sàn khoang nằm dưới trục thân
    const zMui = dauXe + 0.6, zDuoi = duoiXe + 0.4;
    const daiThan = zDuoi - zMui;
    const goc = Math.PI * 0.6;                    // phần đặc ôm từ đáy lên quá nửa thân

    const bac = new THREE.MeshStandardMaterial({ color: 0xD9E0E4, roughness: 0.35, metalness: 0.35, side: THREE.DoubleSide });
    const kinh = new THREE.MeshStandardMaterial({ color: 0x9FCFDD, roughness: 0.1, transparent: true, opacity: 0.12, depthWrite: false, side: THREE.DoubleSide });
    const nhan = new THREE.MeshStandardMaterial({ color: 0x0A6E8A, roughness: 0.4, side: THREE.DoubleSide });
    const dam = new THREE.MeshStandardMaterial({ color: 0x1F2B31, roughness: 0.6 });

    // Thân ống: một lát cắt theo góc quanh trục (theta = 0 là đáy)
    const ong = (r1, r2, dai, batDau, rongGoc, vatLieu, z) => {
        const g = new THREE.CylinderGeometry(r2, r1, dai, 48, 1, true, batDau, rongGoc);
        g.rotateX(Math.PI / 2);
        const m = new THREE.Mesh(g, vatLieu);
        m.position.set(0, truc, z);
        canh.add(m);
        return m;
    };
    ong(R, R, daiThan, -goc, goc * 2, bac, (zMui + zDuoi) / 2);
    ong(R, R, daiThan, goc, Math.PI * 2 - goc * 2, kinh, (zMui + zDuoi) / 2);
    // Vạch màu chạy dọc thân ở hai bên
    ong(R + 0.01, R + 0.01, daiThan, goc - 0.07, 0.05, nhan, (zMui + zDuoi) / 2);
    ong(R + 0.01, R + 0.01, daiThan, -goc + 0.02, 0.05, nhan, (zMui + zDuoi) / 2);

    // Mũi thuôn: nửa quả cầu kéo dài về phía trước, nửa dưới đặc, nửa trên trong suốt
    const mui = (batDau, rongGoc, vatLieu) => {
        const g = new THREE.SphereGeometry(R, 40, 20, batDau, rongGoc, 0, Math.PI / 2);
        g.rotateX(-Math.PI / 2);
        g.scale(1, 1, 2.1);
        const m = new THREE.Mesh(g, vatLieu);
        m.position.set(0, truc, zMui);
        canh.add(m);
    };
    mui(Math.PI * 1.5 - goc, goc * 2, bac);
    mui(Math.PI * 1.5 + goc, Math.PI * 2 - goc * 2, kinh);

    // Đuôi: ống thon dần, hơi vểnh lên
    const daiDuoi = R * 2.6;
    const duoi1 = ong(R, R * 0.22, daiDuoi, -goc, goc * 2, bac, zDuoi + daiDuoi / 2);
    const duoi2 = ong(R, R * 0.22, daiDuoi, goc, Math.PI * 2 - goc * 2, kinh, zDuoi + daiDuoi / 2);
    [duoi1, duoi2].forEach((m) => { m.position.y += R * 0.12; m.rotation.x = -0.1; });

    // Hình phẳng đùn mỏng (cánh, đuôi)
    const tamPhang = (diem, day, vatLieu) => {
        const s = new THREE.Shape();
        s.moveTo(diem[0][0], diem[0][1]);
        diem.slice(1).forEach((d) => s.lineTo(d[0], d[1]));
        return new THREE.Mesh(new THREE.ExtrudeGeometry(s, { depth: day, bevelEnabled: false }), vatLieu);
    };

    // Đuôi đứng (mặt phẳng dọc thân), màu nhấn
    const caoDuoi = R * 1.5;
    const duoiDung = tamPhang([[0, 0], [R * 1.6, 0], [R * 1.9, caoDuoi], [R * 1.25, caoDuoi]], 0.22, nhan);
    duoiDung.geometry.rotateY(-Math.PI / 2);
    duoiDung.position.set(0.11, truc + R * 0.8, zDuoi + daiDuoi * 0.35);
    canh.add(duoiDung);

    // Cánh và đuôi ngang: hình chiếu bằng, xuôi về sau; nửa trái lật từ nửa phải
    const canhPhang = (goc0, sai, dayCanh, dauSau, cuoiTruoc, cuoiSau, y, z, vatLieu) => {
        [-1, 1].forEach((ben) => {
            const m = tamPhang([[goc0, 0], [goc0 + sai, -cuoiTruoc], [goc0 + sai, -cuoiSau], [goc0, -dauSau]], dayCanh, vatLieu);
            m.geometry.rotateX(-Math.PI / 2);
            m.geometry.scale(ben, 1, 1);
            m.position.set(0, y, z);
            canh.add(m);
        });
    };
    const zCanh = (zMui + zDuoi) / 2 - R * 0.6;
    const saiCanh = R * 2.7;
    canhPhang(R * 0.6, saiCanh, 0.16, R * 1.5, R * 1.25, R * 1.75, truc - R * 0.55, zCanh, bac);
    canhPhang(R * 0.25, R * 1.2, 0.1, R * 0.8, R * 0.7, R * 1.0, truc + R * 0.25, zDuoi + daiDuoi * 0.45, bac);

    // Động cơ dưới cánh và đầu cánh vểnh lên
    [-1, 1].forEach((ben) => {
        const xDc = ben * (R * 0.6 + saiCanh * 0.38);
        // Mép trước cánh ở vị trí động cơ lùi 0.38 * 1.25R so với gốc cánh; động cơ nhô ra trước mép đó một chút
        const zDc = zCanh + R * 0.32;
        const dc = new THREE.Mesh(new THREE.CylinderGeometry(R * 0.3, R * 0.26, R * 1.1, 28), nhan);
        dc.rotation.x = Math.PI / 2;
        dc.position.set(xDc, truc - R * 0.95, zDc);
        canh.add(dc);
        const cuaGio = new THREE.Mesh(new THREE.CircleGeometry(R * 0.26, 28), dam);
        cuaGio.rotation.y = Math.PI;
        cuaGio.position.set(xDc, truc - R * 0.95, zDc - R * 0.56);
        canh.add(cuaGio);
        const vay = new THREE.Mesh(new THREE.BoxGeometry(0.12, R * 0.45, R * 0.35), bac);
        vay.position.set(ben * (R * 0.6 + saiCanh), truc - R * 0.55 + R * 0.2, zCanh + R * 1.25 + R * 0.12);
        vay.rotation.z = -ben * 0.2;
        canh.add(vay);
    });

    // Dãy cửa sổ hai bên, ngay dưới mép phần thân đặc
    const gocCuaSo = goc - 0.16;
    const soCuaSo = Math.max(1, Math.floor((daiThan - 2) / 0.9));
    const cuaSo = new THREE.InstancedMesh(new THREE.BoxGeometry(0.05, 0.3, 0.2), dam, soCuaSo * 2);
    const tg = new THREE.Object3D();
    let i = 0;
    [-1, 1].forEach((ben) => {
        for (let n = 0; n < soCuaSo; n++) {
            const nx = ben * Math.sin(gocCuaSo), ny = -Math.cos(gocCuaSo);
            tg.position.set(nx * (R + 0.02), truc + ny * (R + 0.02), zMui + 1.2 + n * 0.9);
            tg.rotation.set(0, 0, Math.atan2(ny, nx));
            tg.updateMatrix();
            cuaSo.setMatrixAt(i++, tg.matrix);
        }
    });
    canh.add(cuaSo);

    // Kính buồng lái trên mũi
    const kinhLai = new THREE.Mesh(new THREE.BoxGeometry(R * 0.9, 0.2, 0.5), dam);
    kinhLai.position.set(0, truc + R * 0.55, zMui - R * 1.35);
    kinhLai.rotation.x = 0.5;
    canh.add(kinhLai);

    // Sàn khoang
    const san = new THREE.Mesh(new THREE.BoxGeometry(rong, 0.1, daiThan), new THREE.MeshStandardMaterial({ color: 0x3B4A52, roughness: 0.9 }));
    san.position.set(0, 0.16, (zMui + zDuoi) / 2);
    canh.add(san);

    return {};
}

function taoNhan(THREE, chu) {
    const c = document.createElement("canvas");
    c.width = 128;
    c.height = 64;
    const ctx = c.getContext("2d");
    ctx.fillStyle = "#17262E";
    ctx.beginPath();
    ctx.roundRect(4, 4, 120, 56, 18);
    ctx.fill();
    ctx.fillStyle = "#FFFFFF";
    ctx.font = '700 34px "Be Vietnam Pro", system-ui, sans-serif';
    ctx.textAlign = "center";
    ctx.textBaseline = "middle";
    ctx.fillText(chu, 64, 33);
    const texture = new THREE.CanvasTexture(c);
    texture.colorSpace = THREE.SRGBColorSpace;
    const sprite = new THREE.Sprite(new THREE.SpriteMaterial({ map: texture, depthTest: false, transparent: true }));
    sprite.scale.set(0.9, 0.45, 1);
    sprite.renderOrder = 10;
    return sprite;
}
