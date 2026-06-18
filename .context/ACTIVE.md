**Name:** Sprint 0 - Foundation Setup
**Branch:** n/a (workspace chưa là git repo)
**Started:** 2026-06-18
**Focus:** Thiết lập shared context cho Claude CLI và Codex CLI, sau đó chuẩn bị scaffold kỹ thuật cho dự án
**Status:** in-progress

## In Progress

| Task | Owner | Status | Notes |
|---|---|---|---|
| Thiết lập shared AI context | codex | done | Dùng `.context` làm nguồn sự thật chung |
| Tóm tắt kiến trúc từ docs hiện có | codex | done | Đã trích từ tài liệu thiết kế và deployment plan |
| Chuẩn bị roadmap triển khai Phase 1 | team | next | Chờ quyết định scaffold solution thực tế |

## Recent Context

- 2026-06-18: Đọc và chuẩn hóa context từ `docs/system-design.md`, `docs/deployment-plan.md`, `docs/database-design.sql`
- 2026-06-18: Áp dụng mô hình shared context từ repo `orynvn/Multi-AI-Workspace-Setup-`
- 2026-06-18: Tạo adapter chung cho Claude CLI và Codex CLI

## Next Recommended Moves

1. Khởi tạo Git repo hoặc mở đúng repo chứa source code thực tế để bắt đầu lịch sử phát triển chuẩn
2. Scaffold solution .NET 8 với các project nền (`Web`, `API`, `Core`, `Infrastructure`)
3. Chuyển `docs/database-design.sql` thành migration / bootstrap strategy rõ ràng
4. Quyết định auth stack và editor integration trước khi code UI

## Session Start Checklist

- Đọc `AGENTS.md` hoặc `CLAUDE.md`
- Đọc `.context/PROJECT.md`
- Đọc file docs liên quan đến task đang làm
- Nếu làm thay đổi kiến trúc, cập nhật `DECISIONS.md`
- Nếu fix bug/risk, cập nhật `ERRORS.md`
