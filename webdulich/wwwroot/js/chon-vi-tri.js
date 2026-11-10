// Bản đồ chọn vị trí địa điểm ở form thêm/sửa tour (Admin/_ChonViTri, RB-44).
// Gõ tên (form thêm) hoặc chọn địa điểm (form sửa): nhảy tới vị trí đã lưu của địa điểm, chưa có thì tới vị trí gợi ý theo tên.
// Bấm vào bản đồ để đặt lại (trong nước hay nước ngoài đều được). Nút "Bỏ vị trí" để địa điểm không có chấm.
// Không tải được bản đồ thì form vẫn gửi được, vị trí giữ như cũ.
const KIEU_BAN_DO = "https://tiles.openfreemap.org/styles/liberty";
const khung = document.getElementById("chonViTri");

if (khung) batDau();

function boDau(chu) {
    return (chu || "").replace(/Đ/g, "D").replace(/đ/g, "d").normalize("NFD").replace(/[̀-ͯ]/g, "")
        .toLowerCase().trim();
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
    const oVi = document.getElementById("ViDoDiaDiem");
    const oKinh = document.getElementById("KinhDoDiaDiem");
    const trangThai = document.getElementById("viTriTrangThai");
    const goiY = JSON.parse(khung.dataset.goiY || "{}");
    const dsDiaDiem = JSON.parse(khung.dataset.diaDiem || "[]");
    const oTen = document.getElementById("DiaDiem");     // form thêm: ô nhập tên
    const oChon = document.getElementById("DiaDiemId");  // form sửa: ô chọn địa điểm
    let daTuChon = false; // admin đã bấm tay thì không tự nhảy theo tên nữa

    function ghi(vi, kinh, nguon) {
        oVi.value = vi == null ? "" : (+vi).toFixed(6);
        oKinh.value = kinh == null ? "" : (+kinh).toFixed(6);
        trangThai.textContent = vi == null ? "Chưa chọn vị trí" : nguon;
        trangThai.classList.toggle("co", vi != null);
    }

    // Vị trí theo tên: địa điểm đã lưu trước, rồi đến bảng gợi ý (khớp đúng tên, rồi tên có chứa)
    function timTheoTen(ten) {
        const k = boDau(ten);
        if (!k) return null;
        const daLuu = dsDiaDiem.find((d) => boDau(d.ten) === k && d.vi != null);
        if (daLuu) return { vi: daLuu.vi, kinh: daLuu.kinh, nguon: "Vị trí đã lưu của địa điểm" };
        if (goiY[k]) return { vi: goiY[k][0], kinh: goiY[k][1], nguon: "Vị trí gợi ý theo tên, bấm bản đồ để chỉnh" };
        // "Du lịch Đà Lạt" khớp "da lat": so nguyên từ, bỏ tên quá ngắn ("y", "my") để không khớp nhầm giữa chữ
        const khop = Object.keys(goiY).filter((x) => x.length >= 4 && (" " + k + " ").includes(" " + x + " "))
            .sort((a, b) => b.length - a.length)[0];
        return khop ? { vi: goiY[khop][0], kinh: goiY[khop][1], nguon: "Vị trí gợi ý theo tên, bấm bản đồ để chỉnh" } : null;
    }

    // Báo cho máy chủ biết ô vị trí do script quản lý, nên ô rỗng nghĩa là "không đặt trên bản đồ"
    const coJs = document.createElement("input");
    coJs.type = "hidden";
    coJs.name = "viTriCoJs";
    coJs.value = "1";
    khung.appendChild(coJs);

    if (oVi.value) {
        trangThai.textContent = "Vị trí đã lưu";
        trangThai.classList.add("co");
    }

    let map = null, ghim = null;
    function datGhim(vi, kinh, bay) {
        if (!map) return;
        if (vi == null) { if (ghim) { ghim.remove(); ghim = null; } return; }
        if (!ghim) ghim = new maplibregl.Marker({ color: "#E4572E" }).setLngLat([kinh, vi]).addTo(map);
        else ghim.setLngLat([kinh, vi]);
        if (bay) map.flyTo({ center: [kinh, vi], zoom: Math.max(map.getZoom(), 7) });
    }

    function theoTen(ten) {
        if (daTuChon) return;
        const vt = timTheoTen(ten);
        if (vt) { ghi(vt.vi, vt.kinh, vt.nguon); datGhim(vt.vi, vt.kinh, true); }
    }

    oTen?.addEventListener("change", () => theoTen(oTen.value));
    oTen?.addEventListener("blur", () => theoTen(oTen.value));
    oChon?.addEventListener("change", () => {
        daTuChon = false;
        const d = dsDiaDiem.find((x) => String(x.id) === oChon.value);
        if (d && d.vi != null) { ghi(d.vi, d.kinh, "Vị trí đã lưu của địa điểm"); datGhim(d.vi, d.kinh, true); }
        else if (d) { ghi(null, null); datGhim(null); theoTen(d.ten); }
    });
    document.getElementById("viTriBo").addEventListener("click", () => {
        daTuChon = true;
        ghi(null, null);
        datGhim(null);
    });

    // Form thêm mở lại sau khi báo lỗi: đã có tên mà chưa có vị trí thì gợi ý luôn
    if (!oVi.value && oTen?.value) theoTen(oTen.value);
    // Form sửa: địa điểm chưa có vị trí thì gợi ý theo tên địa điểm đang chọn
    if (!oVi.value && oChon?.value) {
        const d = dsDiaDiem.find((x) => String(x.id) === oChon.value);
        if (d) theoTen(d.ten);
    }

    try {
        if (!document.createElement("canvas").getContext("webgl2")) throw new Error("Không có WebGL2");
        await taiThuVien();
        const phanHoi = await fetch(KIEU_BAN_DO, { signal: AbortSignal.timeout(8000) });
        if (!phanHoi.ok) throw new Error("Dịch vụ bản đồ báo lỗi");
        const kieu = await phanHoi.json();
        // Giữ tên địa danh của nền để admin dễ tìm chỗ, chỉ bỏ đường ranh giới (cùng cách với ban-do-tour.js)
        kieu.layers = kieu.layers.filter((l) => !/boundary/.test(l.id));

        const vbd = document.getElementById("viTriBanDo");
        vbd.innerHTML = "";
        // Mở ở Việt Nam, thu nhỏ ra được cả thế giới để chọn điểm đến nước ngoài
        map = new maplibregl.Map({
            container: vbd,
            style: kieu,
            bounds: [[101.8, 8.0], [116.2, 23.6]],
            fitBoundsOptions: { padding: 16 },
            minZoom: 1,
            renderWorldCopies: false,
            dragRotate: false,
            attributionControl: { compact: true }
        });
        map.addControl(new maplibregl.NavigationControl({ showCompass: false }), "top-right");
        map.getCanvas().style.cursor = "crosshair";
        if (oVi.value) datGhim(+oVi.value, +oKinh.value, false);
        map.once("load", () => { if (oVi.value) map.jumpTo({ center: [+oKinh.value, +oVi.value], zoom: 7 }); });

        map.on("click", (e) => {
            const { lat, lng } = e.lngLat;
            daTuChon = true;
            ghi(lat, lng, "Đã chọn trên bản đồ");
            datGhim(lat, lng, false);
        });
    } catch {
        document.getElementById("viTriBanDo").innerHTML =
            '<p class="ad-vt-cho">Chưa tải được bản đồ. Vị trí vẫn được gợi ý theo tên địa điểm khi bạn lưu.</p>';
    }
}
