namespace HotstarApi.Models;

public class Profile
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Relative path under wwwroot/uploads/avatars/</summary>
    public string? AvatarUrl { get; set; }

    public bool IsKidsProfile { get; set; }

    // FK
    public int UserId { get; set; }
    public User User { get; set; } = null!;
}
