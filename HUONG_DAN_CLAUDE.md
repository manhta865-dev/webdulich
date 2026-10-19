# Cách đưa vào project

1. Chép toàn bộ nội dung thư mục này vào **thư mục gốc của solution** (cùng cấp file .sln), giữ nguyên cấu trúc:
   - `CLAUDE.md`
   - `.claude/skills/*/SKILL.md` (5 skill)
   - `docs/` (DE_TAI.md, KE_HOACH.md, KHAO_SAT.md, TaoFormKhaoSat.gs)
2. Tạo nhánh git và commit bản hiện tại trước khi cho Claude Code sửa.
3. Sao lưu database local và host trước khi chạy bất cứ script nào.
4. Mở Claude Code tại thư mục gốc. Việc đầu tiên: nhờ nó đọc `CLAUDE.md` và `docs/`, tóm tắt lại, rồi đề xuất kế hoạch tuần hiện tại **trước khi sửa gì**.
5. Không đưa chuỗi kết nối database, mật khẩu, khóa API vào các file này hoặc vào git.
