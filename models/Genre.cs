namespace HotstarApi.Models;

public class Genre
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    // Navigation — many-to-many with Content
    public ICollection<Content> Contents { get; set; } = new List<Content>();
}
