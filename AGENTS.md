# Codex Workspace Adapter

Đây là entry file dành cho Codex CLI / Codex agents trong workspace này.

## Read Order

1. Đọc `.context/ACTIVE.md`
2. Đọc `.context/PROJECT.md`
3. Nếu task liên quan kiến trúc hoặc state dài hạn, đọc thêm:
   - `.context/DECISIONS.md`
   - `.context/ERRORS.md`
   - `.context/FILE-INDEX.md`
4. Nếu task liên quan thiết kế hệ thống hoặc data model, đọc:
   - `docs/system-design.md`
   - `docs/deployment-plan.md`
   - `docs/database-design.sql`

## Workspace Reality

- Workspace hiện chỉ có docs, chưa có source code thực thi
- Chưa phải Git repo tại thời điểm file này được tạo
- Không được giả định đã có solution `.NET` hay branch workflow thật

## Behavior Rules

- Dùng `.context/PROJECT.md` làm nguồn sự thật chính cho project context
- Sau thay đổi đáng kể, append ngắn vào `.context/HISTORY.md`
- Khi đổi phase, đổi mục tiêu, hoặc hoàn tất milestone, cập nhật `.context/ACTIVE.md`
- Khi chốt quyết định kỹ thuật mới, cập nhật `.context/DECISIONS.md`
- Khi phát hiện bug/risk/anti-pattern lặp lại, cập nhật `.context/ERRORS.md`

## Collaboration Goal

Mục tiêu là để Codex CLI và Claude CLI có thể tiếp nối công việc của nhau với cùng một context dài hạn, thay vì duy trì hai bộ mô tả riêng biệt.
