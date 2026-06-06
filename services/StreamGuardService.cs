using HotstarApi.Data;
using Microsoft.EntityFrameworkCore;

namespace HotstarApi.Services;

/// <summary>
/// Enforces concurrent stream limits based on subscription tier.
///
/// Limits:
///   Free / Basic  →  1 concurrent active session
///   Premium       →  4 concurrent active sessions
/// </summary>
public interface IStreamGuardService
{
    /// <summary>
    /// Returns true if the user is within their plan's concurrent stream limit.
    /// </summary>
    Task<bool> CanStartStreamAsync(int userId);

    /// <summary>
    /// Returns the human-readable error message when the limit is exceeded.
    /// </summary>
    string ConcurrencyLimitMessage { get; }
}

public class StreamGuardService : IStreamGuardService
{
    private readonly ApplicationDbContext _db;

    public string ConcurrencyLimitMessage =>
        "You have reached the maximum limit of concurrent screens for your plan.";

    public StreamGuardService(ApplicationDbContext db) => _db = db;

    public async Task<bool> CanStartStreamAsync(int userId)
    {
        // Load subscription name + active session count in parallel
        var user = await _db.Users
            .AsNoTracking()
            .Include(u => u.Subscription)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null) return false;

        int maxStreams = user.Subscription?.Name switch
        {
            "Premium" => 4,
            _         => 1   // Free and Basic both get 1
        };

        int activeCount = await _db.UserSessions
            .AsNoTracking()
            .CountAsync(s => s.UserId == userId && s.IsActive);

        return activeCount < maxStreams;
    }
}
