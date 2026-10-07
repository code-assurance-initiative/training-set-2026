using System.Net.Mail;
using HarbourLane.Bookings.Rooms;

namespace HarbourLane.Bookings.Bookings;

public static class BookingRequestValidator
{
    public static BookingErrors Validate(BookingRequest request, Room? room, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(request);
        var errors = new BookingErrors();

        if (room is null)
        {
            errors.Add(BookingField.Room, "Choose a room.");
        }

        if (request.Date < today)
        {
            errors.Add(BookingField.Date, "Choose today or a later date.");
        }

        var end = request.StartTime.AddHours(request.Hours);
        if (request.StartTime < OpeningHours.Opens || request.Hours < 1 || end > OpeningHours.Closes || end < request.StartTime)
        {
            errors.Add(BookingField.StartTime, "The booking must start and end between 08:00 and 22:00.");
        }

        if (request.Hours is < 1 or > OpeningHours.MaximumHours)
        {
            errors.Add(BookingField.Hours, $"Book between 1 and {OpeningHours.MaximumHours} hours.");
        }

        if (string.IsNullOrWhiteSpace(request.OrganiserName))
        {
            errors.Add(BookingField.OrganiserName, "Enter the organiser's name.");
        }

        if (!MailAddress.TryCreate(request.OrganiserEmail, out _))
        {
            errors.Add(BookingField.OrganiserEmail, "Enter an e-mail address such as name@domain.org.");
        }

        if (request.Attendees < 1)
        {
            errors.Add(BookingField.Attendees, "At least one person must attend.");
        }
        else if (room is not null && request.Attendees > room.Capacity)
        {
            errors.Add(BookingField.Attendees, $"{room.Name} holds at most {room.Capacity} people.");
        }

        return errors;
    }
}
