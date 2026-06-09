namespace HotstarApi.Dtos.Subscriptions;

public class SubscriptionDto
{
    public int     Id            { get; set; }
    public string  Name          { get; set; } = string.Empty;
    public decimal MonthlyPrice  { get; set; }
    public string  MaxResolution { get; set; } = string.Empty;
    public bool    HasAds        { get; set; }
}
