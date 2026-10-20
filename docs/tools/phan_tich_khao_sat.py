# Phân tích kết quả khảo sát "đi du lịch theo nhóm" theo các ngưỡng trong docs/KHAO_SAT.md.
# Cách chạy: py -3 -I -X utf8 phan_tich.py duong_dan_file.csv
# Đầu vào: file CSV xuất từ Google Form (hàng 1 là tiêu đề câu hỏi). Không ghi gì ra ngoài, chỉ in báo cáo.
import csv
import io
import re
import sys
from collections import Counter

NGUONG = {
    "co_nhu_cau": (0.60, 0.30),   # tỉ lệ từng đi nhóm từ 4 người: ủng hộ từ 60%, không ủng hộ dưới 30%
    "kho_khan_2": 0.50,           # từ 50% chọn ít nhất 2 khó khăn ở câu 8
    "phien": (3.5, 2.5),          # điểm phiền TB: ủng hộ từ 3,5, không ủng hộ dưới 2,5
    "muon_dung": (0.50, 0.30),    # tỉ lệ chọn 4 hoặc 5 ở câu 13
}

TUY_CHON = {
    4: ["Gia đình/họ hàng", "Bạn bè", "Đồng nghiệp/công ty", "Lớp/CLB/hội nhóm", "Khác"],
    7: ["Zalo", "Messenger", "Điện thoại/SMS", "Google Form/Sheet", "Nhắn trực tiếp", "Khác"],
    8: ["Phải nhắn từng người để xin thông tin/giấy tờ", "Thông tin gửi thiếu hoặc sai",
        "Người ứng tiền phải đi đòi lại", "Không biết ai đã trả, ai chưa",
        "Có người trả trễ hoặc bỏ nhóm giữa chừng", "Không có hạn chốt rõ ràng",
        "Ngại gửi CCCD/hộ chiếu qua tin nhắn nhóm", "Không gặp khó khăn"],
    14: ["Mỗi người tự trả phần của mình", "Link mời, nhận ghế không cần tạo tài khoản phức tạp",
         "Xem tiến độ ai đã trả/chưa trả", "Nhắc tự động người chưa trả",
         "Hạn giữ chỗ rõ ràng, quá hạn tự nhả ghế", "Giấy tờ chỉ mình và hệ thống thấy, trưởng nhóm không thấy"],
}


def doc_csv(duong_dan):
    with io.open(duong_dan, encoding="utf-8-sig", newline="") as f:
        hang = list(csv.reader(f))
    tieu_de, du_lieu = hang[0], [h for h in hang[1:] if any(c.strip() for c in h)]
    # cột theo số câu: tìm cột có tiêu đề bắt đầu bằng "N."
    cot = {}
    for i, t in enumerate(tieu_de):
        m = re.match(r"\s*(\d+)\.", t)
        if m:
            cot[int(m.group(1))] = i
    return tieu_de, du_lieu, cot


def lay(h, cot, q):
    i = cot.get(q)
    return h[i].strip() if i is not None and i < len(h) else ""


def tach_nhieu(gia_tri, ds):
    """Câu nhiều lựa chọn: tìm từng lựa chọn đã biết trong ô (vì lựa chọn có dấu phẩy nên không tách theo dấu phẩy)."""
    dung = [x for x in ds if x in gia_tri]
    # lựa chọn "Khác" có thể là chữ người dùng tự nhập: coi như "Khác" nếu còn phần dư
    return dung


def ti_le(n, tong):
    return f"{n}/{tong} ({n / tong * 100:.1f}%)" if tong else "0/0"


def phan_bo(ds_gia_tri, tong=None):
    c = Counter(g for g in ds_gia_tri if g != "")
    tong = tong if tong is not None else sum(c.values())
    return "\n".join(f"- {k}: {ti_le(v, tong)}" for k, v in c.most_common())


def dem_nhieu(ds_cau_tra_loi, q, tong):
    c = Counter()
    for gt in ds_cau_tra_loi:
        for x in tach_nhieu(gt, TUY_CHON[q]):
            c[x] += 1
    return "\n".join(f"- {k}: {ti_le(c.get(k, 0), tong)}" for k in sorted(TUY_CHON[q], key=lambda k: -c.get(k, 0)))


def ket_luan(gia_tri, ung_ho, khong):
    return "ủng hộ giả thuyết" if gia_tri >= ung_ho else ("không ủng hộ" if gia_tri < khong else "nằm giữa hai ngưỡng")


def main(duong_dan):
    tieu_de, du_lieu, cot = doc_csv(duong_dan)
    n = len(du_lieu)
    print(f"# Báo cáo phân tích khảo sát\n\nSố phiếu đọc được: **{n}**. Số cột câu hỏi nhận diện: {len(cot)}/19.\n")
    thieu = [q for q in range(1, 20) if q not in cot]
    if thieu:
        print(f"CẢNH BÁO: không tìm thấy cột cho câu {thieu}\n")

    # --- kiểm tra chất lượng phiếu
    thoi_gian = [h[0].strip() for h in du_lieu]
    trung = [k for k, v in Counter(thoi_gian).items() if v > 1]
    bat_buoc = [q for q in range(1, 19)]
    thieu_tra_loi = [i + 2 for i, h in enumerate(du_lieu) if any(lay(h, cot, q) == "" for q in bat_buoc)]
    nghi_van = []
    for i, h in enumerate(du_lieu):
        gop_y = lay(h, cot, 19).lower()
        if gop_y in {"hoho", "haha", "test", "abc", "asd", "..."}:
            nghi_van.append((i + 2, gop_y))
    print("## 1. Chất lượng phiếu\n")
    print(f"- Dấu thời gian trùng nhau: {len(trung)}")
    print(f"- Phiếu thiếu câu bắt buộc (hàng trong bảng): {thieu_tra_loi or 'không có'}")
    print(f"- Góp ý có vẻ là chữ thử: {nghi_van or 'không có'} (chỉ ảnh hưởng câu 19, không loại phiếu)")
    print(f"- Khoảng thời gian thu: {thoi_gian[0]} đến {thoi_gian[-1]}")
    hop_le = [h for i, h in enumerate(du_lieu) if (i + 2) not in thieu_tra_loi]
    print(f"- **Phiếu hợp lệ: {len(hop_le)}** (mục tiêu 30–50)\n")
    du_lieu = hop_le
    n = len(du_lieu)

    # --- mô tả mẫu
    print("## 2. Mô tả mẫu\n")
    print("**Câu 1. Nhóm tuổi**\n" + phan_bo([lay(h, cot, 1) for h in du_lieu], n))
    print("\n**Câu 4. Kiểu nhóm thường đi (chọn nhiều)**\n" + dem_nhieu([lay(h, cot, 4) for h in du_lieu], 4, n))
    print("\n**Câu 18. Từng đặt tour qua web/app**\n" + phan_bo([lay(h, cot, 18) for h in du_lieu], n))

    # --- nhóm 1: có nhu cầu đi nhóm
    co = [h for h in du_lieu if lay(h, cot, 2).startswith("Có")]
    khong = n - len(co)
    t1 = len(co) / n if n else 0
    print("\n## 3. Có nhu cầu đi nhóm (câu 2, 3, 5)\n")
    print(f"- Từng đi nhóm từ 4 người trong 12 tháng (câu 2 là \"Có\"): **{ti_le(len(co), n)}**, ngưỡng ủng hộ từ 60%, dưới 30% là không ủng hộ → {ket_luan(t1, *NGUONG['co_nhu_cau'])}")
    print("\n**Câu 3. Số lần đi nhóm một năm (toàn mẫu)**\n" + phan_bo([lay(h, cot, 3) for h in du_lieu], n))
    print("\n**Câu 5. Quy mô nhóm gần nhất (toàn mẫu)**\n" + phan_bo([lay(h, cot, 5) for h in du_lieu], n))

    # --- nhóm 2: vướng mắc thật (chỉ người trả lời Có ở câu 2)
    m = len(co)
    print(f"\n## 4. Có vướng mắc thật (câu 6 đến 12), chỉ tính {m} người trả lời \"Có\" ở câu 2\n")
    print("**Câu 6. Ai đứng ra đặt và ứng tiền**\n" + phan_bo([lay(h, cot, 6) for h in co], m))
    co_ung_tien = sum(1 for h in co if lay(h, cot, 6) in ("Mình", "Người khác trong nhóm"))
    print(f"\n- Có người ứng tiền trước (câu 6 là \"Mình\" hoặc \"Người khác trong nhóm\"): {ti_le(co_ung_tien, m)}")
    print("\n**Câu 7. Kênh trao đổi, thu thông tin (chọn nhiều)**\n" + dem_nhieu([lay(h, cot, 7) for h in co], 7, m))
    print("\n**Câu 8. Khó khăn gặp phải (chọn nhiều)**\n" + dem_nhieu([lay(h, cot, 8) for h in co], 8, m))
    so_kk = [len([x for x in tach_nhieu(lay(h, cot, 8), TUY_CHON[8]) if x != "Không gặp khó khăn"]) for h in co]
    tu2 = sum(1 for k in so_kk if k >= 2)
    t2 = tu2 / m if m else 0
    print(f"\n- Chọn ít nhất 2 khó khăn ở câu 8: **{ti_le(tu2, m)}**, ngưỡng ủng hộ từ 50% → {ket_luan(t2, NGUONG['kho_khan_2'], 0.0)}")
    print(f"- Chọn \"Không gặp khó khăn\": {ti_le(sum(1 for h in co if 'Không gặp khó khăn' in lay(h, cot, 8)), m)}")
    print("\n**Câu 9. Thời gian từ lúc chốt đến khi thu đủ tiền**\n" + phan_bo([lay(h, cot, 9) for h in co], m))
    print("\n**Câu 10. Số lần phải nhắc**\n" + phan_bo([lay(h, cot, 10) for h in co], m))
    print("\n**Câu 11. Có người báo bỏ đi sát ngày khởi hành**\n" + phan_bo([lay(h, cot, 11) for h in co], m))

    # --- nhóm 3: mức phiền (câu 12)
    diem12 = [int(lay(h, cot, 12)) for h in co if lay(h, cot, 12).isdigit()]
    tb12 = sum(diem12) / len(diem12) if diem12 else 0
    print("\n## 5. Mức phiền phức (câu 12, thang 1 đến 5)\n")
    print(f"- Điểm trung bình: **{tb12:.2f}** trên {len(diem12)} phiếu, ngưỡng ủng hộ từ 3,5, dưới 2,5 là không ủng hộ → {ket_luan(tb12, *NGUONG['phien'])}")
    print("- Phân bố: " + ", ".join(f"{k}: {v}" for k, v in sorted(Counter(diem12).items())))

    # --- tách nhóm "Mình" (trưởng nhóm)
    minh = [h for h in co if lay(h, cot, 6) == "Mình"]
    if minh:
        d = [int(lay(h, cot, 12)) for h in minh if lay(h, cot, 12).isdigit()]
        print(f"\n**Riêng nhóm \"Mình\" (người đứng ra đặt, {len(minh)} người):** điểm phiền trung bình {sum(d) / len(d):.2f}; ", end="")
        print("chọn ít nhất 2 khó khăn: " + ti_le(sum(1 for h in minh if len([x for x in tach_nhieu(lay(h, cot, 8), TUY_CHON[8]) if x != 'Không gặp khó khăn']) >= 2), len(minh)))

    # --- nhóm 4: ý định sử dụng (câu 13) - tính trên toàn mẫu
    d13 = [int(lay(h, cot, 13)) for h in du_lieu if lay(h, cot, 13).isdigit()]
    cao = sum(1 for x in d13 if x >= 4)
    t4 = cao / len(d13) if d13 else 0
    print("\n## 6. Ý định sử dụng (câu 13, thang 1 đến 5, toàn mẫu)\n")
    print(f"- Chọn 4 hoặc 5: **{ti_le(cao, len(d13))}**, ngưỡng ủng hộ từ 50%, dưới 30% là không ủng hộ → {ket_luan(t4, *NGUONG['muon_dung'])}")
    print("- Phân bố: " + ", ".join(f"{k}: {v}" for k, v in sorted(Counter(d13).items())))
    print("- Lưu ý: đây là **ý định**, không phải hành vi; báo cáo ghi \"ý định sử dụng\", không viết \"nhu cầu thị trường\".")
    d13_co = [int(lay(h, cot, 13)) for h in co if lay(h, cot, 13).isdigit()]
    if d13_co:
        print(f"- Riêng người từng đi nhóm ({len(d13_co)} người): chọn 4 hoặc 5 là {ti_le(sum(1 for x in d13_co if x >= 4), len(d13_co))}")

    # --- nhóm 5: tính năng và thông số thiết kế
    print("\n## 7. Tính năng cần nhất (câu 14, chọn tối đa 3, toàn mẫu)\n")
    print(dem_nhieu([lay(h, cot, 14) for h in du_lieu], 14, n))
    print("\n## 8. Thông số thiết kế (câu 15 đến 17, toàn mẫu)\n")
    print("**Câu 15. Hạn giữ chỗ**\n" + phan_bo([lay(h, cot, 15) for h in du_lieu], n))
    print("\n**Câu 16. Cách trả tiền ưa thích**\n" + phan_bo([lay(h, cot, 16) for h in du_lieu], n))
    print("\n**Câu 17. Xử lý người trễ hạn**\n" + phan_bo([lay(h, cot, 17) for h in du_lieu], n))

    # --- góp ý
    gop_y = [lay(h, cot, 19) for h in du_lieu if lay(h, cot, 19)]
    print(f"\n## 9. Góp ý tự do (câu 19): {len(gop_y)} ý kiến\n")
    for g in gop_y:
        print(f"- {g}")


if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("Cách dùng: py -3 -I -X utf8 phan_tich.py file.csv")
        sys.exit(1)
    main(sys.argv[1])
