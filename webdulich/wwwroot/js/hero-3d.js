// Quả địa cầu 3D ở đầu trang chủ (Three.js, file đặt tại /lib/three).
// Gồm: Việt Nam phát sáng, mây, khí quyển, nền sao, máy bay bay vòng và các điểm đến bấm được.
// Nếu trình duyệt không hỗ trợ WebGL thì ẩn quả cầu, trang vẫn dùng ảnh nền như cũ.
// Thư viện three khá nặng: chỉ tải sau khi trang đã hiện, và không tải khi CSS đã ẩn quả cầu (màn hình hẹp, điện thoại).
const khung = document.getElementById("hero-globe");
const canvas = khung && khung.querySelector("canvas");
const nhan = khung && khung.querySelector(".hero-globe-label");
let THREE = null;
let daBatDau = false, soLanThu = 0;

async function batDau() {
    if (daBatDau) return;
    daBatDau = true;
    try {
        THREE = THREE || await import("/lib/three/three.module.min.js");
        khoiTao();
    } catch (loi) {
        // Trình duyệt có lúc chưa tạo được WebGL (máy đang bận, nhiều tab 3D): thử lại một lần rồi mới ẩn
        console.warn("Quả địa cầu chưa chạy được:", loi && loi.message);
        if (++soLanThu < 2) {
            daBatDau = false;
            setTimeout(batDau, 1500);
        } else {
            khung.hidden = true;
        }
    }
}

if (khung && canvas && getComputedStyle(khung).display !== "none") {
    // Đợi trang tải xong để không tranh băng thông với ảnh đầu trang, nhưng không đợi quá 2,5 giây:
    // nếu còn một tài nguyên nào đó tải chậm (bản đồ, ảnh bên ngoài) thì quả cầu vẫn hiện
    if (document.readyState === "complete") {
        batDau();
    } else {
        window.addEventListener("load", batDau, { once: true });
        setTimeout(batDau, 2500);
    }
}

function khoiTao() {
    const RAD = Math.PI / 180;
    const giamChuyenDong = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    const duongDanTour = khung.dataset.tour || "/DiaDiem/DanhSach";

    // Đường bao gần đúng (vĩ độ, kinh độ)
    const VIET_NAM = [
        [23.3, 105.3], [22.8, 106.7], [22.5, 108.0], [21.5, 108.0], [20.9, 107.0], [20.2, 106.6],
        [19.0, 105.8], [18.0, 106.1], [17.0, 107.2], [16.1, 108.2], [15.0, 109.0], [13.8, 109.3],
        [12.3, 109.3], [11.3, 109.0], [10.5, 107.8], [9.5, 106.5], [8.6, 104.8], [9.5, 104.8],
        [10.4, 104.8], [11.0, 106.0], [12.0, 107.5], [14.0, 107.5], [15.5, 107.2], [17.0, 105.5],
        [18.5, 104.0], [20.0, 104.2], [21.5, 103.2], [22.5, 102.2], [23.3, 103.5]
    ];
    // Đông Nam Á lục địa, rất thô, chỉ để quanh Việt Nam có đất liền mờ
    const LAN_CAN = [
        [25, 97], [24, 102], [23.3, 105], [21, 104], [17, 105.5], [12, 107.5], [10.5, 105], [10.5, 103],
        [12.5, 102], [13.5, 100.5], [12, 100], [9, 99], [6.5, 100.2], [4, 101.5], [1.5, 104], [1.5, 103],
        [4, 100.5], [6, 99.8], [8, 98.3], [10, 98.6], [13, 98], [16, 97.5], [20, 97.8], [22, 97.5]
    ];
    // Hai quần đảo thuộc chủ quyền Việt Nam, phải luôn được vẽ cùng màu với Việt Nam và có ghi tên.
    // Tọa độ gần đúng theo các đảo chính, chỉ để minh họa trên quả cầu.
    const HOANG_SA = [[16.95, 112.3], [16.85, 111.95], [16.5, 111.7], [16.1, 111.5], [16.65, 112.65]];
    const TRUONG_SA = [
        [11.43, 114.33], [10.72, 114.42], [10.38, 114.36], [9.9, 114.4], [9.7, 115.85],
        [8.85, 112.85], [8.65, 111.92], [7.9, 113.8], [10.2, 113.6]
    ];
    // Đèn thành phố lớn (vĩ độ, kinh độ, cỡ chấm), chỉ để trang trí phía ban đêm
    const DEN_THANH_PHO = [
        [21.03, 105.85, 4], [10.78, 106.7, 4.5], [16.05, 108.2, 3], [20.86, 106.68, 2.5], [10.03, 105.78, 2.5],
        [16.46, 107.59, 2], [12.24, 109.19, 2], [11.94, 108.44, 2], [18.67, 105.69, 2],
        [13.75, 100.5, 4.5], [11.55, 104.92, 3], [17.97, 102.6, 2.5], [16.87, 96.2, 3],
        [3.14, 101.69, 3.5], [1.35, 103.82, 3.5], [14.6, 120.98, 4], [22.3, 114.17, 4], [23.13, 113.26, 4]
    ];
    // Điểm đến lấy từ tour đang bán (TrangChuController.Index, data-diem); admin thêm tour là quả cầu tự có chấm mới.
    // Danh sách cố định bên dưới chỉ dùng khi chưa có tour nào tra được vị trí.
    let diemTuTour = [];
    try { diemTuTour = JSON.parse(khung.dataset.diem || "[]"); } catch (e) { diemTuTour = []; }
    const DIEM_DEN = diemTuTour.length ? diemTuTour : [
        { ten: "Hà Nội", lat: 21.03, lon: 105.85 },
        { ten: "Sa Pa", lat: 22.34, lon: 103.84 },
        { ten: "Hạ Long", lat: 20.95, lon: 107.07 },
        { ten: "Huế", lat: 16.46, lon: 107.59 },
        { ten: "Đà Nẵng", lat: 16.05, lon: 108.2 },
        { ten: "Nha Trang", lat: 12.24, lon: 109.19 },
        { ten: "Đà Lạt", lat: 11.94, lon: 108.44 },
        { ten: "TP.HCM", lat: 10.78, lon: 106.7 },
        { ten: "Phú Quốc", lat: 10.2, lon: 103.96 }
    ];

    // Vĩ độ, kinh độ -> điểm trên mặt cầu (khớp cách NẶN ảnh phẳng lên SphereGeometry)
    function diem(lat, lon, r) {
        const p = lat * RAD, l = lon * RAD;
        return new THREE.Vector3(r * Math.cos(p) * Math.cos(l), r * Math.sin(p), -r * Math.cos(p) * Math.sin(l));
    }

    function trongDaGiac(lat, lon, dg) {
        let trong = false;
        for (let i = 0, j = dg.length - 1; i < dg.length; j = i++) {
            const yi = dg[i][0], xi = dg[i][1], yj = dg[j][0], xj = dg[j][1];
            if (((yi > lat) !== (yj > lat)) && (lon < (xj - xi) * (lat - yi) / (yj - yi) + xi)) trong = !trong;
        }
        return trong;
    }

    // ---------- Ảnh phủ mặt đất (vẽ bằng canvas, không cần ảnh ngoài) ----------
    const W = 2048, H = 1024;
    const toaDo = (lat, lon) => [(lon + 180) / 360 * W, (90 - lat) / 180 * H];

    function veAnhDat() {
        const c = document.createElement("canvas");
        c.width = W; c.height = H;
        const g = c.getContext("2d");
        const nen = g.createLinearGradient(0, 0, 0, H);
        nen.addColorStop(0, "#1c6c80"); nen.addColorStop(0.5, "#2a93a8"); nen.addColorStop(1, "#1c6c80");
        g.fillStyle = nen;
        g.fillRect(0, 0, W, H);

        g.strokeStyle = "rgba(180,240,245,0.12)";
        g.lineWidth = 1;
        for (let lon = -180; lon <= 180; lon += 15) { const [x] = toaDo(0, lon); g.beginPath(); g.moveTo(x, 0); g.lineTo(x, H); g.stroke(); }
        for (let lat = -75; lat <= 75; lat += 15) { const [, y] = toaDo(lat, 0); g.beginPath(); g.moveTo(0, y); g.lineTo(W, y); g.stroke(); }
        return c;
    }

    function veAnhPhatSang() {
        const c = document.createElement("canvas");
        c.width = W; c.height = H;
        const g = c.getContext("2d");
        g.fillStyle = "#000";
        g.fillRect(0, 0, W, H);
        // Đất liền mờ
        g.fillStyle = "rgba(190,225,215,0.38)";
        for (let la = 0; la <= 26; la += 0.4) {
            for (let lo = 95; lo <= 112; lo += 0.4) {
                if (trongDaGiac(la, lo, LAN_CAN) && !trongDaGiac(la, lo, VIET_NAM)) {
                    const [x, y] = toaDo(la, lo);
                    g.beginPath(); g.arc(x, y, 1.6, 0, 7); g.fill();
                }
            }
        }
        // Việt Nam sáng
        g.fillStyle = "#FFE3A3";
        g.shadowColor = "#FFB35C";
        g.shadowBlur = 6;
        for (let la = 8; la <= 23.6; la += 0.22) {
            for (let lo = 102; lo <= 109.6; lo += 0.22) {
                if (trongDaGiac(la, lo, VIET_NAM)) {
                    const [x, y] = toaDo(la, lo);
                    g.beginPath(); g.arc(x, y, 1.9, 0, 7); g.fill();
                }
            }
        }
        // Hoàng Sa và Trường Sa cùng màu với đất liền Việt Nam
        HOANG_SA.concat(TRUONG_SA).forEach(([la, lo]) => {
            const [x, y] = toaDo(la, lo);
            g.beginPath(); g.arc(x, y, 2.6, 0, 7); g.fill();
        });
        // Đèn các thành phố lớn, chỉ sáng rõ ở phía đêm
        g.fillStyle = "#FFF4D6";
        g.shadowColor = "#FFC46B";
        g.shadowBlur = 10;
        DEN_THANH_PHO.forEach(([la, lo, r]) => {
            const [x, y] = toaDo(la, lo);
            g.beginPath(); g.arc(x, y, r, 0, 7); g.fill();
        });
        return c;
    }

    function veAnhMay() {
        const c = document.createElement("canvas");
        c.width = 1024; c.height = 512;
        const g = c.getContext("2d");
        g.clearRect(0, 0, c.width, c.height);
        let hat = 7;
        const ngauNhien = () => { hat = (hat * 16807) % 2147483647; return hat / 2147483647; };
        for (let i = 0; i < 260; i++) {
            const x = ngauNhien() * c.width, y = (0.12 + ngauNhien() * 0.76) * c.height;
            const r = 14 + ngauNhien() * 46;
            for (const dx of [-c.width, 0, c.width]) {
                const gr = g.createRadialGradient(x + dx, y, 0, x + dx, y, r);
                gr.addColorStop(0, "rgba(255,255,255," + (0.22 + ngauNhien() * 0.28) + ")");
                gr.addColorStop(1, "rgba(255,255,255,0)");
                g.fillStyle = gr;
                g.beginPath(); g.arc(x + dx, y, r, 0, 7); g.fill();
            }
        }
        return c;
    }

    // ---------- Cảnh ----------
    const renderer = new THREE.WebGLRenderer({ canvas, antialias: true, alpha: true });
    renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, 2));
    renderer.setClearColor(0x000000, 0);

    const canh = new THREE.Scene();
    const camera = new THREE.PerspectiveCamera(32, 1, 0.1, 50);
    camera.position.set(0, 0, 5.2);

    // Ánh sáng ngày đêm: mặt trời đặt đúng vị trí theo giờ thật, nên Việt Nam sáng khi đang là ban ngày ở Việt Nam
    // và tối, lên đèn khi đang là ban đêm. Ánh sáng nền để thấp cho phía đêm tối hẳn nhưng vẫn nhìn ra hình quả cầu.
    canh.add(new THREE.AmbientLight(0x8cc4dc, 0.32));
    const den = new THREE.DirectionalLight(0xfff2dd, 2.6);
    canh.add(den);
    const denNguoc = new THREE.DirectionalLight(0x4ac8d8, 0.22);
    denNguoc.position.set(3, -1, -2);
    canh.add(denNguoc);

    // Điểm mặt trời đứng thẳng đầu: kinh độ theo giờ quốc tế, vĩ độ theo ngày trong năm (công thức gần đúng)
    function viTriMatTroi() {
        const bayGio = new Date();
        const gioUtc = bayGio.getUTCHours() + bayGio.getUTCMinutes() / 60;
        const dauNam = Date.UTC(bayGio.getUTCFullYear(), 0, 1);
        const ngayTrongNam = (bayGio.getTime() - dauNam) / 86400000;
        const viDo = -23.44 * Math.cos(2 * Math.PI * (ngayTrongNam + 10) / 365);
        const kinhDo = (12 - gioUtc) * 15;
        return { viDo, kinhDo };
    }
    const uTroi = { value: new THREE.Vector3(0, 0, 1) };

    // nghieng: nghiêng trục; quay: quay quanh kinh tuyến
    const nghieng = new THREE.Group();
    const quay = new THREE.Group();
    nghieng.add(quay);
    canh.add(nghieng);

    const anhNen = new THREE.CanvasTexture(veAnhDat());
    const anhSang = new THREE.CanvasTexture(veAnhPhatSang());
    anhNen.colorSpace = THREE.SRGBColorSpace;
    anhSang.colorSpace = THREE.SRGBColorSpace;
    anhNen.anisotropy = anhSang.anisotropy = 4;

    const vatLieuDat = new THREE.MeshStandardMaterial({
        map: anhNen, roughness: 0.55, metalness: 0.08,
        emissive: 0xffffff, emissiveMap: anhSang, emissiveIntensity: 1.0
    });
    // Phần phát sáng (Việt Nam, đèn thành phố) mờ đi ở phía ngày, sáng rõ ở phía đêm.
    // Ban ngày vẫn giữ một phần để Việt Nam và hai quần đảo luôn nhận ra được.
    vatLieuDat.onBeforeCompile = (sh) => {
        sh.uniforms.uTroi = uTroi;
        sh.fragmentShader = "uniform vec3 uTroi;\n" + sh.fragmentShader.replace(
            "#include <emissivemap_fragment>",
            "#include <emissivemap_fragment>\n" +
            "float demTroi = smoothstep(0.12, -0.28, dot(normal, uTroi));\n" +
            "totalEmissiveRadiance *= mix(0.4, 1.35, demTroi);"
        );
    };
    const traiDat = new THREE.Mesh(new THREE.SphereGeometry(1, 96, 64), vatLieuDat);
    quay.add(traiDat);

    // Hướng mặt trời trong hệ của quả cầu; mỗi khung hình đổi sang hệ thế giới vì quả cầu quay
    const troiTrongCau = new THREE.Vector3();
    const troiThuc = new THREE.Vector3();
    function capNhatMatTroi() {
        const { viDo, kinhDo } = viTriMatTroi();
        troiTrongCau.copy(diem(viDo, kinhDo, 1)).normalize();
    }
    capNhatMatTroi();
    setInterval(capNhatMatTroi, 60000);
    function datAnhSang() {
        camera.updateMatrixWorld();
        troiThuc.copy(troiTrongCau).transformDirection(quay.matrixWorld);
        den.position.copy(troiThuc).multiplyScalar(5);
        uTroi.value.copy(troiThuc).transformDirection(camera.matrixWorldInverse);
    }

    const anhMay = new THREE.CanvasTexture(veAnhMay());
    anhMay.wrapS = THREE.RepeatWrapping;
    const may = new THREE.Mesh(
        new THREE.SphereGeometry(1.018, 64, 48),
        new THREE.MeshStandardMaterial({ map: anhMay, transparent: true, opacity: 0.85, depthWrite: false, roughness: 1 })
    );
    nghieng.add(may);

    const khiQuyen = new THREE.Mesh(
        new THREE.SphereGeometry(1.11, 64, 48),
        new THREE.ShaderMaterial({
            transparent: true, side: THREE.BackSide, blending: THREE.AdditiveBlending, depthWrite: false,
            vertexShader: "varying vec3 vN; void main(){ vN = normalize(normalMatrix * normal); gl_Position = projectionMatrix * modelViewMatrix * vec4(position, 1.0); }",
            fragmentShader: "varying vec3 vN; void main(){ float i = pow(max(0.0, 0.52 - dot(vN, vec3(0.0, 0.0, 1.0))), 3.0); gl_FragColor = vec4(0.30, 0.80, 0.95, 1.0) * i * 1.35; }"
        })
    );
    canh.add(khiQuyen);

    // Nền sao nhấp nháy nhẹ
    const SO_SAO = 380;
    const viTriSao = new Float32Array(SO_SAO * 3);
    for (let i = 0; i < SO_SAO; i++) {
        const goc = Math.random() * Math.PI * 2, bk = 1.25 + Math.random() * 0.7;
        viTriSao[i * 3] = Math.cos(goc) * bk;
        viTriSao[i * 3 + 1] = Math.sin(goc) * bk;
        viTriSao[i * 3 + 2] = -0.6 - Math.random() * 1.6;
    }
    const hinhSao = new THREE.BufferGeometry();
    hinhSao.setAttribute("position", new THREE.BufferAttribute(viTriSao, 3));
    const sao = new THREE.Points(hinhSao, new THREE.PointsMaterial({ color: 0xffffff, size: 0.04, transparent: true, opacity: 0.85, depthWrite: false }));
    canh.add(sao);

    // ---------- Điểm đến ----------
    const diemDen = DIEM_DEN.map((d) => {
        const vt = diem(d.lat, d.lon, 1.006);
        const chuan = vt.clone().normalize();
        const chamNho = new THREE.Mesh(
            new THREE.SphereGeometry(0.014, 16, 12),
            new THREE.MeshBasicMaterial({ color: 0xff8a4c })
        );
        chamNho.position.copy(vt);
        const vong = new THREE.Mesh(
            new THREE.RingGeometry(0.02, 0.026, 32),
            new THREE.MeshBasicMaterial({ color: 0xffb35c, transparent: true, opacity: 0.8, side: THREE.DoubleSide, depthWrite: false })
        );
        vong.position.copy(vt);
        vong.lookAt(chuan.clone().multiplyScalar(3));
        quay.add(chamNho, vong);
        return { ...d, cham: chamNho, vong, chuan };
    });

    // ---------- Tên hai quần đảo, luôn hiện và quay theo quả cầu ----------
    // Chữ HTML bám theo vị trí đảo nên luôn nét và không bị mặt cầu cắt; chỉ ẩn khi đảo quay ra phía sau
    const dsTenDao = [["Hoàng Sa", 16.5, 112], ["Trường Sa", 10, 114]].map(([chu, lat, lon]) => {
        const moc = new THREE.Object3D();
        moc.position.copy(diem(lat, lon, 1.01));
        quay.add(moc);
        const el = document.createElement("span");
        el.className = "hero-globe-dao";
        el.textContent = chu;
        khung.appendChild(el);
        return { moc, el };
    });
    const vtDao = new THREE.Vector3();
    function capNhatTenDao() {
        dsTenDao.forEach(({ moc, el }) => {
            moc.getWorldPosition(vtDao);
            const hien = vtDao.z > 0.15;
            el.classList.toggle("hien", hien);
            if (hien) {
                const p = chieuRaManHinh(vtDao);
                el.style.transform = "translate(" + Math.round(p.x + 8) + "px," + Math.round(p.y - 9) + "px)";
            }
        });
    }

    // ---------- Máy bay bay vòng ----------
    const BK_BAY = 1.22;
    const quyDao = new THREE.Group();
    quyDao.rotation.set(58 * RAD, 0, -24 * RAD);
    canh.add(quyDao);
    const chotBay = new THREE.Group();
    quyDao.add(chotBay);

    const vatLieuBay = new THREE.MeshStandardMaterial({ color: 0xffffff, emissive: 0x9fe8f0, emissiveIntensity: 0.35, roughness: 0.4 });
    const mayBay = new THREE.Group();
    const than = new THREE.Mesh(new THREE.CylinderGeometry(0.011, 0.011, 0.085, 12), vatLieuBay);
    than.rotation.x = Math.PI / 2;
    const muiBay = new THREE.Mesh(new THREE.ConeGeometry(0.011, 0.03, 12), vatLieuBay);
    muiBay.rotation.x = -Math.PI / 2;
    muiBay.position.z = -0.0575;
    const canhBay = new THREE.Mesh(new THREE.BoxGeometry(0.115, 0.0025, 0.028), vatLieuBay);
    canhBay.position.z = -0.004;
    const duoiNgang = new THREE.Mesh(new THREE.BoxGeometry(0.045, 0.0025, 0.016), vatLieuBay);
    duoiNgang.position.z = 0.04;
    const duoiDung = new THREE.Mesh(new THREE.BoxGeometry(0.0025, 0.026, 0.022), vatLieuBay);
    duoiDung.position.set(0, 0.013, 0.038);
    mayBay.add(than, muiBay, canhBay, duoiNgang, duoiDung);
    mayBay.scale.setScalar(2.4);
    // Mũi hướng theo -z, lưng hướng ra ngoài mặt cầu (+x)
    const bocBay = new THREE.Group();
    bocBay.rotation.z = -Math.PI / 2;
    bocBay.add(mayBay);
    bocBay.position.set(BK_BAY, 0, 0);
    chotBay.add(bocBay);

    // Vệt sáng phía sau máy bay
    const SO_DIEM_VET = 90;
    const viTriVet = new Float32Array(SO_DIEM_VET * 3);
    const mauVet = new Float32Array(SO_DIEM_VET * 3);
    for (let i = 0; i < SO_DIEM_VET; i++) {
        const k = 1 - i / SO_DIEM_VET;
        mauVet[i * 3] = 0.55 * k * k; mauVet[i * 3 + 1] = 0.95 * k * k; mauVet[i * 3 + 2] = 1.0 * k * k;
    }
    const hinhVet = new THREE.BufferGeometry();
    hinhVet.setAttribute("position", new THREE.BufferAttribute(viTriVet, 3));
    hinhVet.setAttribute("color", new THREE.BufferAttribute(mauVet, 3));
    const vet = new THREE.Line(hinhVet, new THREE.LineBasicMaterial({ vertexColors: true, transparent: true, blending: THREE.AdditiveBlending, depthWrite: false }));
    vet.frustumCulled = false;
    quyDao.add(vet);

    // ---------- Điều khiển ----------
    const KINH_VN = 106;
    let lechKinh = 0;      // do người dùng kéo
    let lechVi = 14 * RAD; // độ nghiêng
    let dangKeo = false, keoX = 0, keoY = 0, daKeo = 0;
    let diDangChon = -1, diHover = -1;

    canvas.addEventListener("pointerdown", (e) => {
        dangKeo = true; keoX = e.clientX; keoY = e.clientY; daKeo = 0;
        canvas.setPointerCapture(e.pointerId);
    });
    canvas.addEventListener("pointermove", (e) => {
        if (dangKeo) {
            lechKinh += (e.clientX - keoX) * 0.008;
            lechVi = Math.max(-0.6, Math.min(0.9, lechVi + (e.clientY - keoY) * 0.005));
            daKeo += Math.abs(e.clientX - keoX) + Math.abs(e.clientY - keoY);
            keoX = e.clientX; keoY = e.clientY;
        } else {
            diHover = diemGanChuot(e);
        }
        if (giamChuyenDong) ve(0);
    });
    const thaKeo = () => { dangKeo = false; };
    canvas.addEventListener("pointerup", thaKeo);
    canvas.addEventListener("pointercancel", thaKeo);
    canvas.addEventListener("pointerleave", () => { diHover = -1; });

    const vtTam = new THREE.Vector3();
    function chieuRaManHinh(v) {
        vtTam.copy(v).project(camera);
        const hc = canvas.getBoundingClientRect();
        return { x: (vtTam.x * 0.5 + 0.5) * hc.width, y: (-vtTam.y * 0.5 + 0.5) * hc.height };
    }
    const vtThe = new THREE.Vector3();
    function huongVeCamera(d) {
        d.cham.getWorldPosition(vtThe);
        return vtThe.z; // lớn hơn 0.15 nghĩa là điểm quay về phía người xem
    }
    function diemGanChuot(e) {
        const hc = canvas.getBoundingClientRect();
        const mx = e.clientX - hc.left, my = e.clientY - hc.top;
        let tot = -1, kc = 28;
        diemDen.forEach((d, i) => {
            if (huongVeCamera(d) < 0.15) return;
            d.cham.getWorldPosition(vtThe);
            const p = chieuRaManHinh(vtThe);
            const kcd = Math.hypot(p.x - mx, p.y - my);
            if (kcd < kc) { kc = kcd; tot = i; }
        });
        return tot;
    }

    // ---------- Kích thước ----------
    function doiCo() {
        const w = khung.clientWidth, h = khung.clientHeight;
        if (!w || !h) return;
        renderer.setSize(w, h, false);
        camera.aspect = w / h;
        camera.updateProjectionMatrix();
    }
    new ResizeObserver(() => { doiCo(); if (giamChuyenDong) ve(0); }).observe(khung);

    // ---------- Vẽ mỗi khung hình ----------
    let tChon = 0;
    function capNhatNhan(t) {
        capNhatTenDao();
        // Tự chuyển điểm đến mỗi 2,6 giây nếu không có điểm nào đang được trỏ tới
        if (diHover < 0 && !dangKeo) {
            // Bỏ qua điểm đang ở mặt sau quả cầu (ví dụ tour nước ngoài xa Việt Nam) để nhãn không bị trống
            const n = diemDen.length;
            let idx = Math.floor(t / 2600) % n;
            for (let k = 0; k < n && huongVeCamera(diemDen[idx]) < 0.2; k++) idx = (idx + 1) % n;
            if (idx !== tChon) tChon = idx;
        }
        let i = diHover >= 0 ? diHover : tChon;
        const d = diemDen[i];
        if (!d || huongVeCamera(d) < 0.2) { nhan.classList.remove("hien"); diDangChon = -1; return; }
        d.cham.getWorldPosition(vtThe);
        const p = chieuRaManHinh(vtThe);
        // Nhãn nằm bên trái điểm, để không che vùng biển phía đông có Hoàng Sa và Trường Sa
        nhan.style.transform = "translate(" + Math.round(p.x - 12) + "px," + Math.round(p.y - 14) + "px) translateX(-100%)";
        if (diDangChon !== i) {
            diDangChon = i;
            nhan.textContent = d.ten;
            nhan.href = d.link || duongDanTour + (duongDanTour.indexOf("?") >= 0 ? "&" : "?") + "keyword=" + encodeURIComponent(d.ten);
        }
        nhan.classList.add("hien");
    }

    let gocBay = 0;
    function ve(t) {
        const giay = t / 1000;
        // Lắc nhẹ quanh Việt Nam, người dùng kéo thì lệch theo
        const sway = giamChuyenDong ? 0 : Math.sin(giay * 0.22) * 38 * RAD;
        quay.rotation.y = (-90 - KINH_VN) * RAD + lechKinh + sway;
        nghieng.rotation.x = lechVi;
        may.rotation.y = giay * 0.015;

        diemDen.forEach((d, i) => {
            const phase = (giay * 0.9 + i * 0.7) % 1;
            d.vong.scale.setScalar(1 + phase * 1.8);
            d.vong.material.opacity = (1 - phase) * 0.8;
        });

        // Máy bay và vệt sáng
        gocBay = giay * 0.55;
        chotBay.rotation.y = gocBay;
        for (let i = 0; i < SO_DIEM_VET; i++) {
            const g = gocBay - i * 0.018;
            viTriVet[i * 3] = Math.cos(g) * BK_BAY;
            viTriVet[i * 3 + 1] = 0;
            viTriVet[i * 3 + 2] = -Math.sin(g) * BK_BAY;
        }
        hinhVet.attributes.position.needsUpdate = true;

        sao.material.opacity = 0.7 + Math.sin(giay * 1.6) * 0.15;
        capNhatNhan(t);
        canh.updateMatrixWorld();
        datAnhSang();
        renderer.render(canh, camera);
    }

    // ---------- Vòng lặp (chỉ chạy khi nhìn thấy) ----------
    let hienTrenMan = true, dangChay = false;
    function vong(t) {
        dangChay = false;
        if (!hienTrenMan || document.hidden) return;
        ve(t);
        lenLich();
    }
    function lenLich() {
        if (dangChay || giamChuyenDong) return;
        dangChay = true;
        requestAnimationFrame(vong);
    }
    if ("IntersectionObserver" in window) {
        new IntersectionObserver((ds) => { hienTrenMan = ds[0].isIntersecting; if (hienTrenMan) lenLich(); }).observe(khung);
    }
    document.addEventListener("visibilitychange", lenLich);

    doiCo();
    ve(0);
    khung.classList.add("san-sang");
    lenLich();
}
