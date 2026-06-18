# History

[2026-06-18] docs: tạo shared context workspace cho Multi-Site CMS Publisher
[2026-06-18] docs: chuẩn hóa project summary, active sprint, file index, decisions, errors
[2026-06-18] docs: thêm adapter `AGENTS.md` và `CLAUDE.md` để Codex CLI và Claude CLI dùng chung context
[2026-06-18] chore: thêm `.claude/` rút gọn với hooks, commands, agents áp dụng từ Multi-AI-Workspace-Setup
[2026-06-18] feat(web): hoàn thành Phase 1 Web foundation (Agent B) - Serilog, cookie auth qua minimal-API endpoints, admin shell layout, toast service, Users CRUD (Admin). Build sạch, app khởi động, migrate/seed OK, login flow đã xác thực.
[2026-06-18] chore(scaffold): tạo solution `MultiSiteCmsPublisher.slnx` với 3 project (`Publisher.Web` Blazor Server + minimal API, `Publisher.Core`, `Publisher.Infrastructure`) + `Publisher.Tests`, target net8.0.
[2026-06-18] feat(core+infra): Phase 1 foundation (Agent A) - entities mirror `database-design.sql`, AppDbContext + Fluent config, migration InitialCreate (kèm views/SPs qua raw SQL), seed admin/prompt templates, ConnectionStringEncryptor AES-256-GCM, AuditLogService. Migration applied to LocalDB.
[2026-06-18] feat(infra): Phase 2 backend (Agent C) - SafeIdentifier whitelist, SiteDbConnector (Dapper, INFORMATION_SCHEMA, test-conn + permission probe), InsertCommandBuilder parameterized + SCOPE_IDENTITY. 39 unit tests.
[2026-06-18] feat(web): Phase 2 frontend (Agent D) - Site CRUD (encrypt conn string, test-before-save), field mapping UI (table/column dropdowns, custom fields, INSERT preview, validate test-insert-rollback).
[2026-06-18] feat(web+infra): Phase 3 (Agent E) - PostPublisher (build INSERT từ mapping, Dapper execute trong transaction, capture RemotePostId, status transitions, audit), Post list/editor (Quill, auto-slug bỏ dấu tiếng Việt), Publish Now / Schedule (UTC) / Retry.
[2026-06-18] test(e2e): Wave 4 verification - integration test publish thật vào LocalDB `TargetSiteDemo.dbo.Articles` (row inserted, RemotePostId captured, draft→published), injection bị SafeIdentifier chặn. Build sạch, 56 tests pass, app start + /login OK.
[2026-06-18] fix(web): thêm `@using Publisher.Core.Models` vào `SiteEdit.razor` (race giữa agent song song gây CS0246).
[2026-06-18] chore(config): trỏ AppDb sang SQL Server `localhost/01MultiSiteCMS` (instance mặc định, Windows auth). AppDbContextFactory đọc env `ConnectionStrings__AppDb` (mặc định DB mới). Áp migration: 7 bảng + 2 view + 2 proc; app start OK, seed (admin + prompt templates) đã ghi, /login=200.
[2026-06-18] chore(git): review (build sạch, 56/56 test pass) rồi merge `feature/phases-1-3-foundation` vào `main` (--no-ff, commit fcb2b17). Chưa push origin.
