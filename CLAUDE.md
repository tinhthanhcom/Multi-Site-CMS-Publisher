# Claude Workspace Adapter

Đây là entry file dành cho Claude CLI trong workspace này.

## Read Order

1. Đọc `.context/ACTIVE.md`
2. Đọc `.context/PROJECT.md`
3. Khi cần thêm context dài hạn, đọc:
   - `.context/DECISIONS.md`
   - `.context/ERRORS.md`
   - `.context/FILE-INDEX.md`
4. Khi task liên quan thiết kế, roadmap, database:
   - `docs/system-design.md`
   - `docs/deployment-plan.md`
   - `docs/database-design.sql`

## Operating Notes

- `.context/PROJECT.md` là nguồn sự thật chung
- Không nhân bản lại project context vào file này
- Mọi cập nhật state dài hạn nên ghi vào `.context/*`

## Update Rules

- Sau mỗi task đáng kể: cập nhật `.context/HISTORY.md`
- Khi sprint hoặc focus đổi: cập nhật `.context/ACTIVE.md`
- Khi chốt quyết định kỹ thuật: cập nhật `.context/DECISIONS.md`
- Khi gặp bug/risk đáng nhớ: cập nhật `.context/ERRORS.md`

## Current Reality

- Workspace này hiện mới có docs, chưa có codebase triển khai
- Chưa gắn Git repo cục bộ
- Không nên giả định branch, CI hay file structure đã tồn tại ngoài những gì được ghi trong `.context`
