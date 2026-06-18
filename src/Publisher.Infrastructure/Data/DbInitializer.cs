using Microsoft.EntityFrameworkCore;
using Publisher.Core.Entities;
using Publisher.Core.Enums;

namespace Publisher.Infrastructure.Data;

/// <summary>
/// Idempotent runtime seeding. Runs AFTER migrations (BCrypt hash is computed at runtime,
/// so it cannot live in a migration). Safe to call on every startup.
/// </summary>
public static class DbInitializer
{
    public static async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        var adminId = await EnsureAdminUserAsync(db, ct).ConfigureAwait(false);
        await EnsureDefaultPromptTemplatesAsync(db, adminId, ct).ConfigureAwait(false);
    }

    private static async Task<int> EnsureAdminUserAsync(AppDbContext db, CancellationToken ct)
    {
        var admin = await db.Users.FirstOrDefaultAsync(u => u.Username == "admin", ct).ConfigureAwait(false);
        if (admin is not null)
            return admin.Id;

        admin = new User
        {
            Username = "admin",
            FullName = "Quản Trị Viên",
            Role = UserRoles.Admin,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123", workFactor: 12),
            IsActive = true
        };

        db.Users.Add(admin);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return admin.Id;
    }

    private static async Task EnsureDefaultPromptTemplatesAsync(AppDbContext db, int createdBy, CancellationToken ct)
    {
        // Default templates are global (SiteId == null). Mirrors the SQL seed in database-design.sql.
        const string seoName = "Bài viết SEO tiếng Việt";
        const string newsName = "Tin tức ngắn";

        var existing = await db.AIPromptTemplates
            .Where(t => t.SiteId == null && (t.Name == seoName || t.Name == newsName))
            .Select(t => t.Name)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var toAdd = new List<AIPromptTemplate>();

        if (!existing.Contains(seoName))
        {
            toAdd.Add(new AIPromptTemplate
            {
                SiteId = null,
                Name = seoName,
                Description = "Template viết bài chuẩn SEO, phù hợp mọi lĩnh vực",
                ContentType = "article",
                UserPromptTpl =
                    "Viết một bài viết SEO hoàn chỉnh bằng tiếng Việt về chủ đề: {topic}.\n" +
                    "Từ khóa chính: {keywords}.\n" +
                    "Độ dài: khoảng {length} từ.\n" +
                    "Giọng văn: {tone}.\n" +
                    "Yêu cầu: có tiêu đề H1, các mục H2 rõ ràng, đoạn mở đầu thu hút, kết luận có call-to-action.\n" +
                    "Không dùng ngôn ngữ AI cứng nhắc, viết tự nhiên.",
                DefaultLength = 800,
                DefaultTone = "seo-friendly",
                CreatedBy = createdBy
            });
        }

        if (!existing.Contains(newsName))
        {
            toAdd.Add(new AIPromptTemplate
            {
                SiteId = null,
                Name = newsName,
                Description = "Template viết tin tức ngắn gọn",
                ContentType = "news",
                UserPromptTpl =
                    "Viết một bài tin tức ngắn bằng tiếng Việt về: {topic}.\n" +
                    "Độ dài: khoảng {length} từ. Giọng văn khách quan, trung lập.\n" +
                    "Cấu trúc: Tiêu đề ngắn gọn, lead paragraph tóm tắt chính, thân bài chi tiết.",
                DefaultLength = 400,
                DefaultTone = "formal",
                CreatedBy = createdBy
            });
        }

        if (toAdd.Count > 0)
        {
            db.AIPromptTemplates.AddRange(toAdd);
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }
}
