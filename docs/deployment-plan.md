# Kế Hoạch Triển Khai: Multi-Site CMS Publisher

## Thông Tin Dự Án

| | |
|---|---|
| Tên dự án | Multi-Site CMS Publisher |
| Mục tiêu | Ứng dụng nội bộ quản lý & tự động đăng bài cho nhiều website ASP.NET |
| Tech stack | ASP.NET Core 8 + Blazor Server + SQL Server + Hangfire + Claude AI |
| Môi trường | Intranet / VPN |
| Tổng thời gian ước tính | **7 tuần** |

---

## Tóm Tắt Các Phase

| Phase | Tên | Thời gian | Deliverable |
|---|---|---|---|
| P1 | Setup & Nền Tảng | Tuần 1 | Project scaffold, DB, auth |
| P2 | Quản Lý Site & Mapping | Tuần 2 | CRUD site, field mapping |
| P3 | Soạn & Đăng Bài Thủ Công | Tuần 3 | Post editor, publish |
| P4 | Tích Hợp AI | Tuần 4 | AI content generator |
| P5 | Scheduler Tự Động | Tuần 5 | Hangfire jobs, auto-publish |
| P6 | Dashboard & Audit | Tuần 6 | Stats, logs, reporting |
| P7 | Testing & Deploy | Tuần 7 | QA, production release |

---

## Phase 1: Setup & Nền Tảng (Tuần 1)

### Mục tiêu
Tạo nền tảng kỹ thuật, cấu trúc project, database, và xác thực người dùng.

### Công việc

**1.1 Khởi tạo Project**
- [ ] Tạo solution với các project: `Publisher.Web` (Blazor), `Publisher.API`, `Publisher.Core`, `Publisher.Infrastructure`
- [ ] Cấu hình dependency injection, middleware pipeline
- [ ] Cấu hình logging với Serilog (file + console)
- [ ] Thiết lập `.gitignore`, environment variables

**1.2 Database**
- [ ] Chạy script `database-design.sql` tạo AppDB
- [ ] Cài đặt EF Core + tạo DbContext cho AppDB
- [ ] Cấu hình connection string AppDB từ environment variable
- [ ] Seed data: tài khoản admin, prompt templates mặc định

**1.3 Authentication**
- [ ] Implement đăng nhập bằng username/password (BCrypt)
- [ ] Session-based auth với ASP.NET Core Identity (không dùng JWT vì là intranet)
- [ ] Phân quyền theo Role: Admin | Editor | Viewer
- [ ] Trang login, logout, đổi mật khẩu

**1.4 Layout & Navigation**
- [ ] Blazor layout chính: sidebar navigation, header, breadcrumb
- [ ] Responsive cơ bản (desktop-first, vì app nội bộ)
- [ ] Thông báo toast notification component

### Definition of Done
- Đăng nhập được bằng tài khoản admin
- CRUD Users cơ bản
- Database đã có đủ bảng

---

## Phase 2: Quản Lý Site & Field Mapping (Tuần 2)

### Mục tiêu
Cho phép thêm/sửa/xóa cấu hình website và mapping cấu trúc bảng bài viết.

### Công việc

**2.1 Quản Lý Site**
- [ ] Trang danh sách site (table với filter, search)
- [ ] Form thêm/sửa site: tên, URL, description, connection string (input dạng password)
- [ ] Implement `ConnectionStringEncryptor` (AES-256-GCM)
- [ ] Nút "Test Kết Nối" — kiểm tra kết nối và hiển thị kết quả ngay lập tức
- [ ] Kích hoạt / vô hiệu hóa site
- [ ] Xóa site (soft delete hoặc cảnh báo nếu có bài viết)

**2.2 Dynamic DB Connector**
- [ ] Service `SiteDbConnector` — nhận SiteId, decrypt connection string, tạo `SqlConnection` qua Dapper
- [ ] Method lấy danh sách bảng trong DB của site (`INFORMATION_SCHEMA.TABLES`)
- [ ] Method lấy danh sách cột của bảng (`INFORMATION_SCHEMA.COLUMNS`)
- [ ] Whitelist validation tên bảng/cột (regex `^[a-zA-Z_][a-zA-Z0-9_]*$`)

**2.3 Field Mapping**
- [ ] Trang cấu hình field mapping cho từng site
- [ ] Dropdown chọn bảng (load từ DB thực tế của site)
- [ ] Dropdown chọn cột cho từng trường logic (Title, Content, Status, ...)
- [ ] Cấu hình giá trị Status: Draft value / Published value
- [ ] Thêm Custom Fields (key-value, default value, data type)
- [ ] Preview câu lệnh INSERT sẽ được thực thi
- [ ] Validate: thử INSERT test record rồi rollback để kiểm tra

### Definition of Done
- Thêm được 2+ site với connection string khác nhau
- Test kết nối thành công/thất bại hiển thị đúng
- Cấu hình field mapping và preview INSERT hoạt động đúng

---

## Phase 3: Soạn & Đăng Bài Thủ Công (Tuần 3)

### Mục tiêu
Người dùng có thể viết bài và đăng lên một site cụ thể.

### Công việc

**3.1 Post Editor**
- [ ] Trang danh sách bài viết (filter theo site, status, ngày)
- [ ] Form soạn bài: Title, Content (rich text), Excerpt, Slug (tự sinh từ Title)
- [ ] Tích hợp rich text editor: **TipTap** hoặc **Quill** qua JS interop
- [ ] Chọn site đăng bài (dropdown, chỉ hiện site đã cấu hình mapping)
- [ ] Điền các trường tùy chọn: CategoryId, AuthorId, Thumbnail URL
- [ ] Lưu nháp (Draft) vào AppDB

**3.2 Publish Service**
- [ ] Service `PostPublisher` — nhận PostId, xây câu INSERT động dựa trên field mapping
- [ ] Thực thi INSERT vào DB của site qua Dapper
- [ ] Lưu `RemotePostId` (scope_identity()) vào AppDB sau khi thành công
- [ ] Cập nhật trạng thái: `publishing` → `published` hoặc `failed`
- [ ] Ghi AuditLog khi đăng thành công/thất bại

**3.3 Đăng Bài**
- [ ] Nút "Đăng Ngay" — publish và hiển thị kết quả
- [ ] Nút "Lên Lịch" — chọn datetime, lưu `ScheduledAt`, set status = `scheduled`
- [ ] Cho phép sửa/xóa bài ở trạng thái draft hoặc scheduled
- [ ] Retry thủ công cho bài ở trạng thái `failed`

### Definition of Done
- Soạn bài và đăng thủ công lên site thực tế thành công
- Bài viết xuất hiện trong DB của site đúng với field mapping
- Trạng thái bài viết cập nhật chính xác

---

## Phase 4: Tích Hợp AI (Tuần 4)

### Mục tiêu
Hỗ trợ sinh nội dung bài viết bằng AI (Claude API).

### Công việc

**4.1 AI Service**
- [ ] Service `AIContentGenerator` — gọi Claude API với streaming
- [ ] Cấu hình: API key từ env var, model, max tokens, temperature
- [ ] Xử lý stream response — hiển thị nội dung dần dần trên UI (SignalR hoặc Blazor streaming)
- [ ] Error handling: rate limit, timeout, API error

**4.2 Prompt Template Manager**
- [ ] CRUD Prompt Templates (Admin only)
- [ ] Biến template: `{topic}`, `{keywords}`, `{length}`, `{tone}`, `{site_name}`
- [ ] Template per site (system prompt riêng cho từng site)
- [ ] Test prompt trực tiếp từ giao diện quản lý

**4.3 AI trong Post Editor**
- [ ] Nút "AI Viết" trên form soạn bài
- [ ] Sidebar: nhập topic, keywords, chọn template, chọn giọng văn, độ dài
- [ ] Streaming nội dung vào editor khi AI generate
- [ ] Nút "Generate lại" và "Insert vào editor"
- [ ] Lưu `AIPromptUsed` và `AITokensUsed` vào bảng Posts

**4.4 Kiểm Soát Chi Phí AI**
- [ ] Hiển thị số token ước tính trước khi gọi
- [ ] Log tokens sử dụng theo ngày/tháng per site
- [ ] Cấu hình giới hạn token/ngày (optional)

### Definition of Done
- Nhập topic → AI sinh bài → hiển thị streaming → đăng bài thành công
- Prompt template có thể cấu hình per site
- Token usage được ghi log

---

## Phase 5: Scheduler Tự Động (Tuần 5)

### Mục tiêu
Tự động đăng bài theo lịch, bao gồm AI sinh nội dung + publish không cần người dùng thao tác.

### Công việc

**5.1 Cài Đặt Hangfire**
- [ ] Cài Hangfire với SQL Server storage (dùng AppDB)
- [ ] Cấu hình Hangfire server với `WorkerCount = 5`
- [ ] Dashboard tại `/hangfire` với xác thực Admin
- [ ] Recurring job runner

**5.2 Jobs**
- [ ] `PublishScheduledPostsJob` — chạy mỗi phút, query `sp_GetScheduledPosts`, publish từng bài
- [ ] `AIAutoPublishJob` — sinh nội dung AI + publish theo `AutoPublishSchedules`
- [ ] `RetryFailedPostsJob` — retry bài `failed` với `RetryCount < 3` sau 5 phút
- [ ] `CleanupJob` — dọn log cũ hơn 90 ngày (chạy hàng ngày 2AM)

**5.3 Quản Lý Lịch Tự Động**
- [ ] CRUD `AutoPublishSchedules` (Admin only)
- [ ] Cấu hình: site, topic pool, prompt template, số bài/lần, lịch chạy
- [ ] Kích hoạt/dừng schedule
- [ ] Hiển thị lần chạy tiếp theo (`NextRunAt`)
- [ ] Lịch sử chạy và kết quả

**5.4 Monitoring**
- [ ] Hiển thị trạng thái Hangfire jobs trên dashboard
- [ ] Thông báo (toast/email) khi job thất bại
- [ ] Tổng số bài đã tự động đăng theo ngày/tuần

### Definition of Done
- Bài scheduled tự động publish đúng giờ
- AI auto-publish schedule chạy đúng theo cấu hình
- Hangfire dashboard accessible và hiển thị đúng

---

## Phase 6: Dashboard & Audit Log (Tuần 6)

### Mục tiêu
Dashboard tổng quan, báo cáo, và hệ thống audit log đầy đủ.

### Công việc

**6.1 Dashboard Tổng Quan**
- [ ] Widget: Tổng bài viết hôm nay / tuần này
- [ ] Widget: Bài viết theo từng site (bar chart)
- [ ] Widget: Bài viết AI vs Manual
- [ ] Widget: Bài sắp đăng theo lịch (timeline)
- [ ] Widget: Bài thất bại cần xử lý
- [ ] Filter theo khoảng thời gian

**6.2 Audit Log Viewer**
- [ ] Trang xem audit log với filter: user, site, action, thời gian
- [ ] Phân trang (pagesize 50)
- [ ] Export CSV
- [ ] Hiển thị chi tiết từng log entry

**6.3 Báo Cáo**
- [ ] Bảng thống kê bài viết theo site theo tháng
- [ ] Thống kê token AI sử dụng theo ngày
- [ ] Thống kê tỷ lệ đăng thành công/thất bại

**6.4 Notification**
- [ ] Toast notification real-time khi job hoàn thành (SignalR)
- [ ] Badge số lượng bài `failed` trên navigation
- [ ] Email notification cho admin khi có nhiều bài fail liên tiếp (optional)

### Definition of Done
- Dashboard hiển thị đúng số liệu thực tế
- Audit log ghi lại đầy đủ các thao tác
- Export CSV hoạt động

---

## Phase 7: Testing & Production Deploy (Tuần 7)

### Mục tiêu
Kiểm thử toàn diện và triển khai production.

### Công việc

**7.1 Testing**
- [ ] Test kết nối với từng DB site thực tế
- [ ] Test INSERT bài viết vào từng site với field mapping thực tế
- [ ] Test AI generate với các loại nội dung khác nhau
- [ ] Test scheduler với bài lên lịch ngắn (5 phút)
- [ ] Test retry khi DB site không available
- [ ] Test phân quyền: Editor không truy cập được trang Admin
- [ ] Test SQL injection prevention (nhập ký tự đặc biệt vào field mapping)
- [ ] Load test: đăng nhiều bài cùng lúc

**7.2 Security Review**
- [ ] Kiểm tra tất cả connection string đã mã hóa trong DB
- [ ] Kiểm tra không có credential nào trong code / config file
- [ ] Kiểm tra tất cả input qua field mapping đều được validate
- [ ] Verify app chỉ accessible trong intranet

**7.3 Production Setup**
- [ ] Cấu hình IIS hoặc Kestrel trên server production
- [ ] Set environment variables: `PUBLISHER_ENCRYPTION_KEY`, `ANTHROPIC_API_KEY`
- [ ] Cấu hình HTTPS (self-signed cert cho intranet)
- [ ] Backup plan cho AppDB (SQL Server Agent job backup hàng ngày)
- [ ] Cấu hình Windows Service cho Hangfire workers

**7.4 Documentation**
- [ ] Hướng dẫn sử dụng cho Editor (cách soạn bài, lên lịch)
- [ ] Hướng dẫn Admin (thêm site, cấu hình mapping)
- [ ] Runbook xử lý sự cố thường gặp

**7.5 Bàn Giao**
- [ ] Demo toàn bộ tính năng
- [ ] Đào tạo người dùng (30 phút)
- [ ] Bàn giao source code + tài liệu

### Definition of Done
- Toàn bộ tính năng P1-P6 hoạt động đúng trên môi trường production
- Không có lỗ hổng bảo mật nghiêm trọng
- Tài liệu bàn giao đầy đủ

---

## Rủi Ro & Biện Pháp Giảm Thiểu

| Rủi ro | Xác suất | Tác động | Biện pháp |
|---|---|---|---|
| DB các site có cấu trúc bảng phức tạp (trigger, constraint) | Cao | Cao | Test INSERT với transaction + rollback trước khi cấu hình. Ghi log lỗi chi tiết |
| AI API timeout hoặc rate limit | Trung bình | Thấp | Retry với exponential backoff, hiển thị lỗi rõ ràng cho user |
| Network không ổn định đến DB site | Trung bình | Cao | Retry 3 lần, cập nhật status = failed, notify admin |
| Connection string bị lộ | Thấp | Rất cao | AES-256 encryption, key chỉ ở env var, không log, chỉ admin xem |
| Dữ liệu không khớp field mapping | Trung bình | Trung bình | Validate bằng test INSERT (rollback) khi lưu mapping |

---

## Công Cụ & Dependencies

```
ASP.NET Core 8
Blazor Server
Entity Framework Core 8
Dapper 2.x
Hangfire 1.8 + Hangfire.SqlServer
Serilog + Serilog.Sinks.File
TipTap hoặc Quill (rich text editor, via JS interop)
BCrypt.Net-Next
Anthropic SDK hoặc HttpClient (Claude API)
SQL Server 2019+
```

---

## Ghi Chú Quan Trọng

> **Encryption Key**: `PUBLISHER_ENCRYPTION_KEY` phải là chuỗi ngẫu nhiên 32 bytes (256-bit). Backup key này cẩn thận — nếu mất, không thể decrypt connection string của các site.

> **Quyền DB Site**: Tài khoản trong connection string của mỗi site chỉ cần quyền `INSERT` (và `SELECT` nếu cần đọc danh mục). Không dùng `sa` hoặc tài khoản có quyền cao.

> **Không expose ra internet**: App này chứa connection string của nhiều DB quan trọng. Chỉ chạy trong intranet hoặc sau VPN.
