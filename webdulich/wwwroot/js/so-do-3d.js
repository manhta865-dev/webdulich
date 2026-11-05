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

    dungThan(THREE, canh, duLieu.LaMayBay, { rong, dai, dauXe, duoiXe, giuaZ });

    // Ghế: gom theo trạng thái rồi dùng InstancedMesh để máy bay 180 chỗ vẫn nhẹ
    const hinhDem = new THREE.BoxGeometry(0.74, 0.2, 0.68);
    const hinhTua = new THREE.BoxGeometry(0.74, 0.66, 0.16);
    const trungGian = new THREE.Object3D();
    const dsMesh = [];
    const vatLieuSang = [];
    Object.keys(MAU).forEach((loai) => {
        const dsCho = duLieu.DsCho.filter((c) => c.Loai === loai);
        if (!dsCho.length) return;
        const m = MAU[loai];
        const vatLieu = new THREE.MeshStandardMaterial({
            color: m.mau, roughness: 0.55,
            emissive: m.sang || 0x000000, emissiveIntensity: m.doSang || 0
        });
        if (loai === "da-tra") vatLieuSang.push(vatLieu);
        [[hinhDem, 0.38, 0], [hinhTua, 0.74, 0.28]].forEach(([hinh, y, lechZ]) => {
            const mesh = new THREE.InstancedMesh(hinh, vatLieu, dsCho.length);
            dsCho.forEach((c, i) => {
                trungGian.position.set(viTriX(c.Cot), y, viTriZ(c.Hang) + lechZ);
                trungGian.updateMatrix();
                mesh.setMatrixAt(i, trungGian.matrix);
            });
            mesh.userData.dsCho = dsCho;
            canh.add(mesh);
            dsMesh.push(mesh);
        });
    });

    // Nhãn mã chỗ cho ghế của nhóm
    const cuaNhom = duLieu.DsCho.filter((c) => c.Ten);
    cuaNhom.forEach((c) => {
        const nhan = taoNhan(THREE, c.Ma);
        nhan.position.set(viTriX(c.Cot), 1.42, viTriZ(c.Hang) + 0.1);
        canh.add(nhan);
    });

    // Xe nhỏ thì nhìn cả xe; sơ đồ dài như máy bay thì nhìn vào khu ghế của nhóm để ghế không bị li ti
    const muTieu = new THREE.Vector3(0, 0.45, giuaZ);
    let banKinh = Math.hypot(rong, dai) / 2 * 0.92;
    if (soHang > 15) {
        const dsZ = cuaNhom.map((c) => viTriZ(c.Hang));
        const nho = Math.min(...dsZ), lon = Math.max(...dsZ);
        muTieu.z = (nho + lon) / 2;
        banKinh = Math.max(4.6, (lon - nho) / 2 + 3);
    }

    let gocNgang = 2.5, gocDoc = 0.95, tuDong = !giamChuyenDong, dangHien = true;

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
            : cho.Ma + (cho.Loai === "khach-khac" ? ": khách ngoài nhóm" : ": còn trống");
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

function dungThan(THREE, canh, laMayBay, k) {
    const { rong, dai, dauXe, duoiXe, giuaZ } = k;
    const mauThan = new THREE.MeshStandardMaterial({ color: 0x0A6E8A, roughness: 0.5 });
    const mauSan = new THREE.MeshStandardMaterial({ color: 0x33454E, roughness: 0.9 });
    const mauKinh = new THREE.MeshStandardMaterial({ color: 0x9CD3E0, transparent: true, opacity: 0.22, depthWrite: false, side: THREE.DoubleSide });
    const hop = (r, c, d, vatLieu, x, y, z) => {
        const m = new THREE.Mesh(new THREE.BoxGeometry(r, c, d), vatLieu);
        m.position.set(x, y, z);
        canh.add(m);
        return m;
    };

    hop(rong, 0.14, dai, mauSan, 0, 0.14, giuaZ);
    hop(rong + 0.12, 0.42, dai + 0.12, mauThan, 0, -0.14, giuaZ);

    // Hai bên và đuôi trong suốt để thấy ghế
    [-1, 1].forEach((ben) => hop(0.06, 1.3, dai, mauKinh, ben * (rong / 2 + 0.03), 0.8, giuaZ));
    hop(rong, 1.3, 0.06, mauKinh, 0, 0.8, duoiXe);

    if (laMayBay) {
        const mui = new THREE.Mesh(new THREE.ConeGeometry(rong / 2 + 0.06, rong * 0.9, 24), mauThan);
        mui.rotation.x = -Math.PI / 2;
        mui.scale.y = 1;
        mui.position.set(0, 0.2, dauXe - rong * 0.45);
        canh.add(mui);
        [-1, 1].forEach((ben) => {
            const canh1 = hop(rong * 1.1, 0.1, 4, mauThan, ben * (rong / 2 + rong * 0.55), 0.05, giuaZ);
            canh1.rotation.y = ben * 0.25;
        });
        hop(0.14, 2, 2.4, mauThan, 0, 1.2, duoiXe - 0.8);
    } else {
        // Kính lái, ghế lái, vô lăng, bánh xe
        const kinh = hop(rong, 1.4, 0.06, mauKinh, 0, 0.8, dauXe + 0.14);
        kinh.rotation.x = -0.2;
        const mauGheLai = new THREE.MeshStandardMaterial({ color: 0x5B6B73, roughness: 0.7 });
        hop(0.74, 0.2, 0.68, mauGheLai, -rong / 2 + 0.8, 0.38, dauXe + 1.5);
        hop(0.74, 0.66, 0.16, mauGheLai, -rong / 2 + 0.8, 0.74, dauXe + 1.78);
        const volang = new THREE.Mesh(new THREE.TorusGeometry(0.22, 0.04, 8, 20), mauSan);
        volang.rotation.x = -0.8;
        volang.position.set(-rong / 2 + 0.8, 0.8, dauXe + 0.8);
        canh.add(volang);

        const mauBanh = new THREE.MeshStandardMaterial({ color: 0x1E282D, roughness: 0.9 });
        const hinhBanh = new THREE.CylinderGeometry(0.46, 0.46, 0.3, 24);
        [dauXe + 1.3, duoiXe - 1.3].forEach((z) => {
            [-1, 1].forEach((ben) => {
                const banh = new THREE.Mesh(hinhBanh, mauBanh);
                banh.rotation.z = Math.PI / 2;
                banh.position.set(ben * (rong / 2 + 0.02), -0.34, z);
                canh.add(banh);
            });
        });
    }
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
