# File Index

## Current Workspace Files

| Path | Purpose | Notes |
|---|---|---|
| `docs/system-design.md` | tài liệu kiến trúc tổng thể | nguồn chính cho module map và security constraints |
| `docs/deployment-plan.md` | lộ trình triển khai 7 phase | nguồn chính cho current phase và backlog |
| `docs/database-design.sql` | schema AppDB và stored procedures | nguồn chính cho domain model |

## Shared AI Context Files

| Path | Purpose |
|---|---|
| `.context/PROJECT.md` | nguồn sự thật chung cho project |
| `.context/ACTIVE.md` | sprint hiện tại và session handoff |
| `.context/HISTORY.md` | changelog ngắn gọn |
| `.context/DECISIONS.md` | index các quyết định kiến trúc |
| `.context/ERRORS.md` | bug log, risk, anti-pattern |

## Expected Future Code Structure

Theo `docs/deployment-plan.md`, solution dự kiến sẽ gồm:

| Expected Path | Purpose |
|---|---|
| `src/Publisher.Web/` | Blazor Server admin UI |
| `src/Publisher.API/` | API / app endpoints |
| `src/Publisher.Core/` | domain models, interfaces, business contracts |
| `src/Publisher.Infrastructure/` | EF Core, Dapper, Hangfire, integrations |
| `tests/` | unit/integration tests |

## Update Rule

Cập nhật file này khi:
- xuất hiện source code thật;
- đổi tên module hoặc project;
- thêm thư mục feature lớn;
- cần giúp AI locate file nhanh hơn trong workspace lớn.
