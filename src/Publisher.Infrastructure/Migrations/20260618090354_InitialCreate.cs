using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Publisher.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    SiteId = table.Column<int>(type: "int", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(50)", nullable: true),
                    EntityId = table.Column<string>(type: "nvarchar(50)", nullable: true),
                    Details = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", nullable: true),
                    IsSuccess = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1000)", nullable: true),
                    DurationMs = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(256)", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", nullable: true),
                    Role = table.Column<string>(type: "nvarchar(20)", nullable: false, defaultValue: "Editor"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    LastLoginAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.CheckConstraint("CK_Users_Role", "[Role] IN ('Admin', 'Editor', 'Viewer')");
                });

            migrationBuilder.CreateTable(
                name: "Sites",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    BaseUrl = table.Column<string>(type: "nvarchar(500)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", nullable: true),
                    ConnectionStringEnc = table.Column<string>(type: "nvarchar(2000)", nullable: false),
                    DbType = table.Column<string>(type: "nvarchar(20)", nullable: false, defaultValue: "SqlServer"),
                    SystemPrompt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DefaultTone = table.Column<string>(type: "nvarchar(50)", nullable: true),
                    DefaultLanguage = table.Column<string>(type: "nvarchar(10)", nullable: false, defaultValue: "vi"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    LastConnectionTest = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastConnectionStatus = table.Column<bool>(type: "bit", nullable: true),
                    LastConnectionError = table.Column<string>(type: "nvarchar(500)", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sites", x => x.Id);
                    table.CheckConstraint("CK_Sites_DbType", "[DbType] IN ('SqlServer', 'MySQL')");
                    table.ForeignKey(
                        name: "FK_Sites_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AIPromptTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SiteId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(300)", nullable: true),
                    ContentType = table.Column<string>(type: "nvarchar(50)", nullable: false, defaultValue: "article"),
                    SystemPrompt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserPromptTpl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DefaultLength = table.Column<int>(type: "int", nullable: false, defaultValue: 800),
                    DefaultTone = table.Column<string>(type: "nvarchar(50)", nullable: false, defaultValue: "seo-friendly"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIPromptTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AIPromptTemplates_Site",
                        column: x => x.SiteId,
                        principalTable: "Sites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AIPromptTemplates_User",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Posts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SiteId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(500)", nullable: true),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Excerpt = table.Column<string>(type: "nvarchar(1000)", nullable: true),
                    Thumbnail = table.Column<string>(type: "nvarchar(500)", nullable: true),
                    CategoryId = table.Column<string>(type: "nvarchar(50)", nullable: true),
                    AuthorId = table.Column<string>(type: "nvarchar(50)", nullable: true),
                    SeoTitle = table.Column<string>(type: "nvarchar(300)", nullable: true),
                    SeoDescription = table.Column<string>(type: "nvarchar(500)", nullable: true),
                    CustomDataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", nullable: false, defaultValue: "draft"),
                    ScheduledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RemotePostId = table.Column<string>(type: "nvarchar(50)", nullable: true),
                    PublishError = table.Column<string>(type: "nvarchar(1000)", nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsAIGenerated = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    AIPromptUsed = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AITokensUsed = table.Column<int>(type: "int", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Posts", x => x.Id);
                    table.CheckConstraint("CK_Posts_Status", "[Status] IN ('draft', 'scheduled', 'publishing', 'published', 'failed')");
                    table.ForeignKey(
                        name: "FK_Posts_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Posts_Site",
                        column: x => x.SiteId,
                        principalTable: "Sites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SiteFieldMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SiteId = table.Column<int>(type: "int", nullable: false),
                    TableName = table.Column<string>(type: "nvarchar(128)", nullable: false),
                    SchemaName = table.Column<string>(type: "nvarchar(128)", nullable: false, defaultValue: "dbo"),
                    FieldTitle = table.Column<string>(type: "nvarchar(128)", nullable: false),
                    FieldContent = table.Column<string>(type: "nvarchar(128)", nullable: false),
                    FieldStatus = table.Column<string>(type: "nvarchar(128)", nullable: false),
                    StatusValueDraft = table.Column<string>(type: "nvarchar(50)", nullable: false, defaultValue: "0"),
                    StatusValuePublished = table.Column<string>(type: "nvarchar(50)", nullable: false, defaultValue: "1"),
                    FieldSlug = table.Column<string>(type: "nvarchar(128)", nullable: true),
                    FieldExcerpt = table.Column<string>(type: "nvarchar(128)", nullable: true),
                    FieldThumbnail = table.Column<string>(type: "nvarchar(128)", nullable: true),
                    FieldPublishedAt = table.Column<string>(type: "nvarchar(128)", nullable: true),
                    FieldCategoryId = table.Column<string>(type: "nvarchar(128)", nullable: true),
                    FieldAuthorId = table.Column<string>(type: "nvarchar(128)", nullable: true),
                    FieldSortOrder = table.Column<string>(type: "nvarchar(128)", nullable: true),
                    FieldSeoTitle = table.Column<string>(type: "nvarchar(128)", nullable: true),
                    FieldSeoDescription = table.Column<string>(type: "nvarchar(128)", nullable: true),
                    DefaultAuthorId = table.Column<string>(type: "nvarchar(50)", nullable: true),
                    DefaultCategoryId = table.Column<string>(type: "nvarchar(50)", nullable: true),
                    CustomFieldsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteFieldMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SiteFieldMappings_Site",
                        column: x => x.SiteId,
                        principalTable: "Sites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SiteFieldMappings_User",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AutoPublishSchedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SiteId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(300)", nullable: true),
                    PromptTemplateId = table.Column<int>(type: "int", nullable: true),
                    TopicsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    KeywordsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ScheduleType = table.Column<string>(type: "nvarchar(20)", nullable: false, defaultValue: "daily"),
                    CronExpression = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    TimeOfDay = table.Column<TimeSpan>(type: "time", nullable: true),
                    DayOfWeek = table.Column<byte>(type: "tinyint", nullable: true),
                    PostsPerRun = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    LastRunAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastRunStatus = table.Column<string>(type: "nvarchar(20)", nullable: true),
                    NextRunAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TotalPostsPublished = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutoPublishSchedules", x => x.Id);
                    table.CheckConstraint("CK_AutoSchedules_Type", "[ScheduleType] IN ('daily', 'weekly', 'cron')");
                    table.ForeignKey(
                        name: "FK_AutoPublishSchedules_Site",
                        column: x => x.SiteId,
                        principalTable: "Sites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AutoPublishSchedules_Template",
                        column: x => x.PromptTemplateId,
                        principalTable: "AIPromptTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AutoPublishSchedules_User",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AIPromptTemplates_CreatedBy",
                table: "AIPromptTemplates",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_AIPromptTemplates_SiteId",
                table: "AIPromptTemplates",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Action",
                table: "AuditLogs",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_CreatedAt",
                table: "AuditLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_SiteId",
                table: "AuditLogs",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId",
                table: "AuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AutoPublishSchedules_CreatedBy",
                table: "AutoPublishSchedules",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_AutoPublishSchedules_PromptTemplateId",
                table: "AutoPublishSchedules",
                column: "PromptTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_AutoPublishSchedules_SiteId",
                table: "AutoPublishSchedules",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_CreatedBy",
                table: "Posts",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_ScheduledAt",
                table: "Posts",
                column: "ScheduledAt",
                filter: "[Status] = 'scheduled'");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_SiteId",
                table: "Posts",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_Status",
                table: "Posts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SiteFieldMappings_CreatedBy",
                table: "SiteFieldMappings",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "UQ_SiteFieldMappings_Site",
                table: "SiteFieldMappings",
                column: "SiteId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sites_CreatedBy",
                table: "Sites",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "UQ_Sites_Name",
                table: "Sites",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            // ============================================================
            // VIEWS & STORED PROCEDURES (mirrors database-design.sql, Section 10)
            // Each batch is its own migrationBuilder.Sql() call; no GO separators.
            // ============================================================

            migrationBuilder.Sql(@"
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
JOIN Users u ON p.CreatedBy = u.Id;");

            migrationBuilder.Sql(@"
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
GROUP BY s.Id, s.Name;");

            migrationBuilder.Sql(@"
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
END;");

            migrationBuilder.Sql(@"
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
END;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop views & stored procedures first (mirrors Up()).
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_WriteAuditLog;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_GetScheduledPosts;");
            migrationBuilder.Sql("DROP VIEW IF EXISTS vw_SiteStats;");
            migrationBuilder.Sql("DROP VIEW IF EXISTS vw_PostsSummary;");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "AutoPublishSchedules");

            migrationBuilder.DropTable(
                name: "Posts");

            migrationBuilder.DropTable(
                name: "SiteFieldMappings");

            migrationBuilder.DropTable(
                name: "AIPromptTemplates");

            migrationBuilder.DropTable(
                name: "Sites");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
