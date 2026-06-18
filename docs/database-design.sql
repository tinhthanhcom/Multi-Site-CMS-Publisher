-- ============================================================
-- DATABASE DESIGN: Multi-Site CMS Publisher (AppDB)
-- SQL Server 2019+
-- Version: 1.0
-- ============================================================

-- Tạo database
CREATE DATABASE PublisherApp
    COLLATE Vietnamese_CI_AS;
GO

USE PublisherApp;
GO

-- ============================================================
-- SECTION 1: AUTHENTICATION & USERS
-- ============================================================

CREATE TABLE Users (
    Id              INT             NOT NULL IDENTITY(1,1),
    Username        NVARCHAR(50)    NOT NULL,
    PasswordHash    NVARCHAR(256)   NOT NULL,   -- BCrypt hash
    FullName        NVARCHAR(100)   NOT NULL,
    Email           NVARCHAR(150)   NULL,
    Role            NVARCHAR(20)    NOT NULL    DEFAULT 'Editor',   -- Admin | Editor | Viewer
    IsActive        BIT             NOT NULL    DEFAULT 1,
    LastLoginAt     DATETIME2       NULL,
    CreatedAt       DATETIME2       NOT NULL    DEFAULT GETUTCDATE(),
    UpdatedAt       DATETIME2       NOT NULL    DEFAULT GETUTCDATE(),

    CONSTRAINT PK_Users             PRIMARY KEY (Id),
    CONSTRAINT UQ_Users_Username    UNIQUE (Username),
    CONSTRAINT CK_Users_Role        CHECK (Role IN ('Admin', 'Editor', 'Viewer'))
);
GO

-- ============================================================
-- SECTION 2: SITE MANAGEMENT
-- ============================================================

CREATE TABLE Sites (
    Id                      INT             NOT NULL IDENTITY(1,1),
    Name                    NVARCHAR(100)   NOT NULL,           -- Tên hiển thị (vd: "Website Spa ABC")
    BaseUrl                 NVARCHAR(500)   NULL,               -- URL website (để tham khảo)
    Description             NVARCHAR(500)   NULL,
    -- Connection
    ConnectionStringEnc     NVARCHAR(2000)  NOT NULL,           -- AES-256 encrypted
    DbType                  NVARCHAR(20)    NOT NULL DEFAULT 'SqlServer',   -- SqlServer | MySQL (mở rộng sau)
    -- AI Prompt
    SystemPrompt            NVARCHAR(MAX)   NULL,               -- System prompt riêng cho site này
    DefaultTone             NVARCHAR(50)    NULL,               -- formal | casual | seo-friendly
    DefaultLanguage         NVARCHAR(10)    NOT NULL DEFAULT 'vi',
    -- Status
    IsActive                BIT             NOT NULL DEFAULT 1,
    LastConnectionTest      DATETIME2       NULL,
    LastConnectionStatus    BIT             NULL,               -- NULL = chưa test, 1 = OK, 0 = Fail
    LastConnectionError     NVARCHAR(500)   NULL,
    -- Audit
    CreatedBy               INT             NOT NULL,
    CreatedAt               DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt               DATETIME2       NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT PK_Sites             PRIMARY KEY (Id),
    CONSTRAINT UQ_Sites_Name        UNIQUE (Name),
    CONSTRAINT FK_Sites_CreatedBy   FOREIGN KEY (CreatedBy) REFERENCES Users(Id),
    CONSTRAINT CK_Sites_DbType      CHECK (DbType IN ('SqlServer', 'MySQL'))
);
GO

-- ============================================================
-- SECTION 3: FIELD MAPPING
-- ============================================================

-- Cấu hình mapping bảng + trường bài viết cho từng site
CREATE TABLE SiteFieldMappings (
    Id              INT             NOT NULL IDENTITY(1,1),
    SiteId          INT             NOT NULL,
    -- Bảng chứa bài viết
    TableName       NVARCHAR(128)   NOT NULL,   -- vd: "Posts", "Articles", "tbl_TinTuc"
    SchemaName      NVARCHAR(128)   NOT NULL DEFAULT 'dbo',
    -- Trường bắt buộc
    FieldTitle      NVARCHAR(128)   NOT NULL,   -- vd: "Title", "TieuDe"
    FieldContent    NVARCHAR(128)   NOT NULL,   -- vd: "Content", "NoiDung"
    FieldStatus     NVARCHAR(128)   NOT NULL,   -- vd: "Status", "IsPublished"
    -- Giá trị Status tương ứng
    StatusValueDraft        NVARCHAR(50)    NOT NULL DEFAULT '0',    -- Giá trị khi lưu nháp
    StatusValuePublished    NVARCHAR(50)    NOT NULL DEFAULT '1',    -- Giá trị khi đã đăng
    -- Trường tùy chọn (NULL = không mapping)
    FieldSlug               NVARCHAR(128)   NULL,
    FieldExcerpt            NVARCHAR(128)   NULL,
    FieldThumbnail          NVARCHAR(128)   NULL,
    FieldPublishedAt        NVARCHAR(128)   NULL,
    FieldCategoryId         NVARCHAR(128)   NULL,
    FieldAuthorId           NVARCHAR(128)   NULL,
    FieldSortOrder          NVARCHAR(128)   NULL,
    FieldSeoTitle           NVARCHAR(128)   NULL,
    FieldSeoDescription     NVARCHAR(128)   NULL,
    -- Giá trị mặc định
    DefaultAuthorId         NVARCHAR(50)    NULL,   -- ID tác giả mặc định trong DB site
    DefaultCategoryId       NVARCHAR(50)    NULL,   -- ID danh mục mặc định
    -- Trường bổ sung tùy chỉnh (JSON array)
    -- [{"fieldName": "ViewCount", "defaultValue": "0", "dataType": "int"}]
    CustomFieldsJson        NVARCHAR(MAX)   NULL,
    -- Audit
    CreatedBy               INT             NOT NULL,
    CreatedAt               DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt               DATETIME2       NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT PK_SiteFieldMappings     PRIMARY KEY (Id),
    CONSTRAINT UQ_SiteFieldMappings_Site UNIQUE (SiteId),   -- Mỗi site chỉ có 1 mapping
    CONSTRAINT FK_SiteFieldMappings_Site FOREIGN KEY (SiteId) REFERENCES Sites(Id) ON DELETE CASCADE,
    CONSTRAINT FK_SiteFieldMappings_User FOREIGN KEY (CreatedBy) REFERENCES Users(Id)
);
GO

-- ============================================================
-- SECTION 4: POSTS (Bài Viết Trong AppDB)
-- ============================================================

CREATE TABLE Posts (
    Id              INT             NOT NULL IDENTITY(1,1),
    SiteId          INT             NOT NULL,
    -- Nội dung
    Title           NVARCHAR(500)   NOT NULL,
    Slug            NVARCHAR(500)   NULL,
    Content         NVARCHAR(MAX)   NOT NULL,
    Excerpt         NVARCHAR(1000)  NULL,
    Thumbnail       NVARCHAR(500)   NULL,
    CategoryId      NVARCHAR(50)    NULL,   -- ID danh mục trong DB của site
    AuthorId        NVARCHAR(50)    NULL,   -- ID tác giả trong DB của site
    SeoTitle        NVARCHAR(300)   NULL,
    SeoDescription  NVARCHAR(500)   NULL,
    CustomDataJson  NVARCHAR(MAX)   NULL,   -- Custom fields dạng JSON
    -- Trạng thái
    Status          NVARCHAR(20)    NOT NULL DEFAULT 'draft',   -- draft | scheduled | publishing | published | failed
    ScheduledAt     DATETIME2       NULL,   -- Thời điểm đăng theo lịch
    PublishedAt     DATETIME2       NULL,   -- Thời điểm thực sự đăng thành công
    -- Kết quả đăng bài
    RemotePostId    NVARCHAR(50)    NULL,   -- ID bài viết trên DB site (sau khi INSERT thành công)
    PublishError    NVARCHAR(1000)  NULL,   -- Lỗi nếu đăng thất bại
    RetryCount      INT             NOT NULL DEFAULT 0,
    -- AI
    IsAIGenerated   BIT             NOT NULL DEFAULT 0,
    AIPromptUsed    NVARCHAR(MAX)   NULL,
    AITokensUsed    INT             NULL,
    -- Audit
    CreatedBy       INT             NOT NULL,
    CreatedAt       DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt       DATETIME2       NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT PK_Posts             PRIMARY KEY (Id),
    CONSTRAINT FK_Posts_Site        FOREIGN KEY (SiteId) REFERENCES Sites(Id),
    CONSTRAINT FK_Posts_CreatedBy   FOREIGN KEY (CreatedBy) REFERENCES Users(Id),
    CONSTRAINT CK_Posts_Status      CHECK (Status IN ('draft', 'scheduled', 'publishing', 'published', 'failed'))
);
GO

CREATE INDEX IX_Posts_SiteId     ON Posts (SiteId);
CREATE INDEX IX_Posts_Status     ON Posts (Status);
CREATE INDEX IX_Posts_ScheduledAt ON Posts (ScheduledAt) WHERE Status = 'scheduled';
GO

-- ============================================================
-- SECTION 5: AI PROMPT TEMPLATES
-- ============================================================

CREATE TABLE AIPromptTemplates (
    Id              INT             NOT NULL IDENTITY(1,1),
    SiteId          INT             NULL,   -- NULL = template dùng chung cho tất cả site
    Name            NVARCHAR(100)   NOT NULL,
    Description     NVARCHAR(300)   NULL,
    ContentType     NVARCHAR(50)    NOT NULL DEFAULT 'article',   -- article | product | news | social
    SystemPrompt    NVARCHAR(MAX)   NULL,
    UserPromptTpl   NVARCHAR(MAX)   NOT NULL,   -- Template với biến {topic}, {keywords}, {length}, {tone}
    DefaultLength   INT             NOT NULL DEFAULT 800,    -- Số từ mặc định
    DefaultTone     NVARCHAR(50)    NOT NULL DEFAULT 'seo-friendly',
    IsActive        BIT             NOT NULL DEFAULT 1,
    CreatedBy       INT             NOT NULL,
    CreatedAt       DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt       DATETIME2       NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT PK_AIPromptTemplates         PRIMARY KEY (Id),
    CONSTRAINT FK_AIPromptTemplates_Site    FOREIGN KEY (SiteId) REFERENCES Sites(Id) ON DELETE CASCADE,
    CONSTRAINT FK_AIPromptTemplates_User    FOREIGN KEY (CreatedBy) REFERENCES Users(Id)
);
GO

-- ============================================================
-- SECTION 6: AUTO-PUBLISH SCHEDULES
-- ============================================================

-- Cấu hình lịch tự động sinh nội dung + đăng bài
CREATE TABLE AutoPublishSchedules (
    Id                  INT             NOT NULL IDENTITY(1,1),
    SiteId              INT             NOT NULL,
    Name                NVARCHAR(100)   NOT NULL,
    Description         NVARCHAR(300)   NULL,
    -- AI Config
    PromptTemplateId    INT             NULL,
    TopicsJson          NVARCHAR(MAX)   NOT NULL,   -- JSON array: ["topic1", "topic2", ...]
    KeywordsJson        NVARCHAR(MAX)   NULL,
    -- Schedule Config
    ScheduleType        NVARCHAR(20)    NOT NULL DEFAULT 'daily',   -- daily | weekly | cron
    CronExpression      NVARCHAR(100)   NULL,   -- Nếu ScheduleType = 'cron'
    TimeOfDay           TIME            NULL,   -- Giờ đăng (dùng cho daily/weekly)
    DayOfWeek           TINYINT         NULL,   -- 0=CN, 1=T2, ... 6=T7 (dùng cho weekly)
    PostsPerRun         INT             NOT NULL DEFAULT 1,   -- Số bài đăng mỗi lần chạy
    -- Status
    IsActive            BIT             NOT NULL DEFAULT 1,
    LastRunAt           DATETIME2       NULL,
    LastRunStatus       NVARCHAR(20)    NULL,   -- success | failed | partial
    NextRunAt           DATETIME2       NULL,
    TotalPostsPublished INT             NOT NULL DEFAULT 0,
    -- Audit
    CreatedBy           INT             NOT NULL,
    CreatedAt           DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt           DATETIME2       NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT PK_AutoPublishSchedules          PRIMARY KEY (Id),
    CONSTRAINT FK_AutoPublishSchedules_Site     FOREIGN KEY (SiteId) REFERENCES Sites(Id) ON DELETE CASCADE,
    CONSTRAINT FK_AutoPublishSchedules_Template FOREIGN KEY (PromptTemplateId) REFERENCES AIPromptTemplates(Id),
    CONSTRAINT FK_AutoPublishSchedules_User     FOREIGN KEY (CreatedBy) REFERENCES Users(Id),
    CONSTRAINT CK_AutoSchedules_Type            CHECK (ScheduleType IN ('daily', 'weekly', 'cron'))
);
GO

-- ============================================================
-- SECTION 7: AUDIT LOGS
-- ============================================================

CREATE TABLE AuditLogs (
    Id              BIGINT          NOT NULL IDENTITY(1,1),
    UserId          INT             NULL,   -- NULL = system/scheduler
    SiteId          INT             NULL,
    Action          NVARCHAR(50)    NOT NULL,   -- POST_PUBLISHED | SITE_CREATED | CONFIG_CHANGED | AI_GENERATED | LOGIN | ...
    EntityType      NVARCHAR(50)    NULL,   -- Posts | Sites | SiteFieldMappings | ...
    EntityId        NVARCHAR(50)    NULL,
    Details         NVARCHAR(MAX)   NULL,   -- JSON details
    IpAddress       NVARCHAR(45)    NULL,
    IsSuccess       BIT             NOT NULL DEFAULT 1,
    ErrorMessage    NVARCHAR(1000)  NULL,
    DurationMs      INT             NULL,
    CreatedAt       DATETIME2       NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT PK_AuditLogs PRIMARY KEY (Id)
);
GO

CREATE INDEX IX_AuditLogs_UserId    ON AuditLogs (UserId);
CREATE INDEX IX_AuditLogs_SiteId    ON AuditLogs (SiteId);
CREATE INDEX IX_AuditLogs_Action    ON AuditLogs (Action);
CREATE INDEX IX_AuditLogs_CreatedAt ON AuditLogs (CreatedAt);
GO

-- ============================================================
-- SECTION 8: HANGFIRE TABLES (tự tạo bởi Hangfire, ghi để tham khảo)
-- Hangfire sẽ tự tạo các bảng sau khi khởi động:
--   HangFire.Job, HangFire.JobQueue, HangFire.State
--   HangFire.Counter, HangFire.Hash, HangFire.List
--   HangFire.Set, HangFire.Server
-- ============================================================

-- ============================================================
-- SECTION 9: SEED DATA
-- ============================================================

-- Tài khoản admin mặc định (password: Admin@123 - phải đổi ngay)
INSERT INTO Users (Username, PasswordHash, FullName, Role)
VALUES (
    'admin',
    '$2a$12$placeholder_bcrypt_hash_replace_this',   -- Thay bằng BCrypt hash thực tế
    N'Quản Trị Viên',
    'Admin'
);
GO

-- Prompt template mặc định
INSERT INTO AIPromptTemplates (Name, Description, ContentType, UserPromptTpl, DefaultLength, DefaultTone, CreatedBy)
VALUES (
    N'Bài viết SEO tiếng Việt',
    N'Template viết bài chuẩn SEO, phù hợp mọi lĩnh vực',
    'article',
    N'Viết một bài viết SEO hoàn chỉnh bằng tiếng Việt về chủ đề: {topic}.
Từ khóa chính: {keywords}.
Độ dài: khoảng {length} từ.
Giọng văn: {tone}.
Yêu cầu: có tiêu đề H1, các mục H2 rõ ràng, đoạn mở đầu thu hút, kết luận có call-to-action.
Không dùng ngôn ngữ AI cứng nhắc, viết tự nhiên.',
    800,
    'seo-friendly',
    1
);
GO

INSERT INTO AIPromptTemplates (Name, Description, ContentType, UserPromptTpl, DefaultLength, DefaultTone, CreatedBy)
VALUES (
    N'Tin tức ngắn',
    N'Template viết tin tức ngắn gọn',
    'news',
    N'Viết một bài tin tức ngắn bằng tiếng Việt về: {topic}.
Độ dài: khoảng {length} từ. Giọng văn khách quan, trung lập.
Cấu trúc: Tiêu đề ngắn gọn, lead paragraph tóm tắt chính, thân bài chi tiết.',
    400,
    'formal',
    1
);
GO

-- ============================================================
-- SECTION 10: VIEWS & STORED PROCEDURES
-- ============================================================

-- View tổng quan bài viết
CREATE VIEW vw_PostsSummary AS
SELECT
    p.Id,
    p.Title,
    p.Status,
    p.IsAIGenerated,
    p.ScheduledAt,
    p.PublishedAt,
    p.CreatedAt,
    s.Name      AS SiteName,
    s.BaseUrl   AS SiteUrl,
    u.FullName  AS CreatedByName
FROM Posts p
JOIN Sites s ON p.SiteId = s.Id
JOIN Users u ON p.CreatedBy = u.Id;
GO

-- View thống kê theo site
CREATE VIEW vw_SiteStats AS
SELECT
    s.Id        AS SiteId,
    s.Name      AS SiteName,
    COUNT(p.Id)                                             AS TotalPosts,
    SUM(CASE WHEN p.Status = 'published' THEN 1 ELSE 0 END) AS PublishedCount,
    SUM(CASE WHEN p.Status = 'scheduled' THEN 1 ELSE 0 END) AS ScheduledCount,
    SUM(CASE WHEN p.Status = 'draft'     THEN 1 ELSE 0 END) AS DraftCount,
    SUM(CASE WHEN p.Status = 'failed'    THEN 1 ELSE 0 END) AS FailedCount,
    SUM(CASE WHEN p.IsAIGenerated = 1    THEN 1 ELSE 0 END) AS AIGeneratedCount,
    MAX(p.PublishedAt)                                      AS LastPublishedAt
FROM Sites s
LEFT JOIN Posts p ON s.Id = p.SiteId
GROUP BY s.Id, s.Name;
GO

-- SP lấy bài viết cần đăng theo lịch
CREATE PROCEDURE sp_GetScheduledPosts
    @AsOfTime DATETIME2 = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET @AsOfTime = ISNULL(@AsOfTime, GETUTCDATE());

    SELECT p.*, s.ConnectionStringEnc, s.DbType, sfm.*
    FROM Posts p
    JOIN Sites s ON p.SiteId = s.Id
    JOIN SiteFieldMappings sfm ON s.Id = sfm.SiteId
    WHERE p.Status = 'scheduled'
      AND p.ScheduledAt <= @AsOfTime
      AND s.IsActive = 1
    ORDER BY p.ScheduledAt ASC;
END;
GO

-- SP ghi audit log
CREATE PROCEDURE sp_WriteAuditLog
    @UserId     INT,
    @SiteId     INT = NULL,
    @Action     NVARCHAR(50),
    @EntityType NVARCHAR(50) = NULL,
    @EntityId   NVARCHAR(50) = NULL,
    @Details    NVARCHAR(MAX) = NULL,
    @IpAddress  NVARCHAR(45) = NULL,
    @IsSuccess  BIT = 1,
    @ErrorMsg   NVARCHAR(1000) = NULL,
    @DurationMs INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO AuditLogs (UserId, SiteId, Action, EntityType, EntityId, Details, IpAddress, IsSuccess, ErrorMessage, DurationMs)
    VALUES (@UserId, @SiteId, @Action, @EntityType, @EntityId, @Details, @IpAddress, @IsSuccess, @ErrorMsg, @DurationMs);
END;
GO
