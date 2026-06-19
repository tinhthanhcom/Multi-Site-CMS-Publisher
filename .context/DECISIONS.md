# Decisions

## Active Decisions

### D-001: Shared AI context uses `.context/` as single source of truth
- Date: 2026-06-18
- Status: accepted
- Why:
  - Tránh lặp nội dung giữa `AGENTS.md` và `CLAUDE.md`
  - Giảm drift khi thay đổi kiến trúc, phase hoặc convention
  - Phù hợp với mô hình trong repo `orynvn/Multi-AI-Workspace-Setup-`
- Consequence:
  - Mọi cập nhật trạng thái dự án nên đi vào `.context/*`
  - Entry files cho từng AI chỉ còn vai trò điều hướng

### D-002: Tạm coi dự án đang ở giai đoạn docs-first / pre-scaffold
- Date: 2026-06-18
- Status: accepted
- Why:
  - Workspace hiện chỉ có thư mục `docs`
  - Chưa có `.sln`, source code, hoặc Git history cục bộ
- Consequence:
  - Các AI không nên giả định codebase đã tồn tại
  - Mọi task triển khai tiếp theo cần bám vào tài liệu thiết kế hiện có

### D-003: Solution layout — 3 project + tests, không tách API riêng
- Date: 2026-06-18
- Status: accepted
- Why:
  - Blazor Server gọi service in-process nên project `Publisher.API` riêng là dư thừa
  - Giảm boilerplate, ít hosting hơn cho app nội bộ
- Consequence:
  - `Publisher.Web` (Blazor Server + minimal API endpoint cho auth) + `Publisher.Core` + `Publisher.Infrastructure` + `Publisher.Tests`
  - Lệch nhẹ so với deployment-plan (liệt kê 4 project) — chấp nhận
  - Target `net8.0` (đúng docs); máy có cả runtime .NET 8 và 10. SDK 10 tạo `.slnx`, dùng template `blazor` (InteractiveServer)

### D-004: Auth — cookie tự xây trên bảng Users, không dùng ASP.NET Core Identity
- Date: 2026-06-18
- Status: accepted
- Why:
  - Schema đã có bảng `Users` (BCrypt PasswordHash, cột Role); Identity sẽ tạo schema trùng/xung đột
  - Intranet, không cần JWT
- Consequence:
  - Cookie auth, BCrypt.Net-Next verify, role claims Admin/Editor/Viewer
  - Đăng nhập/đăng xuất qua minimal-API endpoint (`/auth/*`) vì InteractiveServer không gọi được `SignInAsync` trong circuit; login page là static-SSR form

### D-005: Database EF code-first + Quill editor
- Date: 2026-06-18
- Status: accepted
- Why:
  - Entities mirror `database-design.sql`; migration InitialCreate tái tạo bảng/index/constraint + views/SPs qua `migrationBuilder.Sql()`
  - Áp vào LocalDB `(localdb)\MSSQLLocalDB`; seed admin + prompt templates ở runtime (DbInitializer) vì cần BCrypt hash động
  - Quill chọn cho rich text (JS interop đơn giản nhất trong Blazor), có thể thay TipTap sau
- Consequence:
  - FK không-cascade đặt `DeleteBehavior.Restrict` để tránh multiple-cascade-path của SQL Server
  - Connection string site mã hóa AES-256-GCM; key từ env `PUBLISHER_ENCRYPTION_KEY` (dev fallback `Encryption:Key` trong `appsettings.Development.json`, file này gitignored — xem `appsettings.Development.json.example`)

### D-006: AI Phase 4 — gọi qua dịch vụ "AI Gateway" riêng (wrap codex CLI), Claude/Gemini là fallback
- Date: 2026-06-19
- Status: accepted
- Why:
  - Muốn dùng **codex CLI** làm nguồn sinh nội dung chính (chạy headless `codex exec --json --sandbox read-only --ask-for-approval never`), tận dụng hạ tầng sẵn có thay vì trả phí token API trực tiếp
  - Đặt chuỗi fallback (codex → Claude → Gemini) **bên trong dịch vụ** (unified gateway) → CMS chỉ gọi 1 endpoint, đổi provider không phải sửa CMS
  - Tách dịch vụ ra VPS riêng + domain riêng để cô lập runtime Node/codex khỏi app .NET
- Consequence:
  - Repo/thư mục mới `ai-gateway/` (Node 20 + TypeScript + Fastify); KHÔNG nằm trong solution .NET, có thể tách repo riêng sau
  - API v1: `POST /v1/generate` (JSON request/response, **không streaming** ở v1 vì codex chỉ stream mức message), trả bản gốc + tất cả bản dịch trong 1 response; auth Bearer token
  - CMS Phase 4 gọi gateway qua typed `HttpClient` (`IAIContentService`), key từ env `PUBLISHER_AIGATEWAY_KEY`
  - Rủi ro: codex là coding agent (overhead/latency) → nếu cần đổi CodexProvider sang OpenAI API trực tiếp mà không đụng CMS; pin version `@openai/codex`
  - Plan đầy đủ: `C:\Users\trung\.claude\plans\memoized-mixing-kahn.md`

## When To Add A New Decision

Thêm decision mới khi có thay đổi ở một trong các nhóm sau:
- thay đổi kiến trúc solution;
- thay đổi auth model;
- thay đổi cách mã hóa secret / connection string;
- thay đổi scheduler / background processing;
- thay đổi AI provider strategy;
- thay đổi database strategy hoặc publish flow.
