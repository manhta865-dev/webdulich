// Đầu trang chủ: rê chuột lên ảnh nền thì mặt nước gợn như vừa chạm tay (Views/TrangChu/Index.cshtml, khối .tc-dau-nen).
// Cách làm: vẽ lại ảnh lên canvas bằng WebGL; mỗi lần chuột di qua thả một "giọt" (tối đa 16 giọt gần nhất),
// shader cộng các vòng sóng lan ra và tắt dần rồi xô lệch ảnh theo sóng. Không giọt nào còn sóng thì ngừng vẽ.
// Chỉ chạy trên máy có chuột, không bật giảm chuyển động, có WebGL; còn lại giữ ảnh tĩnh (thẻ <img>).
(function () {
    "use strict";
    var dau = document.getElementById("tcDau");
    if (!dau) return;
    var nen = dau.querySelector(".tc-dau-nen");
    var anh = nen && nen.querySelector("img");
    var canvas = nen && nen.querySelector("canvas");
    if (!anh || !canvas) return;

    var coChuot = window.matchMedia && window.matchMedia("(hover: hover) and (pointer: fine)").matches;
    var giamChuyenDong = window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    if (!coChuot || giamChuyenDong) return;

    var gl = canvas.getContext("webgl", { alpha: false, antialias: false, premultipliedAlpha: false, preserveDrawingBuffer: false });
    if (!gl) return;

    var SO_GIOT = 16;
    var DOI_GIOT = 2.6; // giây một giọt còn sóng

    var vs = "attribute vec2 p; varying vec2 uv; void main(){ uv = p * 0.5 + 0.5; uv.y = 1.0 - uv.y; gl_Position = vec4(p, 0.0, 1.0); }";
    var fs = [
        "precision mediump float;",
        "varying vec2 uv;",
        "uniform sampler2D anh;",
        "uniform vec2 tiLeKhung;",   // khung canvas: rộng / cao
        "uniform vec2 phuKin;",       // co ảnh để phủ kín khung như object-fit: cover
        "uniform float bayGio;",
        "uniform vec3 giot[" + SO_GIOT + "];", // x, y (0..1 theo khung), lúc thả (giây)
        "void main(){",
        "  vec2 lech = vec2(0.0);",
        "  float sang = 0.0;",
        "  for (int i = 0; i < " + SO_GIOT + "; i++) {",
        "    vec3 g = giot[i];",
        "    float tuoi = bayGio - g.z;",
        "    if (g.z < 0.0 || tuoi < 0.0 || tuoi > " + DOI_GIOT.toFixed(1) + ") continue;",
        "    vec2 d = (uv - g.xy) * vec2(tiLeKhung.x / tiLeKhung.y, 1.0);",
        "    float r = length(d);",
        "    float mat = tuoi * 0.42;",                          // mép sóng lan ra theo thời gian
        "    float vung = (1.0 - smoothstep(mat - 0.16, mat + 0.02, r)) * smoothstep(0.0, 0.03, r);",
        "    float song = sin(r * 58.0 - tuoi * 13.0) * vung;",
        "    float tat = exp(-tuoi * 1.25) * exp(-r * 2.6);",
        "    vec2 huong = r > 0.0001 ? d / r : vec2(0.0);",
        "    lech += huong * song * tat * 0.026;",
        "    sang += song * tat * 0.16;",
        "  }",
        "  vec2 t = (uv - 0.5) * phuKin + 0.5 + lech;",
        "  vec3 mau = texture2D(anh, clamp(t, 0.001, 0.999)).rgb;",
        "  gl_FragColor = vec4(mau + sang, 1.0);",
        "}"
    ].join("\n");

    function bienDich(loai, ma) {
        var s = gl.createShader(loai);
        gl.shaderSource(s, ma);
        gl.compileShader(s);
        return gl.getShaderParameter(s, gl.COMPILE_STATUS) ? s : null;
    }
    var v = bienDich(gl.VERTEX_SHADER, vs), f = bienDich(gl.FRAGMENT_SHADER, fs);
    if (!v || !f) return;
    var ct = gl.createProgram();
    gl.attachShader(ct, v);
    gl.attachShader(ct, f);
    gl.linkProgram(ct);
    if (!gl.getProgramParameter(ct, gl.LINK_STATUS)) return;
    gl.useProgram(ct);

    var bo = gl.createBuffer();
    gl.bindBuffer(gl.ARRAY_BUFFER, bo);
    gl.bufferData(gl.ARRAY_BUFFER, new Float32Array([-1, -1, 1, -1, -1, 1, 1, 1]), gl.STATIC_DRAW);
    var viTri = gl.getAttribLocation(ct, "p");
    gl.enableVertexAttribArray(viTri);
    gl.vertexAttribPointer(viTri, 2, gl.FLOAT, false, 0, 0);

    var uTiLe = gl.getUniformLocation(ct, "tiLeKhung");
    var uPhu = gl.getUniformLocation(ct, "phuKin");
    var uGio = gl.getUniformLocation(ct, "bayGio");
    var uGiot = gl.getUniformLocation(ct, "giot");

    var dsGiot = new Float32Array(SO_GIOT * 3);
    for (var i = 0; i < SO_GIOT; i++) dsGiot[i * 3 + 2] = -1;
    var giotTiep = 0;
    var batDau = performance.now();
    var dangVe = false;
    var giotCuoi = -10;
    var trongKhung = true;

    function giay() { return (performance.now() - batDau) / 1000; }

    function doKhung() {
        var heSo = Math.min(window.devicePixelRatio || 1, 1.25);
        var w = Math.max(1, Math.round(nen.clientWidth * heSo));
        var h = Math.max(1, Math.round(nen.clientHeight * heSo));
        if (canvas.width !== w || canvas.height !== h) {
            canvas.width = w;
            canvas.height = h;
        }
        gl.viewport(0, 0, w, h);
        var tlKhung = w / h, tlAnh = anh.naturalWidth / anh.naturalHeight;
        // Giống object-fit: cover: chiều nào dư thì cắt bớt
        var phu = tlKhung > tlAnh ? [1, tlAnh / tlKhung] : [tlKhung / tlAnh, 1];
        gl.uniform2f(uTiLe, w, h);
        gl.uniform2f(uPhu, phu[0], phu[1]);
    }

    function ve() {
        var t = giay();
        gl.uniform1f(uGio, t);
        gl.uniform3fv(uGiot, dsGiot);
        gl.drawArrays(gl.TRIANGLE_STRIP, 0, 4);
        if (trongKhung && t - giotCuoi < DOI_GIOT) {
            requestAnimationFrame(ve);
        } else {
            dangVe = false;
        }
    }

    function batVe() {
        if (!dangVe) {
            dangVe = true;
            requestAnimationFrame(ve);
        }
    }

    function napAnh() {
        var tex = gl.createTexture();
        gl.bindTexture(gl.TEXTURE_2D, tex);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.CLAMP_TO_EDGE);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.CLAMP_TO_EDGE);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.LINEAR);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.LINEAR);
        gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGB, gl.RGB, gl.UNSIGNED_BYTE, anh);
        doKhung();
        gl.uniform3fv(uGiot, dsGiot);
        gl.uniform1f(uGio, 0);
        gl.drawArrays(gl.TRIANGLE_STRIP, 0, 4);
        nen.classList.add("co-nuoc");

        var lanTha = 0;
        dau.addEventListener("pointermove", function (e) {
            if (e.pointerType && e.pointerType !== "mouse") return;
            var bayGio = performance.now();
            if (bayGio - lanTha < 55) return; // khoảng 18 giọt mỗi giây là đủ mượt
            lanTha = bayGio;
            var r = nen.getBoundingClientRect();
            var x = (e.clientX - r.left) / r.width, y = (e.clientY - r.top) / r.height;
            if (x < 0 || x > 1 || y < 0 || y > 1) return;
            var t = giay();
            dsGiot[giotTiep * 3] = x;
            dsGiot[giotTiep * 3 + 1] = y;
            dsGiot[giotTiep * 3 + 2] = t;
            giotTiep = (giotTiep + 1) % SO_GIOT;
            giotCuoi = t;
            batVe();
        });

        var henDoi = 0;
        window.addEventListener("resize", function () {
            clearTimeout(henDoi);
            henDoi = setTimeout(function () { doKhung(); batVe(); }, 120);
        });

        // Đầu trang cuộn khỏi màn hình thì không vẽ nữa
        if ("IntersectionObserver" in window) {
            new IntersectionObserver(function (ds) {
                trongKhung = ds[0].isIntersecting;
                if (trongKhung) batVe();
            }).observe(dau);
        }
    }

    if (anh.complete && anh.naturalWidth) {
        napAnh();
    } else {
        anh.addEventListener("load", napAnh, { once: true });
    }
})();
