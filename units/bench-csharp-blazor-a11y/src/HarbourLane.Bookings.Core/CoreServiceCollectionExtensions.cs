using HarbourLane.Bookings.Bookings;
using HarbourLane.Bookings.Calendar;
using HarbourLane.Bookings.Content;
using HarbourLane.Bookings.Preferences;
using HarbourLane.Bookings.Rooms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HarbourLane.Bookings;

public static class CoreServiceCollectionExtensions
{
    public static IServiceCollection AddBookingCore(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IRoomCatalog>(_ => new InMemoryRoomCatalog(RoomSeed.Rooms));
        services.AddSingleton<IBookingStore, InMemoryBookingStore>();
        services.AddSingleton<ISiteNoticeStore, InMemorySiteNoticeStore>();
        services.AddSingleton<IUserPreferencesStore, InMemoryUserPreferencesStore>();
        services.AddScoped<BookingService>();
        services.AddScoped<WeekCalendarBuilder>();
        return services;
    }
}
