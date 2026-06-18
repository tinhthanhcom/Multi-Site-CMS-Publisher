**Name:** Multi-Site CMS Publisher
**Description:** Ứng dụng nội bộ quản lý nhiều website ASP.NET, hỗ trợ soạn bài, AI sinh nội dung, lên lịch và đăng bài tự động trực tiếp qua database của từng site.
**Repository:** Chưa gắn Git repo trong workspace hiện tại
**Primary Stack:** ASP.NET Core 8, Blazor Server, SQL Server, Entity Framework Core, Dapper, Hangfire, Serilog
**Environment:** Intranet / VPN only
**Last Updated:** 2026-06-18

## Product Goal

Xây dựng một hệ thống nội bộ cho phép đội vận hành nội dung:
- quản lý kết nối tới nhiều website ASP.NET;
- cấu hình field mapping linh hoạt theo từng database;
- soạn bài thủ công hoặc nhờ AI hỗ trợ;
- đăng bài ngay hoặc theo lịch tự động;
- theo dõi audit log, trạng thái publish và scheduler.

## Current State

- Workspace hiện mới có tài liệu thiết kế và kế hoạch triển khai.
- Chưa thấy source code ứng dụng, solution `.sln`, hoặc cấu trúc project .NET trong thư mục hiện tại.
- Chưa phải Git repository tại thời điểm tạo context này.
- Dự án đang ở pha chuẩn bị triển khai kỹ thuật, gần với `Phase 1: Setup & Nền tảng`.

## Architecture Summary

### Frontend
- Blazor Server admin UI
- Màn hình chính: dashboard, site config, post editor, scheduler

### Backend
- ASP.NET Core 8 API / application services
- Business modules:
  - Site Manager
  - Field Mapping
  - Post Service / Publisher
  - AI Content Generator
  - Scheduler
  - Audit Log

### Data Access
- `AppDB` dùng SQL Server để lưu users, sites, mappings, posts, prompt templates, schedules, audit logs
- Entity Framework Core cho AppDB
- Dapper cho kết nối động và thao tác với database của từng website đích

### Scheduler / Jobs
- Hangfire với SQL Server storage
- Job types:
  - publish scheduled posts
  - AI auto publish
  - retry failed posts
  - cleanup logs

### AI
- Provider ưu tiên trong tài liệu: Anthropic Claude
- Có thể mở rộng OpenAI
- Hỗ trợ prompt template chung và prompt riêng theo site

## Non-Negotiable Constraints

- Chỉ chạy trong intranet hoặc sau VPN
- Bắt buộc HTTPS
- Connection string của site phải được mã hóa AES-256-GCM
- Không log secret hoặc connection string
- Validate tên bảng/cột bằng whitelist trước khi sinh query động
- Chỉ cho phép parameterized values, không raw SQL từ người dùng
- Tài khoản DB site chỉ nên có quyền tối thiểu, ưu tiên `INSERT` và `SELECT` nếu cần

## Module Map

| Module | Type | Responsibility | Status |
|---|---|---|---|
| foundation | platform | solution structure, DI, config, logging, auth base | planned |
| appdb | data | schema, EF Core context, seed data | designed |
| site-manager | feature | quản lý site, encrypted connection string, test connection | designed |
| field-mapping | feature | map bảng/cột giữa logic post và DB site | designed |
| post-editor | feature | soạn bài, draft, preview, slug, metadata | designed |
| publisher | feature | publish ngay, publish theo lịch, retry fail | designed |
| ai-content | feature | generate nội dung, prompt templates, token tracking | designed |
| scheduler | feature | Hangfire jobs và auto publish schedules | designed |
| audit-dashboard | feature | logs, stats, reporting, notifications | designed |
| deployment | ops | env vars, IIS/Kestrel, backup, production hardening | planned |

## Domain Concepts

- `Site`: website đích với metadata, encrypted connection string, prompt mặc định
- `SiteFieldMapping`: cấu hình bảng/cột cho thao tác publish
- `Post`: bản nháp / scheduled / published trong AppDB
- `AIPromptTemplate`: template dùng để sinh nội dung
- `AutoPublishSchedule`: cấu hình AI + lịch chạy
- `AuditLog`: lịch sử thao tác và sự kiện hệ thống

## Canonical Status Flow

`draft -> scheduled -> publishing -> published`

Nhánh lỗi:

`publishing -> failed`

## Planned API Surface

- `/api/sites`
- `/api/sites/{id}/test-conn`
- `/api/sites/{id}/mapping`
- `/api/sites/{id}/tables`
- `/api/sites/{id}/columns/{table}`
- `/api/posts`
- `/api/posts/{id}/publish`
- `/api/posts/{id}/schedule`
- `/api/ai/generate`
- `/api/ai/templates`
- `/api/logs`
- `/api/dashboard/stats`

## Data Model Highlights

Core tables từ tài liệu:
- `Users`
- `Sites`
- `SiteFieldMappings`
- `Posts`
- `AIPromptTemplates`
- `AutoPublishSchedules`
- `AuditLogs`

Views / procedures đã được định nghĩa trong tài liệu SQL:
- `vw_PostsSummary`
- `vw_SiteStats`
- `sp_GetScheduledPosts`
- `sp_WriteAuditLog`

## Working Agreements For AI Assistants

Mọi AI làm việc trong workspace này nên:
- đọc `AGENTS.md` hoặc `CLAUDE.md` trước, sau đó đọc `.context/ACTIVE.md` và file này;
- xem `docs/system-design.md`, `docs/deployment-plan.md`, `docs/database-design.sql` trước khi đề xuất kiến trúc mới;
- cập nhật `.context/HISTORY.md` sau mỗi thay đổi đáng kể;
- cập nhật `.context/ACTIVE.md` khi đổi phase, đổi trọng tâm, hoặc hoàn tất task quan trọng;
- ghi quyết định mới vào `.context/DECISIONS.md` và thêm ADR riêng nếu quyết định ảnh hưởng kiến trúc;
- ghi lỗi lặp lại, anti-pattern, hoặc production risk vào `.context/ERRORS.md`.

## Priorities Right Now

1. Khởi tạo solution và project structure .NET 8
2. Thiết lập AppDB + EF Core + seed data cơ bản
3. Thiết lập auth / role model
4. Hoàn thành site management + dynamic DB inspection
5. Hoàn thành field mapping an toàn trước khi đi vào publish thực tế

## Open Questions

- Chọn kiến trúc solution cụ thể: monolith nhiều project hay tách Web/API riêng thật sự
- Cơ chế auth cuối cùng: ASP.NET Core Identity tùy biến hay session auth tự xây gọn hơn
- Rich text editor sẽ dùng TipTap hay Quill trong Blazor
- Hạ tầng deploy chính: IIS, Kestrel sau reverse proxy, hay Linux service
- Có cần hỗ trợ OpenAI song song Anthropic ngay từ phase đầu không

## References

- `docs/system-design.md`
- `docs/deployment-plan.md`
- `docs/database-design.sql`
