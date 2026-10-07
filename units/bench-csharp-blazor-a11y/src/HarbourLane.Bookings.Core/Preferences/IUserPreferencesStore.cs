namespace HarbourLane.Bookings.Preferences;

public interface IUserPreferencesStore
{
    Task<UserPreferences> GetAsync(string userId, CancellationToken cancellationToken);

    Task SaveAsync(string userId, UserPreferences preferences, CancellationToken cancellationToken);
}
