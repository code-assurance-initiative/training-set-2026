using System.Collections.Concurrent;

namespace HarbourLane.Bookings.Preferences;

public sealed class InMemoryUserPreferencesStore : IUserPreferencesStore
{
    private readonly ConcurrentDictionary<string, UserPreferences> _byUser = new(StringComparer.Ordinal);

    public Task<UserPreferences> GetAsync(string userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_byUser.GetValueOrDefault(userId, UserPreferences.Default));
    }

    public Task SaveAsync(string userId, UserPreferences preferences, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        cancellationToken.ThrowIfCancellationRequested();
        _byUser[userId] = preferences;
        return Task.CompletedTask;
    }
}
