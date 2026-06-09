namespace HotstarApi.Models;

public class Subscription
{
    public int Id { get; set; }

    /// <summary>Free | Basic | Premium</summary>
    public string Name { get; set; } = string.Empty;

    public decimal MonthlyPrice { get; set; }

    /// <summary>e.g. "480p", "720p", "1080p", "4K"</summary>
    public string MaxResolution { get; set; } = string.Empty;

    public bool HasAds { get; set; }

    // Navigation
    public ICollection<User> Users { get; set; } = new List<User>();
}
