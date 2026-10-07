using System.Globalization;

namespace HarbourLane.Bookings.Web.Components;

internal static class Money
{
    private static readonly CultureInfo Pounds = CultureInfo.GetCultureInfo("en-GB");

    public static string PerHour(decimal rate) => rate.ToString("C", Pounds) + " an hour";
}
