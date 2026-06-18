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
| `.claude/settings.json` | cấu hình Claude CLI hooks và MCP |
| `.claude/agents/` | agent prompts rút gọn cho Claude CLI |
| `.claude/commands/` | lệnh tắt hỗ trợ workflow |
| `.claude/hooks/` | safety và context discipline hooks |

## Actual Code Structure (Phase 1-3 implemented)

Solution: `MultiSiteCmsPublisher.slnx` (net8.0). Không có project API riêng (xem D-003).

| Path | Purpose |
|---|---|
| `src/Publisher.Core/Entities/` | POCO entities mirror `database-design.sql` (User, Site, SiteFieldMapping, Post, AIPromptTemplate, AutoPublishSchedule, AuditLog) |
| `src/Publisher.Core/Enums/` | `UserRole`/`UserRoles`, `PostStatus`/`PostStatuses` (string constants) |
| `src/Publisher.Core/Interfaces/` | `IConnectionStringEncryptor`, `IAuditLogService`, `ISiteDbConnector`, `IPostPublisher` |
| `src/Publisher.Core/Models/` | DTO: TableInfo, ColumnInfo, ConnectionTestResult, InsertPreview, PublishResult, FieldMappingInput, CustomFieldDef, PostPublishData |
| `src/Publisher.Core/Options/` | `EncryptionOptions` |
| `src/Publisher.Infrastructure/Data/` | `AppDbContext`, `Configurations/*`, `AppDbContextFactory` (design-time), `DbInitializer` (seed), `Migrations/*` |
| `src/Publisher.Infrastructure/Security/` | `ConnectionStringEncryptor` (AES-256-GCM), `SafeIdentifier` (whitelist) |
| `src/Publisher.Infrastructure/Sites/` | `SiteDbConnector` (Dapper, INFORMATION_SCHEMA) |
| `src/Publisher.Infrastructure/Publishing/` | `InsertCommandBuilder` (parameterized INSERT), `PostPublisher` |
| `src/Publisher.Infrastructure/Auditing/` | `AuditLogService` |
| `src/Publisher.Infrastructure/DependencyInjection.cs` | `AddInfrastructure(...)` đăng ký mọi service |
| `src/Publisher.Web/Program.cs` | Serilog, cookie auth, AddInfrastructure, migrate+seed on startup, map `/auth/*` |
| `src/Publisher.Web/Endpoints/AuthEndpoints.cs` | login/logout/change-password (minimal API) |
| `src/Publisher.Web/Components/Layout/` | admin shell (sidebar/header/breadcrumb), `EmptyLayout` |
| `src/Publisher.Web/Components/Pages/Account/` | Login, AccessDenied, ChangePassword |
| `src/Publisher.Web/Components/Pages/Users/` | UserList, UserEdit (Admin) |
| `src/Publisher.Web/Components/Pages/Sites/` | SiteList, SiteEdit, SiteMapping (Admin) |
| `src/Publisher.Web/Components/Pages/Posts/` | PostList, PostEditor |
| `src/Publisher.Web/Services/` | ToastService, MappingValidator, SlugGenerator (static) |
| `src/Publisher.Web/wwwroot/js/quill-interop.js` | Quill rich text interop |
| `src/Publisher.Web/appsettings.Development.json.example` | mẫu cấu hình dev (file thật gitignored) |
| `tests/Publisher.Tests/` | xUnit: SafeIdentifier, InsertCommandBuilder, SlugGenerator (unit) + PublishEndToEndTests (`[Trait Category=Integration]`, cần LocalDB) |

## Update Rule

Cập nhật file này khi:
- xuất hiện source code thật;
- đổi tên module hoặc project;
- thêm thư mục feature lớn;
- cần giúp AI locate file nhanh hơn trong workspace lớn.
