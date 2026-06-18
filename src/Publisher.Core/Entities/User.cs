namespace Publisher.Core.Entities;

/// <summary>Maps to table dbo.Users.</summary>
public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    /// <summary>'Admin' | 'Editor' | 'Viewer' (see <see cref="Enums.UserRoles"/>).</summary>
    public string Role { get; set; } = Enums.UserRoles.Editor;
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation
    public ICollection<Site> CreatedSites { get; set; } = new List<Site>();
    public ICollection<Post> CreatedPosts { get; set; } = new List<Post>();
}
