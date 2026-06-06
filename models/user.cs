namespace HotstarApi.Models;

public class User
{
    public int Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // FK — nullable: a freshly registered user starts with the Free plan (seeded)
    public int? SubscriptionId { get; set; }
    public Subscription? Subscription { get; set; }

    // Navigation
    public ICollection<Profile> Profiles { get; set; } = new List<Profile>();
}
