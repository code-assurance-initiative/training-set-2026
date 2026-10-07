using System.Security.Cryptography;

namespace HarbourLane.Bookings.Bookings;

/// <summary>Short booking references a visitor can read out on the phone (no 0/O, 1/I).</summary>
public static class BookingReferences
{
    private const string Alphabet = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ";

    public static string Next() => "HL-" + RandomNumberGenerator.GetString(Alphabet, 6);
}
