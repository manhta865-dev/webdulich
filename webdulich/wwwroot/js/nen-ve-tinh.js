// Nền ảnh vệ tinh và nút chuyển "Bản đồ | Vệ tinh" dùng chung cho các bản đồ MapLibre (trang tour, Bản đồ tour).
// Ảnh vệ tinh Esri World Imagery, gọi thẳng từ trình duyệt, không cần khóa. Dòng ghi nguồn lấy đúng theo dịch vụ
// (trường copyrightText của World_Imagery/MapServer) và chỉ hiện khi đang bật vệ tinh.
// Lớp ảnh nằm trên nền bản đồ gốc nhưng dưới đường đi và điểm dừng của TravelVN; địa hình 3D vẫn giữ.
// Ảnh không tải được thì tự quay về nền bản đồ. Lựa chọn nhớ theo từng trình duyệt (tiện ích, mất cũng không sao).
const ANH_VE_TINH = "https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}";
const NGUON_VE_TINH = "Ảnh vệ tinh: Esri, Vantor, Earthstar Geographics, and the GIS User Community";
const KHOA_NHO = "tv-nen-ban-do";
const SO_LOI_TOI_DA = 6;

function docLuaChon() {
    try {
        return localStorage.getItem(KHOA_NHO) === "veTinh";
    } catch (e) {
        return false;
    }
}

function ghiLuaChon(veTinh) {
    try {
        localStorage.setItem(KHOA_NHO, veTinh ? "veTinh" : "banDo");
    } catch (e) {
        // Trình duyệt chặn lưu: chỉ là không nhớ lựa chọn
    }
}

// Gọi sau khi bản đồ đã "load". truocLop: id lớp mà ảnh vệ tinh phải nằm dưới (đường đi của TravelVN);
// bỏ trống thì ảnh nằm trên cùng các lớp hiện có, các lớp thêm sau vẫn nằm trên ảnh.
// viTri: góc đặt nút (mặc định trên trái), chọn góc không che điểm quan trọng của từng bản đồ.
export function themNenVeTinh(map, { truocLop, viTri = "top-left" } = {}) {
    map.addSource("veTinh", { type: "raster", tiles: [ANH_VE_TINH], tileSize: 256, maxzoom: 19, attribution: NGUON_VE_TINH });
    map.addLayer(
        { id: "veTinh", type: "raster", source: "veTinh", layout: { visibility: "none" }, paint: { "raster-fade-duration": 200 } },
        truocLop && map.getLayer(truocLop) ? truocLop : undefined);

    const hop = document.createElement("div");
    hop.className = "maplibregl-ctrl maplibregl-ctrl-group tv-nen";
    hop.setAttribute("role", "group");
    hop.setAttribute("aria-label", "Kiểu nền bản đồ");
    const nutBanDo = taoNut("Bản đồ");
    const nutVeTinh = taoNut("Vệ tinh");
    hop.append(nutBanDo, nutVeTinh);

    let soLoi = 0;
    let hong = false;

    function chon(veTinh, nho) {
        if (hong) veTinh = false;
        map.setLayoutProperty("veTinh", "visibility", veTinh ? "visible" : "none");
        nutBanDo.setAttribute("aria-pressed", String(!veTinh));
        nutVeTinh.setAttribute("aria-pressed", String(veTinh));
        if (nho) ghiLuaChon(veTinh);
    }

    nutBanDo.addEventListener("click", () => chon(false, true));
    nutVeTinh.addEventListener("click", () => chon(true, true));

    // Dịch vụ ảnh lỗi liên tục (mạng chặn, dịch vụ ngừng): về nền bản đồ, khóa nút và nói lý do
    map.on("error", (e) => {
        if (e.sourceId !== "veTinh" || hong) return;
        if (++soLoi >= SO_LOI_TOI_DA) {
            hong = true;
            chon(false, false);
            nutVeTinh.disabled = true;
            nutVeTinh.title = "Chưa tải được ảnh vệ tinh, đang dùng nền bản đồ";
        }
    });

    map.addControl({ onAdd: () => hop, onRemove: () => hop.remove() }, viTri);
    chon(docLuaChon(), false);

    // Dòng nguồn dài thêm phần ảnh vệ tinh: thu gọn thành nút "i" (như trang tour) để không che bản đồ trên điện thoại,
    // bấm vào vẫn xem đủ nguồn
    map.getContainer().querySelector(".maplibregl-ctrl-attrib")?.classList.remove("maplibregl-compact-show");
}

function taoNut(chu) {
    const nut = document.createElement("button");
    nut.type = "button";
    nut.textContent = chu;
    return nut;
}
