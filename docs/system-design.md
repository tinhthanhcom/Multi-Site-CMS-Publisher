# Hệ thống Quản lý & Đăng Bài Tự Động (Multi-Site CMS Publisher)

## 1. Tổng Quan Hệ Thống

### 1.1 Mục Tiêu

Xây dựng ứng dụng nội bộ cho phép quản lý và tự động đăng bài viết lên nhiều website ASP.NET cùng lúc, thông qua kết nối trực tiếp vào database của từng website. Hỗ trợ viết bài bằng AI và lên lịch đăng bài tự động.

### 1.2 Phạm Vi

- Quản lý cấu hình kết nối đến nhiều SQL Server database
- Cấu hình mapping cấu trúc bảng bài viết linh hoạt cho từng website
- Viết bài thủ công hoặc hỗ trợ AI sinh nội dung
- Đăng bài ngay lập tức hoặc lên lịch tự động
- Dashboard quản lý tổng quan

### 1.3 Đối Tượng Sử Dụng

Người quản trị nội bộ, biên tập viên nội dung — truy cập trong môi trường intranet/VPN.

---

## 2. Kiến Trúc Hệ Thống

```
┌─────────────────────────────────────────────────────┐
│                  FRONTEND (Blazor Server)             │
│  Dashboard | Site Config | Post Editor | Scheduler   │
└────────────────────┬────────────────────────────────-┘
                     │ HTTP / SignalR
┌────────────────────▼────────────────────────────────-┐
│              BACKEND (ASP.NET Core Web API)           │
│                                                       │
│  ┌─────────────┐  ┌──────────────┐  ┌─────────────┐  │
│  │ Site Manager│  │ Post Service │  │ AI Service  │  │
│  └─────────────┘  └──────────────┘  └─────────────┘  │
│  ┌─────────────┐  ┌──────────────┐  ┌─────────────┐  │
│  │ DB Connector│  │ Scheduler    │  │ Audit Log   │  │
│  │ (Dapper)    │  │ (Hangfire)   │  │ Service     │  │
│  └─────────────┘  └──────────────┘  └─────────────┘  │
└──────────┬──────────────────────────────-─────────────┘
           │
    ┌──────▼──────┐
    │  App DB      │  ← SQL Server (nội bộ)
    │  (AppDB)     │     Lưu cấu hình, bài viết, log
    └─────────────┘
           │
    Dynamic Connections
    ┌──────▼──────┐  ┌─────────────┐  ┌─────────────┐
    │  Site A DB  │  │  Site B DB  │  │  Site C DB  │
    │  SQL Server │  │  SQL Server │  │  SQL Server │
    └─────────────┘  └─────────────┘  └─────────────┘
```

### 2.1 Các Thành Phần Chính

| Thành phần | Công nghệ | Vai trò |
|---|---|---|
| Frontend | Blazor Server | UI quản trị |
| Backend API | ASP.NET Core 8 | Business logic |
| ORM nội bộ | Entity Framework Core | Truy cập AppDB |
| Dynamic Query | Dapper | INSERT động vào DB các site |
| Scheduler | Hangfire | Lên lịch đăng bài tự động |
| AI Integration | Claude API / OpenAI | Sinh nội dung bài viết |
| Encryption | AES-256 | Mã hóa connection string |
| Logging | Serilog + EF | Ghi log audit |

---

## 3. Thiết Kế Module

### 3.1 Module Quản Lý Website (Site Manager)

**Chức năng:**
- CRUD thông tin website (tên, URL, ghi chú)
- Cấu hình connection string (lưu dưới dạng mã hóa AES-256)
- Kiểm tra kết nối DB (Test Connection)
- Kích hoạt / vô hiệu hóa website

**Validation khi lưu:**
- Test kết nối trước khi lưu
- Kiểm tra permission tối thiểu: `INSERT` trên bảng bài viết

### 3.2 Module Cấu Hình Field Mapping

**Chức năng:**
- Cấu hình tên bảng bài viết cho từng site
- Mapping các trường bắt buộc: Title, Content, Slug, Status, PublishedAt
- Mapping các trường tùy chọn: Excerpt, Thumbnail, CategoryId, AuthorId, Tags
- Cấu hình giá trị mặc định cho từng trường
- Preview câu lệnh INSERT trước khi lưu

**Danh sách trường có thể mapping:**

| Nhóm | Trường logic | Ví dụ tên thực tế |
|---|---|---|
| Bắt buộc | Title | Title, PostTitle, TieuDe |
| Bắt buộc | Content | Content, Body, NoiDung |
| Bắt buộc | Status | Status, IsActive, TrangThai |
| Tùy chọn | Slug | Slug, Url, FriendlyUrl |
| Tùy chọn | Excerpt | Excerpt, ShortDesc, TomTat |
| Tùy chọn | PublishedAt | PublishedAt, PostDate, NgayDang |
| Tùy chọn | CategoryId | CategoryId, CatId, MaDanhMuc |
| Tùy chọn | Thumbnail | Thumbnail, Image, HinhAnh |
| Tùy chọn | AuthorId | AuthorId, UserId, NguoiDang |

### 3.3 Module Soạn & Đăng Bài (Post Editor)

**Flow đăng bài thủ công:**

```
Chọn Site → Load Field Mapping → Soạn bài (hoặc AI viết)
    → Preview → Validate → Đăng ngay / Lên lịch
```

**Chức năng soạn bài:**
- Rich text editor (TipTap / Quill)
- Tự động sinh Slug từ Title
- Preview nội dung trước khi đăng
- Lưu nháp (Draft) vào AppDB trước khi đăng lên site

**Trạng thái bài viết:**

```
[draft] → [scheduled] → [publishing] → [published]
                                    ↘ [failed]
```

### 3.4 Module AI Content Generator

**Flow:**

```
Nhập từ khóa / chủ đề / brief
    → Chọn loại nội dung (bài viết, mô tả sản phẩm, tin tức...)
    → Cấu hình độ dài, giọng văn, ngôn ngữ
    → Gọi AI API → Stream response → Hiển thị kết quả
    → Người dùng chỉnh sửa → Đăng bài
```

**Cấu hình AI:**

```json
{
  "provider": "anthropic",
  "model": "claude-sonnet-4-20250514",
  "defaultPromptTemplate": "Viết bài SEO về chủ đề: {topic}. Độ dài: {length} từ. Giọng văn: {tone}.",
  "maxTokens": 4000,
  "temperature": 0.7
}
```

**Template prompt theo từng site:**
- Mỗi site có thể cấu hình system prompt riêng (giọng văn, domain, lĩnh vực)
- Biến động: `{topic}`, `{keywords}`, `{length}`, `{tone}`, `{site_name}`

### 3.5 Module Scheduler (Tự Động Đăng Bài)

**Loại lịch:**
- One-time: Đăng một lần vào thời điểm cụ thể
- Recurring: Đăng theo chu kỳ (hàng ngày, hàng tuần)
- AI Auto: Tự động sinh nội dung + đăng theo lịch

**Hangfire Job Types:**

```csharp
// Đăng bài theo lịch
[Queue("publish")]
PublishScheduledPostsJob : IJob

// AI tự động sinh + đăng bài
[Queue("ai-auto")]
AIAutoPublishJob : IJob

// Retry khi thất bại
MaxRetryAttempts: 3
RetryDelay: 5 minutes
```

**Dashboard Hangfire:** Truy cập tại `/hangfire` (yêu cầu xác thực admin)

### 3.6 Module Audit Log

Ghi lại toàn bộ thao tác quan trọng:

| Sự kiện | Thông tin ghi log |
|---|---|
| INSERT bài viết | SiteId, PostId, TableName, UserId, Timestamp |
| Thay đổi cấu hình | SiteId, OldValue (masked), NewValue (masked), UserId |
| Kết nối DB | SiteId, Success/Fail, ErrorMessage |
| AI Generate | SiteId, Prompt, TokensUsed, Duration |
| Login/Logout | UserId, IP, UserAgent |

---

## 4. Bảo Mật

### 4.1 Mã Hóa Connection String

```csharp
// Sử dụng AES-256-GCM
// Key lưu trong Environment Variable: PUBLISHER_ENCRYPTION_KEY
// Không bao giờ log connection string dưới bất kỳ dạng nào
public class ConnectionStringEncryptor {
    public string Encrypt(string plainConnectionString);
    public string Decrypt(string encryptedConnectionString);
}
```

### 4.2 Chống SQL Injection qua Field Mapping

- Whitelist tên bảng và tên cột: chỉ cho phép `[a-zA-Z0-9_]`
- Validate tên bảng/cột tồn tại thực sự trong DB trước khi lưu mapping
- Dùng parameterized query cho values (Dapper `@param`)
- Không cho phép người dùng nhập raw SQL

```csharp
// Validate tên bảng/cột trước khi dùng
private static readonly Regex SafeIdentifier = new(@"^[a-zA-Z_][a-zA-Z0-9_]*$");

if (!SafeIdentifier.IsMatch(tableName))
    throw new SecurityException("Invalid table name");
```

### 4.3 Xác Thực & Phân Quyền

| Role | Quyền |
|---|---|
| Admin | Toàn bộ, bao gồm cấu hình site và field mapping |
| Editor | Soạn bài, đăng bài, xem lịch |
| Viewer | Chỉ xem dashboard và log |

### 4.4 Network

- App chỉ chạy trong intranet/VPN
- HTTPS bắt buộc
- Rate limiting cho AI API endpoint

---

## 5. Cấu Hình Môi Trường

```json
// appsettings.json (Production)
{
  "ConnectionStrings": {
    "AppDb": "Server=...;Database=PublisherApp;..."
  },
  "Encryption": {
    "KeyEnvVar": "PUBLISHER_ENCRYPTION_KEY"
  },
  "AI": {
    "Provider": "anthropic",
    "ApiKeyEnvVar": "ANTHROPIC_API_KEY",
    "DefaultModel": "claude-sonnet-4-20250514"
  },
  "Hangfire": {
    "WorkerCount": 5,
    "DashboardPath": "/hangfire"
  },
  "Auth": {
    "AdminPassword": "từ env var"
  }
}
```

---

## 6. API Endpoints Chính

```
POST   /api/sites                    Tạo site mới
GET    /api/sites                    Danh sách sites
PUT    /api/sites/{id}               Cập nhật site
POST   /api/sites/{id}/test-conn     Test kết nối DB
DELETE /api/sites/{id}               Xóa site

POST   /api/sites/{id}/mapping       Lưu field mapping
GET    /api/sites/{id}/mapping       Lấy field mapping
GET    /api/sites/{id}/tables        Lấy danh sách bảng từ DB site
GET    /api/sites/{id}/columns/{t}   Lấy danh sách cột của bảng

POST   /api/posts                    Tạo bài nháp
PUT    /api/posts/{id}               Cập nhật bài
POST   /api/posts/{id}/publish       Đăng bài ngay
POST   /api/posts/{id}/schedule      Lên lịch đăng
GET    /api/posts                    Danh sách bài (filter by site, status)

POST   /api/ai/generate              Sinh nội dung bằng AI (stream)
GET    /api/ai/templates             Danh sách prompt templates

GET    /api/logs                     Audit log (filter/pagination)
GET    /api/dashboard/stats          Thống kê tổng quan
```

---

## 7. Yêu Cầu Hạ Tầng

| Thành phần | Yêu cầu tối thiểu |
|---|---|
| OS | Windows Server 2019+ hoặc Linux |
| Runtime | .NET 8 |
| AppDB | SQL Server 2019+ (Express đủ dùng) |
| RAM | 2 GB |
| Storage | 10 GB |
| Network | Có thể kết nối đến tất cả DB các site |
| Internet | Cần kết nối ra ngoài để gọi AI API |
